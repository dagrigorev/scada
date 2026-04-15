namespace RapidScada.Alarms.Models;

/// <summary>
/// Alarm instance representing an active or historical alarm
/// </summary>
public sealed class Alarm
{
    public long Id { get; set; }
    public int TagId { get; set; }
    public int DeviceId { get; set; }
    public string AlarmRuleId { get; set; } = string.Empty;
    public AlarmSeverity Severity { get; set; }
    public AlarmState State { get; set; }
    public string Message { get; set; } = string.Empty;
    public double TriggerValue { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ClearedAt { get; set; }
    public string? ClearReason { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime? LastEscalatedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Alarm rule definition
/// </summary>
public sealed class AlarmRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TagId { get; set; }
    public bool Enabled { get; set; } = true;
    public AlarmCondition Condition { get; set; } = new();
    public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
    public int Priority { get; set; } = 5;
    public TimeSpan? Deadband { get; set; }
    public TimeSpan? MinimumDuration { get; set; }
    public List<AlarmAction> Actions { get; set; } = new();
    public EscalationPolicy? EscalationPolicy { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Alarm condition definition
/// </summary>
public sealed class AlarmCondition
{
    public ConditionType Type { get; set; }
    public double? Threshold { get; set; }
    public double? HighLimit { get; set; }
    public double? LowLimit { get; set; }
    public double? DeviationPercent { get; set; }
    public TimeSpan? TimeWindow { get; set; }
    public string? Expression { get; set; } // For complex conditions
}

/// <summary>
/// Action to take when alarm triggers
/// </summary>
public sealed class AlarmAction
{
    public AlarmActionType Type { get; set; }
    public List<string> Recipients { get; set; } = new();
    public string? Template { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool ExecuteOnTrigger { get; set; } = true;
    public bool ExecuteOnClear { get; set; } = false;
}

/// <summary>
/// Escalation policy for unacknowledged alarms
/// </summary>
public sealed class EscalationPolicy
{
    public List<EscalationLevel> Levels { get; set; } = new();
    public TimeSpan MaxEscalationTime { get; set; } = TimeSpan.FromHours(24);
}

/// <summary>
/// Escalation level definition
/// </summary>
public sealed class EscalationLevel
{
    public int Level { get; set; }
    public TimeSpan DelayAfterTrigger { get; set; }
    public List<string> NotifyRecipients { get; set; } = new();
    public AlarmSeverity? UpgradeSeverity { get; set; }
}

/// <summary>
/// Alarm severity levels
/// </summary>
public enum AlarmSeverity
{
    Info = 0,
    Low = 1,
    Warning = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Alarm state in state machine
/// </summary>
public enum AlarmState
{
    Inactive,           // Not triggered
    Active,             // Triggered, not acknowledged
    Acknowledged,       // Acknowledged by operator
    Suppressed,         // Temporarily suppressed
    Cleared,            // Condition cleared
    Expired             // Auto-expired
}

/// <summary>
/// Condition types for alarm evaluation
/// </summary>
public enum ConditionType
{
    GreaterThan,        // Value > Threshold
    LessThan,           // Value < Threshold
    EqualTo,            // Value == Threshold
    OutOfRange,         // Value < LowLimit OR Value > HighLimit
    InRange,            // LowLimit <= Value <= HighLimit
    RateOfChange,       // Change rate exceeds limit
    Deviation,          // Deviation from setpoint exceeds %
    TimeInState,        // Value in state for TimeWindow
    Custom              // Custom expression
}

/// <summary>
/// Action types for alarm responses
/// </summary>
public enum AlarmActionType
{
    SendEmail,
    SendSms,
    SendPush,
    ExecuteScript,
    CallWebhook,
    LogToFile,
    ControlOutput
}

/// <summary>
/// Alarm trigger for state machine
/// </summary>
public enum AlarmTrigger
{
    Trigger,            // Condition met
    Acknowledge,        // Operator acknowledges
    Clear,              // Condition cleared
    Suppress,           // Operator suppresses
    Escalate,           // Escalation timer
    Expire              // Expiration timer
}

/// <summary>
/// Alarm statistics and metrics
/// </summary>
public sealed record AlarmStatistics
{
    public int TotalAlarms { get; init; }
    public int ActiveAlarms { get; init; }
    public int AcknowledgedAlarms { get; init; }
    public int ClearedAlarms { get; init; }
    public int SuppressedAlarms { get; init; }
    public Dictionary<AlarmSeverity, int> AlarmsBySeverity { get; init; } = new();
    public Dictionary<int, int> AlarmsByDevice { get; init; } = new();
    public TimeSpan AverageAcknowledgmentTime { get; init; }
    public TimeSpan AverageClearTime { get; init; }
}
