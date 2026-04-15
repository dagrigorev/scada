using Dapper;
using Npgsql;
using RapidScada.Alarms.Models;
using System.Text.Json;

namespace RapidScada.Alarms.Services;

public interface IAlarmRepository
{
    Task<List<AlarmRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<AlarmRule?> GetRuleByIdAsync(string ruleId, CancellationToken cancellationToken = default);
    Task<List<AlarmRule>> GetRulesByTagIdAsync(int tagId, CancellationToken cancellationToken = default);
    Task SaveRuleAsync(AlarmRule rule, CancellationToken cancellationToken = default);
    Task DeleteRuleAsync(string ruleId, CancellationToken cancellationToken = default);
    
    Task<long> CreateAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task UpdateAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default);
    Task<Alarm?> GetAlarmByIdAsync(long alarmId, CancellationToken cancellationToken = default);
    Task<List<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default);
    Task<List<Alarm>> GetAlarmsByDeviceAsync(int deviceId, CancellationToken cancellationToken = default);
    Task<AlarmStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default);
}

public sealed class AlarmRepository : IAlarmRepository
{
    private readonly string _connectionString;
    private readonly ILogger<AlarmRepository> _logger;

    public AlarmRepository(string connectionString, ILogger<AlarmRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<List<AlarmRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, tag_id, enabled, severity, priority, 
                   condition_data, actions_data, escalation_data, metadata
            FROM alarm_rules
            WHERE enabled = true
            ORDER BY priority DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AlarmRuleRow>(sql);

        return rows.Select(MapFromRow).ToList();
    }

    public async Task<AlarmRule?> GetRuleByIdAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, tag_id, enabled, severity, priority,
                   condition_data, actions_data, escalation_data, metadata
            FROM alarm_rules
            WHERE id = @RuleId";

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<AlarmRuleRow>(sql, new { RuleId = ruleId });

        return row is not null ? MapFromRow(row) : null;
    }

    public async Task<List<AlarmRule>> GetRulesByTagIdAsync(int tagId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, name, description, tag_id, enabled, severity, priority,
                   condition_data, actions_data, escalation_data, metadata
            FROM alarm_rules
            WHERE tag_id = @TagId AND enabled = true
            ORDER BY priority DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AlarmRuleRow>(sql, new { TagId = tagId });

        return rows.Select(MapFromRow).ToList();
    }

    public async Task SaveRuleAsync(AlarmRule rule, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO alarm_rules (id, name, description, tag_id, enabled, severity, priority,
                                     condition_data, actions_data, escalation_data, metadata)
            VALUES (@Id, @Name, @Description, @TagId, @Enabled, @Severity, @Priority,
                    @ConditionData::jsonb, @ActionsData::jsonb, @EscalationData::jsonb, @Metadata::jsonb)
            ON CONFLICT (id) DO UPDATE SET
                name = EXCLUDED.name,
                description = EXCLUDED.description,
                enabled = EXCLUDED.enabled,
                severity = EXCLUDED.severity,
                priority = EXCLUDED.priority,
                condition_data = EXCLUDED.condition_data,
                actions_data = EXCLUDED.actions_data,
                escalation_data = EXCLUDED.escalation_data,
                metadata = EXCLUDED.metadata";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            rule.Id,
            rule.Name,
            rule.Description,
            rule.TagId,
            rule.Enabled,
            Severity = (int)rule.Severity,
            rule.Priority,
            ConditionData = JsonSerializer.Serialize(rule.Condition),
            ActionsData = JsonSerializer.Serialize(rule.Actions),
            EscalationData = JsonSerializer.Serialize(rule.EscalationPolicy),
            Metadata = JsonSerializer.Serialize(rule.Metadata)
        });
    }

    public async Task DeleteRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM alarm_rules WHERE id = @RuleId";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { RuleId = ruleId });
    }

    public async Task<long> CreateAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO alarms (tag_id, device_id, rule_id, severity, state, message, 
                                trigger_value, triggered_at, metadata)
            VALUES (@TagId, @DeviceId, @AlarmRuleId, @Severity, @State, @Message,
                    @TriggerValue, @TriggeredAt, @Metadata::jsonb)
            RETURNING id";

        await using var connection = new NpgsqlConnection(_connectionString);
        var id = await connection.QuerySingleAsync<long>(sql, new
        {
            alarm.TagId,
            alarm.DeviceId,
            alarm.AlarmRuleId,
            Severity = (int)alarm.Severity,
            State = alarm.State.ToString(),
            alarm.Message,
            alarm.TriggerValue,
            alarm.TriggeredAt,
            Metadata = JsonSerializer.Serialize(alarm.Metadata)
        });

        alarm.Id = id;
        return id;
    }

    public async Task UpdateAlarmAsync(Alarm alarm, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE alarms SET
                state = @State,
                acknowledged_at = @AcknowledgedAt,
                acknowledged_by = @AcknowledgedBy,
                cleared_at = @ClearedAt,
                clear_reason = @ClearReason,
                escalation_level = @EscalationLevel,
                last_escalated_at = @LastEscalatedAt,
                metadata = @Metadata::jsonb
            WHERE id = @Id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new
        {
            alarm.Id,
            State = alarm.State.ToString(),
            alarm.AcknowledgedAt,
            alarm.AcknowledgedBy,
            alarm.ClearedAt,
            alarm.ClearReason,
            alarm.EscalationLevel,
            alarm.LastEscalatedAt,
            Metadata = JsonSerializer.Serialize(alarm.Metadata)
        });
    }

    public async Task<Alarm?> GetAlarmByIdAsync(long alarmId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tag_id, device_id, rule_id as alarm_rule_id, severity, state, message,
                   trigger_value, triggered_at, acknowledged_at, acknowledged_by,
                   cleared_at, clear_reason, escalation_level, last_escalated_at, metadata
            FROM alarms
            WHERE id = @AlarmId";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Alarm>(sql, new { AlarmId = alarmId });
    }

    public async Task<List<Alarm>> GetActiveAlarmsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tag_id, device_id, rule_id as alarm_rule_id, severity, state, message,
                   trigger_value, triggered_at, acknowledged_at, acknowledged_by,
                   cleared_at, clear_reason, escalation_level, last_escalated_at, metadata
            FROM alarms
            WHERE state IN ('Active', 'Acknowledged')
            ORDER BY severity DESC, triggered_at DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<Alarm>(sql);
        return results.ToList();
    }

    public async Task<List<Alarm>> GetAlarmsByDeviceAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT id, tag_id, device_id, rule_id as alarm_rule_id, severity, state, message,
                   trigger_value, triggered_at, acknowledged_at, acknowledged_by,
                   cleared_at, clear_reason, escalation_level, last_escalated_at, metadata
            FROM alarms
            WHERE device_id = @DeviceId
            ORDER BY triggered_at DESC
            LIMIT 100";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<Alarm>(sql, new { DeviceId = deviceId });
        return results.ToList();
    }

    public async Task<AlarmStatistics> GetStatisticsAsync(DateTime? since = null, CancellationToken cancellationToken = default)
    {
        var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);

        const string sql = @"
            SELECT 
                COUNT(*) as total_alarms,
                COUNT(CASE WHEN state = 'Active' THEN 1 END) as active_alarms,
                COUNT(CASE WHEN state = 'Acknowledged' THEN 1 END) as acknowledged_alarms,
                COUNT(CASE WHEN state = 'Cleared' THEN 1 END) as cleared_alarms,
                COUNT(CASE WHEN state = 'Suppressed' THEN 1 END) as suppressed_alarms
            FROM alarms
            WHERE triggered_at >= @Since";

        await using var connection = new NpgsqlConnection(_connectionString);
        var stats = await connection.QuerySingleAsync<dynamic>(sql, new { Since = sinceDate });

        return new AlarmStatistics
        {
            TotalAlarms = stats.total_alarms,
            ActiveAlarms = stats.active_alarms,
            AcknowledgedAlarms = stats.acknowledged_alarms,
            ClearedAlarms = stats.cleared_alarms,
            SuppressedAlarms = stats.suppressed_alarms
        };
    }

    private AlarmRule MapFromRow(AlarmRuleRow row)
    {
        return new AlarmRule
        {
            Id = row.id,
            Name = row.name,
            Description = row.description ?? string.Empty,
            TagId = row.tag_id,
            Enabled = row.enabled,
            Severity = (AlarmSeverity)row.severity,
            Priority = row.priority,
            Condition = JsonSerializer.Deserialize<AlarmCondition>(row.condition_data) ?? new(),
            Actions = JsonSerializer.Deserialize<List<AlarmAction>>(row.actions_data) ?? new(),
            EscalationPolicy = string.IsNullOrEmpty(row.escalation_data) 
                ? null 
                : JsonSerializer.Deserialize<EscalationPolicy>(row.escalation_data),
            Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(row.metadata) ?? new()
        };
    }

    private sealed record AlarmRuleRow(
        string id,
        string name,
        string? description,
        int tag_id,
        bool enabled,
        int severity,
        int priority,
        string condition_data,
        string actions_data,
        string? escalation_data,
        string metadata);
}
