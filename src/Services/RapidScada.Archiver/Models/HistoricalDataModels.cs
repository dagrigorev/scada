namespace RapidScada.Archiver.Models;

/// <summary>
/// Historical tag value record for time-series storage
/// </summary>
public sealed record TagHistoryRecord
{
    public DateTime Timestamp { get; init; }
    public int TagId { get; init; }
    public double Value { get; init; }
    public double Quality { get; init; }
    public int? DeviceId { get; init; }
}

/// <summary>
/// Aggregated tag statistics for downsampling
/// </summary>
public sealed record TagStatistics
{
    public DateTime TimeWindow { get; init; }
    public int TagId { get; init; }
    public double MinValue { get; init; }
    public double MaxValue { get; init; }
    public double AvgValue { get; init; }
    public double SumValue { get; init; }
    public int SampleCount { get; init; }
}

/// <summary>
/// Event/alarm record for historical storage
/// </summary>
public sealed record EventRecord
{
    public long EventId { get; init; }
    public DateTime Timestamp { get; init; }
    public string EventType { get; init; } = string.Empty;
    public int? TagId { get; init; }
    public int? DeviceId { get; init; }
    public string Severity { get; init; } = "Info";
    public string Message { get; init; } = string.Empty;
    public bool Acknowledged { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
}

/// <summary>
/// Archive configuration for retention policies
/// </summary>
public sealed record ArchiveRetentionPolicy
{
    public string Name { get; init; } = string.Empty;
    public TimeSpan RawDataRetention { get; init; } = TimeSpan.FromDays(7);
    public TimeSpan MinuteDataRetention { get; init; } = TimeSpan.FromDays(30);
    public TimeSpan HourlyDataRetention { get; init; } = TimeSpan.FromDays(365);
    public TimeSpan DailyDataRetention { get; init; } = TimeSpan.FromDays(3650); // 10 years
    public bool EnableCompression { get; init; } = true;
}

/// <summary>
/// Query parameters for historical data
/// </summary>
public sealed record HistoricalDataQuery
{
    public int TagId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public TimeSpan? Interval { get; init; }
    public AggregationType Aggregation { get; init; } = AggregationType.Raw;
}

/// <summary>
/// Types of aggregation for historical queries
/// </summary>
public enum AggregationType
{
    Raw,        // No aggregation
    Average,    // Average value
    Min,        // Minimum value
    Max,        // Maximum value
    Sum,        // Sum of values
    Count,      // Count of samples
    First,      // First value in period
    Last        // Last value in period
}
