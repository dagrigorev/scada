using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RapidScada.Application.Abstractions;
using RapidScada.Realtime.Hubs;

namespace RapidScada.Realtime.Services;

/// <summary>
/// Background service that monitors tag changes and broadcasts updates via SignalR
/// </summary>
public sealed class RealtimeBroadcastService : BackgroundService
{
    private readonly ILogger<RealtimeBroadcastService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ScadaHub> _hubContext;
    private readonly RealtimeOptions _options;
    private readonly Dictionary<int, (double Value, DateTime Timestamp)> _lastValues = new();

    public RealtimeBroadcastService(
        ILogger<RealtimeBroadcastService> logger,
        IServiceProvider serviceProvider,
        IHubContext<ScadaHub> hubContext,
        IOptions<RealtimeOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Realtime Broadcast Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BroadcastTagUpdatesAsync(stoppingToken);
                await Task.Delay(_options.BroadcastIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in realtime broadcast service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Realtime Broadcast Service stopped");
    }

    private async Task BroadcastTagUpdatesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();

        // Get all tags with current values
        var tags = await tagRepository.GetWithCurrentValuesAsync(cancellationToken);

        var updates = new List<TagValueUpdate>();

        foreach (var tag in tags)
        {
            if (tag.CurrentValue is null)
                continue;

            var tagId = tag.Id.Value;

            // Check if value changed
            if (_lastValues.TryGetValue(tagId, out var lastValue))
            {
                var valueChanged = Math.Abs(lastValue.Value - (tag.CurrentValue.TryGetNumericValue(out var val) ? val : 0)) > 0.0001;
                var timeChanged = tag.CurrentValue.Timestamp > lastValue.Timestamp;

                if (!valueChanged && !timeChanged)
                    continue;
            }

            if (!tag.CurrentValue.TryGetNumericValue(out var numericValue))
                continue;

            // Store last value
            _lastValues[tagId] = (numericValue, tag.CurrentValue.Timestamp);

            // Create update
            updates.Add(new TagValueUpdate
            {
                TagId = tagId,
                Value = numericValue,
                Quality = tag.CurrentValue.Quality,
                Timestamp = tag.CurrentValue.Timestamp,
                DeviceId = tag.DeviceId.Value
            });
        }

        if (updates.Count == 0)
            return;

        // Broadcast to all connected clients
        await _hubContext.Clients.All.SendAsync("TagValuesUpdated", updates, cancellationToken);

        _logger.LogDebug("Broadcasted {Count} tag updates", updates.Count);
    }
}

public sealed class RealtimeOptions
{
    public const string Section = "Realtime";

    /// <summary>
    /// How often to broadcast updates (milliseconds)
    /// </summary>
    public int BroadcastIntervalMs { get; set; } = 1000;

    /// <summary>
    /// Enable Redis backplane for scaling
    /// </summary>
    public bool UseRedisBackplane { get; set; } = false;

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }
}
