using RapidScada.Application.Abstractions;
using RapidScada.Archiver.Models;
using RapidScada.Archiver.Repositories;
using RapidScada.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace RapidScada.Archiver;

/// <summary>
/// Background service for archiving tag values and events
/// </summary>
public sealed class ArchiverWorker : BackgroundService
{
    private readonly ILogger<ArchiverWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHistoricalDataRepository _historicalRepo;
    private readonly ArchiverOptions _options;

    public ArchiverWorker(
        ILogger<ArchiverWorker> logger,
        IServiceProvider serviceProvider,
        IHistoricalDataRepository historicalRepo,
        IOptions<ArchiverOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _historicalRepo = historicalRepo;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SCADA Archiver starting at: {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Archive interval: {Interval}ms", _options.ArchiveIntervalMs);

        // Start retention policy task
        var retentionTask = Task.Run(() => RunRetentionPolicyAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveTagValuesAsync(stoppingToken);
                await Task.Delay(_options.ArchiveIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Archiver worker stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in archiver worker");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("SCADA Archiver stopped at: {Time}", DateTimeOffset.Now);
    }

    private async Task ArchiveTagValuesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();

        // Get all tags with current values
        var tags = await tagRepository.GetWithCurrentValuesAsync(cancellationToken);

        if (!tags.Any())
        {
            _logger.LogDebug("No tags with values to archive");
            return;
        }

        // Convert to history records
        var historyRecords = tags
            .Where(t => t.CurrentValue != null)
            .Select(t => new TagHistoryRecord
            {
                Timestamp = t.CurrentValue!.Timestamp,
                TagId = t.Id.Value,
                Value = t.CurrentValue.TryGetNumericValue(out var val) ? val : 0,
                Quality = t.CurrentValue.Quality,
                DeviceId = t.DeviceId.Value
            })
            .ToList();

        // Batch write to database
        if (historyRecords.Count > 0)
        {
            await _historicalRepo.WriteTagValuesAsync(historyRecords, cancellationToken);

            _logger.LogDebug(
                "Archived {Count} tag values at {Time}",
                historyRecords.Count,
                DateTime.UtcNow);
        }
    }

    private async Task RunRetentionPolicyAsync(CancellationToken cancellationToken)
    {
        var policy = new ArchiveRetentionPolicy
        {
            Name = "Default",
            RawDataRetention = TimeSpan.FromDays(_options.RawDataRetentionDays),
            MinuteDataRetention = TimeSpan.FromDays(_options.MinuteDataRetentionDays),
            HourlyDataRetention = TimeSpan.FromDays(_options.HourlyDataRetentionDays),
            DailyDataRetention = TimeSpan.FromDays(_options.DailyDataRetentionDays),
            EnableCompression = _options.EnableCompression
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Run retention policy daily
                await Task.Delay(TimeSpan.FromDays(1), cancellationToken);

                _logger.LogInformation("Applying retention policy: {PolicyName}", policy.Name);
                await _historicalRepo.ApplyRetentionPolicyAsync(policy, cancellationToken);
                _logger.LogInformation("Retention policy applied successfully");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying retention policy");
            }
        }
    }
}

/// <summary>
/// Configuration options for the Archiver service
/// </summary>
public sealed class ArchiverOptions
{
    public const string Section = "Archiver";

    /// <summary>
    /// How often to archive tag values (milliseconds)
    /// </summary>
    public int ArchiveIntervalMs { get; set; } = 60000; // 1 minute

    /// <summary>
    /// How long to keep raw data (days)
    /// </summary>
    public int RawDataRetentionDays { get; set; } = 7;

    /// <summary>
    /// How long to keep minute-aggregated data (days)
    /// </summary>
    public int MinuteDataRetentionDays { get; set; } = 30;

    /// <summary>
    /// How long to keep hourly-aggregated data (days)
    /// </summary>
    public int HourlyDataRetentionDays { get; set; } = 365;

    /// <summary>
    /// How long to keep daily-aggregated data (days)
    /// </summary>
    public int DailyDataRetentionDays { get; set; } = 3650; // 10 years

    /// <summary>
    /// Enable TimescaleDB compression
    /// </summary>
    public bool EnableCompression { get; set; } = true;
}
