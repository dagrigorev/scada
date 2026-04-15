using RapidScada.Application.Abstractions;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Server;

/// <summary>
/// Background worker service for SCADA server operations
/// </summary>
public sealed class ServerWorker : BackgroundService
{
    private readonly ILogger<ServerWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

    public ServerWorker(
        ILogger<ServerWorker> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SCADA Server starting at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDevicePollingAsync(stoppingToken);
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Server worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in server worker");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("SCADA Server stopped at: {Time}", DateTimeOffset.Now);
    }

    private async Task ProcessDevicePollingAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var lineRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLineRepository>();
        var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();
        var driverFactory = scope.ServiceProvider.GetRequiredService<IDeviceDriverFactory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Get all active communication lines
        var activeLines = await lineRepository.GetActiveAsync(cancellationToken);

        foreach (var line in activeLines)
        {
            try
            {
                await ProcessCommunicationLineAsync(
                    line,
                    deviceRepository,
                    tagRepository,
                    driverFactory,
                    unitOfWork,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing communication line {LineId} - {LineName}",
                    line.Id,
                    line.Name.Value);
            }
        }
    }

    private async Task ProcessCommunicationLineAsync(
        CommunicationLine line,
        IDeviceRepository deviceRepository,
        ITagRepository tagRepository,
        IDeviceDriverFactory driverFactory,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var devices = await deviceRepository.GetByCommunicationLineAsync(line.Id, cancellationToken);

        foreach (var device in devices)
        {
            try
            {
                await PollDeviceAsync(
                    device,
                    line,
                    deviceRepository,
                    tagRepository,
                    driverFactory,
                    unitOfWork,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error polling device {DeviceId} - {DeviceName}",
                    device.Id,
                    device.Name.Value);

                device.SetErrorState(ex.Message);
                await deviceRepository.UpdateAsync(device, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        line.RecordActivity();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task PollDeviceAsync(
        Device device,
        CommunicationLine line,
        IDeviceRepository deviceRepository,
        ITagRepository tagRepository,
        IDeviceDriverFactory driverFactory,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var driverResult = driverFactory.CreateDriver(device.DeviceTypeId);
        if (driverResult.IsFailure)
        {
            _logger.LogWarning(
                "No driver available for device type {DeviceTypeId}",
                device.DeviceTypeId);
            return;
        }

        var driver = driverResult.Value;

        try
        {
            // Initialize driver
            var initResult = await driver.InitializeAsync(
                device,
                line.ConnectionSettings,
                cancellationToken);

            if (initResult.IsFailure)
            {
                device.UpdateCommunicationStatus(false);
                return;
            }

            // Connect to device
            var connectResult = await driver.ConnectAsync(cancellationToken);
            if (connectResult.IsFailure)
            {
                device.UpdateCommunicationStatus(false);
                return;
            }

            // Read all tags
            var readResult = await driver.ReadTagsAsync(cancellationToken);
            
            if (readResult.IsSuccess)
            {
                // Update tag values
                var tagUpdates = new List<(TagId TagId, TagValue Value)>();

                foreach (var reading in readResult.Value)
                {
                    var tag = device.Tags.FirstOrDefault(t => t.Number == reading.TagNumber);
                    if (tag is not null)
                    {
                        var tagValue = TagValue.Create(
                            reading.Value,
                            reading.Timestamp,
                            reading.Quality);

                        tagUpdates.Add((tag.Id, tagValue));
                    }
                }

                if (tagUpdates.Count > 0)
                {
                    await tagRepository.BulkUpdateValuesAsync(tagUpdates, cancellationToken);
                }

                device.UpdateCommunicationStatus(true);
                
                _logger.LogDebug(
                    "Successfully polled device {DeviceId} - {DeviceName}, read {Count} tags",
                    device.Id,
                    device.Name.Value,
                    readResult.Value.Count);
            }
            else
            {
                device.UpdateCommunicationStatus(false);
                _logger.LogWarning(
                    "Failed to read from device {DeviceId}: {Error}",
                    device.Id,
                    readResult.Error.Message);
            }
        }
        finally
        {
            await driver.DisconnectAsync();
        }

        await deviceRepository.UpdateAsync(device, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
