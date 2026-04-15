using Dapper;
using Npgsql;
using RapidScada.Archiver.Models;

namespace RapidScada.Archiver.Repositories;

/// <summary>
/// High-performance repository for time-series historical data using Dapper
/// </summary>
public interface IHistoricalDataRepository
{
    Task WriteTagValuesAsync(IEnumerable<TagHistoryRecord> records, CancellationToken cancellationToken = default);
    Task WriteEventAsync(EventRecord eventRecord, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagHistoryRecord>> QueryTagHistoryAsync(HistoricalDataQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<TagStatistics>> QueryAggregatedDataAsync(HistoricalDataQuery query, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventRecord>> QueryEventsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
    Task ApplyRetentionPolicyAsync(ArchiveRetentionPolicy policy, CancellationToken cancellationToken = default);
}

public sealed class HistoricalDataRepository : IHistoricalDataRepository
{
    private readonly string _connectionString;
    private readonly ILogger<HistoricalDataRepository> _logger;

    public HistoricalDataRepository(
        string connectionString,
        ILogger<HistoricalDataRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task WriteTagValuesAsync(
        IEnumerable<TagHistoryRecord> records,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO tag_history (time, tag_id, value, quality, device_id)
            VALUES (@Timestamp, @TagId, @Value, @Quality, @DeviceId)
            ON CONFLICT DO NOTHING";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var recordsList = records.ToList();
        await connection.ExecuteAsync(sql, recordsList);

        _logger.LogDebug("Wrote {Count} tag history records", recordsList.Count);
    }

    public async Task WriteEventAsync(
        EventRecord eventRecord,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO event_history 
            (event_id, time, event_type, tag_id, device_id, severity, message, acknowledged, acknowledged_at, acknowledged_by)
            VALUES 
            (@EventId, @Timestamp, @EventType, @TagId, @DeviceId, @Severity, @Message, @Acknowledged, @AcknowledgedAt, @AcknowledgedBy)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(sql, eventRecord);

        _logger.LogDebug("Wrote event record {EventId}", eventRecord.EventId);
    }

    public async Task<IEnumerable<TagHistoryRecord>> QueryTagHistoryAsync(
        HistoricalDataQuery query,
        CancellationToken cancellationToken = default)
    {
        var sql = query.Aggregation switch
        {
            AggregationType.Raw => @"
                SELECT time as Timestamp, tag_id as TagId, value as Value, quality as Quality, device_id as DeviceId
                FROM tag_history
                WHERE tag_id = @TagId AND time >= @StartTime AND time <= @EndTime
                ORDER BY time",

            AggregationType.Average => @"
                SELECT time_bucket(@Interval, time) as Timestamp, 
                       tag_id as TagId, 
                       AVG(value) as Value, 
                       AVG(quality) as Quality,
                       MAX(device_id) as DeviceId
                FROM tag_history
                WHERE tag_id = @TagId AND time >= @StartTime AND time <= @EndTime
                GROUP BY time_bucket(@Interval, time), tag_id
                ORDER BY time_bucket(@Interval, time)",

            AggregationType.Min => @"
                SELECT time_bucket(@Interval, time) as Timestamp, 
                       tag_id as TagId, 
                       MIN(value) as Value, 
                       AVG(quality) as Quality,
                       MAX(device_id) as DeviceId
                FROM tag_history
                WHERE tag_id = @TagId AND time >= @StartTime AND time <= @EndTime
                GROUP BY time_bucket(@Interval, time), tag_id
                ORDER BY time_bucket(@Interval, time)",

            AggregationType.Max => @"
                SELECT time_bucket(@Interval, time) as Timestamp, 
                       tag_id as TagId, 
                       MAX(value) as Value, 
                       AVG(quality) as Quality,
                       MAX(device_id) as DeviceId
                FROM tag_history
                WHERE tag_id = @TagId AND time >= @StartTime AND time <= @EndTime
                GROUP BY time_bucket(@Interval, time), tag_id
                ORDER BY time_bucket(@Interval, time)",

            _ => throw new ArgumentException($"Unsupported aggregation type: {query.Aggregation}")
        };

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = new
        {
            query.TagId,
            query.StartTime,
            query.EndTime,
            Interval = query.Interval ?? TimeSpan.FromMinutes(1)
        };

        var results = await connection.QueryAsync<TagHistoryRecord>(sql, parameters);

        _logger.LogDebug(
            "Queried {Count} records for tag {TagId} from {Start} to {End}",
            results.Count(),
            query.TagId,
            query.StartTime,
            query.EndTime);

        return results;
    }

    public async Task<IEnumerable<TagStatistics>> QueryAggregatedDataAsync(
        HistoricalDataQuery query,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT time_bucket(@Interval, time) as TimeWindow,
                   tag_id as TagId,
                   MIN(value) as MinValue,
                   MAX(value) as MaxValue,
                   AVG(value) as AvgValue,
                   SUM(value) as SumValue,
                   COUNT(*) as SampleCount
            FROM tag_history
            WHERE tag_id = @TagId AND time >= @StartTime AND time <= @EndTime
            GROUP BY time_bucket(@Interval, time), tag_id
            ORDER BY time_bucket(@Interval, time)";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var parameters = new
        {
            query.TagId,
            query.StartTime,
            query.EndTime,
            Interval = query.Interval ?? TimeSpan.FromHours(1)
        };

        return await connection.QueryAsync<TagStatistics>(sql, parameters);
    }

    public async Task<IEnumerable<EventRecord>> QueryEventsAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT event_id as EventId, time as Timestamp, event_type as EventType, 
                   tag_id as TagId, device_id as DeviceId, severity as Severity, 
                   message as Message, acknowledged as Acknowledged, 
                   acknowledged_at as AcknowledgedAt, acknowledged_by as AcknowledgedBy
            FROM event_history
            WHERE time >= @StartTime AND time <= @EndTime
            ORDER BY time DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QueryAsync<EventRecord>(sql, new { StartTime = startTime, EndTime = endTime });
    }

    public async Task ApplyRetentionPolicyAsync(
        ArchiveRetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Delete old raw data
        var rawCutoff = DateTime.UtcNow - policy.RawDataRetention;
        var deletedRaw = await connection.ExecuteAsync(
            "DELETE FROM tag_history WHERE time < @Cutoff",
            new { Cutoff = rawCutoff });

        _logger.LogInformation(
            "Deleted {Count} raw records older than {Cutoff}",
            deletedRaw,
            rawCutoff);

        // Compress old data if enabled
        if (policy.EnableCompression)
        {
            await connection.ExecuteAsync(
                "SELECT compress_chunk(i) FROM show_chunks('tag_history', older_than => @Cutoff) i",
                new { Cutoff = rawCutoff });

            _logger.LogInformation("Compressed chunks older than {Cutoff}", rawCutoff);
        }
    }
}
