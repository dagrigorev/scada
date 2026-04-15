using Microsoft.Extensions.Options;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Communicator;

/// <summary>
/// Communication worker service responsible for device polling and data acquisition
/// </summary>
public sealed class CommunicatorWorker : BackgroundService
{
    private readonly ILogger<CommunicatorWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly CommunicatorOptions _options;

    public CommunicatorWorker(
        ILogger<CommunicatorWorker> logger,
        IServiceProvider serviceProvider,
        IOptions<CommunicatorOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SCADA Communicator starting at: {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Polling interval: {Interval}ms", _options.PollingIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessCommunicationCycleAsync(stoppingToken);
                await Task.Delay(_options.PollingIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Communicator worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in communicator worker");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("SCADA Communicator stopped at: {Time}", DateTimeOffset.Now);
    }

    private async Task ProcessCommunicationCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var lineRepository = scope.ServiceProvider.GetRequiredService<ICommunicationLineRepository>();
        var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();
        var driverFactory = scope.ServiceProvider.GetRequiredService<IDeviceDriverFactory>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Get all active communication lines
        var activeLines = await lineRepository.GetActiveAsync(cancellationToken);

        _logger.LogDebug("Processing {Count} active communication lines", activeLines.Count);

        // Process lines in parallel with controlled concurrency
        var semaphore = new SemaphoreSlim(_options.MaxParallelLines);
        var tasks = activeLines.Select(line => ProcessLineAsync(
            line,
            deviceRepository,
            tagRepository,
            driverFactory,
            unitOfWork,
            semaphore,
            cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task ProcessLineAsync(
        CommunicationLine line,
        IDeviceRepository deviceRepository,
        ITagRepository tagRepository,
        IDeviceDriverFactory driverFactory,
        IUnitOfWork unitOfWork,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Processing line {LineId} - {LineName}", line.Id, line.Name.Value);

            var devices = await deviceRepository.GetByCommunicationLineAsync(line.Id, cancellationToken);

            foreach (var device in devices)
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

            line.RecordActivity();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Completed processing line {LineId}", line.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing communication line {LineId} - {LineName}",
                line.Id,
                line.Name.Value);
        }
        finally
        {
            semaphore.Release();
        }
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
                "No driver available for device {DeviceId} type {DeviceTypeId}",
                device.Id,
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
                _logger.LogWarning(
                    "Failed to initialize driver for device {DeviceId}: {Error}",
                    device.Id,
                    initResult.Error.Message);
                device.UpdateCommunicationStatus(false);
                return;
            }

            // Connect to device
            var connectResult = await driver.ConnectAsync(cancellationToken);
            if (connectResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to connect to device {DeviceId}: {Error}",
                    device.Id,
                    connectResult.Error.Message);
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

                // Log driver statistics periodically
                if (_options.LogStatistics)
                {
                    var stats = driver.GetStatistics();
                    _logger.LogInformation(
                        "Device {DeviceId} stats: Reads={SuccessReads}/{TotalReads}, AvgTime={AvgTime}ms",
                        device.Id,
                        stats.SuccessfulReads,
                        stats.SuccessfulReads + stats.FailedReads,
                        stats.AverageReadTime.TotalMilliseconds);
                }
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
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error polling device {DeviceId} - {DeviceName}",
                device.Id,
                device.Name.Value);

            device.SetErrorState(ex.Message);
        }
        finally
        {
            await driver.DisconnectAsync();
            await deviceRepository.UpdateAsync(device, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Configuration options for the Communicator service
/// </summary>
public sealed class CommunicatorOptions
{
    public const string Section = "Communicator";

    /// <summary>
    /// Polling interval in milliseconds
    /// </summary>
    public int PollingIntervalMs { get; set; } = 10000;

    /// <summary>
    /// Maximum number of communication lines to process in parallel
    /// </summary>
    public int MaxParallelLines { get; set; } = 5;

    /// <summary>
    /// Whether to log driver statistics
    /// </summary>
    public bool LogStatistics { get; set; } = true;
}
