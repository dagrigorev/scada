using Microsoft.Extensions.Options;
using RapidScada.Alarms.Engine;
using RapidScada.Alarms.Models;
using RapidScada.Alarms.Services;
using RapidScada.Alarms.StateMachines;
using RapidScada.Application.Abstractions;

namespace RapidScada.Alarms;

/// <summary>
/// Background service that monitors tags and detects alarm conditions
/// </summary>
public sealed class AlarmMonitoringWorker : BackgroundService
{
    private readonly ILogger<AlarmMonitoringWorker> _logger;
    private readonly ILogger<AlarmStateMachine> _smLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAlarmRepository _alarmRepository;
    private readonly AlarmEvaluationEngine _evaluationEngine;
    private readonly AlarmOptions _options;
    private readonly Dictionary<string, Alarm> _activeAlarms = new();
    private readonly Dictionary<int, double> _previousValues = new();

    public AlarmMonitoringWorker(
        ILogger<AlarmMonitoringWorker> logger,
        ILogger<AlarmStateMachine> smLogger,
        IServiceProvider serviceProvider,
        IAlarmRepository alarmRepository,
        AlarmEvaluationEngine evaluationEngine,
        IOptions<AlarmOptions> options)
    {
        _logger = logger;
        _smLogger = smLogger;
        _serviceProvider = serviceProvider;
        _alarmRepository = alarmRepository;
        _evaluationEngine = evaluationEngine;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alarm Monitoring Service starting");

        // Load active alarms from database
        await LoadActiveAlarmsAsync();

        // Start escalation monitoring
        var escalationTask = Task.Run(() => MonitorEscalationsAsync(stoppingToken), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorAlarmsAsync(stoppingToken);
                await Task.Delay(_options.MonitoringIntervalMs, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in alarm monitoring");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("Alarm Monitoring Service stopped");
    }

    private async Task LoadActiveAlarmsAsync()
    {
        var activeAlarms = await _alarmRepository.GetActiveAlarmsAsync();
        
        foreach (var alarm in activeAlarms)
        {
            _activeAlarms[alarm.AlarmRuleId] = alarm;
        }

        _logger.LogInformation("Loaded {Count} active alarms", activeAlarms.Count);
    }

    private async Task MonitorAlarmsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();

        // Get all alarm rules
        var rules = await _alarmRepository.GetActiveRulesAsync(cancellationToken);

        if (!rules.Any())
        {
            _logger.LogDebug("No active alarm rules configured");
            return;
        }

        // Get unique tag IDs
        var tagIds = rules.Select(r => r.TagId).Distinct().ToList();

        // Get current tag values
        foreach (var tagId in tagIds)
        {
            try
            {
                var tag = await tagRepository.GetByIdAsync(Domain.ValueObjects.TagId.Create(tagId), cancellationToken);
                
                if (tag?.CurrentValue is null)
                    continue;

                if (!tag.CurrentValue.TryGetNumericValue(out var currentValue))
                    continue;

                // Get previous value for rate-of-change calculations
                _previousValues.TryGetValue(tagId, out var previousValue);

                // Evaluate all rules for this tag
                var tagRules = rules.Where(r => r.TagId == tagId);

                foreach (var rule in tagRules)
                {
                    await EvaluateRuleAsync(rule, tag, currentValue, previousValue, cancellationToken);
                }

                // Store current value for next iteration
                _previousValues[tagId] = currentValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring tag {TagId}", tagId);
            }
        }
    }

    private async Task EvaluateRuleAsync(
        AlarmRule rule,
        Domain.Entities.Tag tag,
        double currentValue,
        double previousValue,
        CancellationToken cancellationToken)
    {
        var conditionMet = _evaluationEngine.EvaluateCondition(rule, currentValue, previousValue);

        var existingAlarm = _activeAlarms.TryGetValue(rule.Id, out var alarm) ? alarm : null;

        if (conditionMet)
        {
            if (existingAlarm is null)
            {
                // Trigger new alarm
                await TriggerAlarmAsync(rule, tag, currentValue, cancellationToken);
            }
            else if (existingAlarm.State == AlarmState.Cleared)
            {
                // Re-trigger cleared alarm
                var stateMachine = new AlarmStateMachine(existingAlarm, _smLogger);
                stateMachine.Fire(AlarmTrigger.Trigger);
                await _alarmRepository.UpdateAlarmAsync(existingAlarm, cancellationToken);
            }
        }
        else
        {
            if (existingAlarm is not null && 
                existingAlarm.State is AlarmState.Active or AlarmState.Acknowledged)
            {
                // Clear alarm
                await ClearAlarmAsync(existingAlarm, "Condition no longer met", cancellationToken);
            }
        }
    }

    private async Task TriggerAlarmAsync(
        AlarmRule rule,
        Domain.Entities.Tag tag,
        double triggerValue,
        CancellationToken cancellationToken)
    {
        var alarm = new Alarm
        {
            TagId = rule.TagId,
            DeviceId = tag.DeviceId.Value,
            AlarmRuleId = rule.Id,
            Severity = rule.Severity,
            State = AlarmState.Active,
            Message = $"{rule.Name}: {tag.Name} = {triggerValue}",
            TriggerValue = triggerValue,
            TriggeredAt = DateTime.UtcNow
        };

        await _alarmRepository.CreateAlarmAsync(alarm, cancellationToken);
        _activeAlarms[rule.Id] = alarm;

        var stateMachine = new AlarmStateMachine(alarm, _smLogger);
        stateMachine.Fire(AlarmTrigger.Trigger);

        await _alarmRepository.UpdateAlarmAsync(alarm, cancellationToken);

        // Execute alarm actions
        await ExecuteAlarmActionsAsync(rule, alarm, cancellationToken);

        _logger.LogWarning(
            "Alarm triggered: {RuleName} - {Message} (Severity: {Severity})",
            rule.Name,
            alarm.Message,
            alarm.Severity);
    }

    private async Task ClearAlarmAsync(Alarm alarm, string reason, CancellationToken cancellationToken)
    {
        var stateMachine = new AlarmStateMachine(alarm, _smLogger);
        stateMachine.Fire(AlarmTrigger.Clear);

        alarm.ClearReason = reason;
        await _alarmRepository.UpdateAlarmAsync(alarm, cancellationToken);

        _logger.LogInformation(
            "Alarm cleared: {AlarmId} - {Reason}",
            alarm.Id,
            reason);
    }

    private async Task ExecuteAlarmActionsAsync(AlarmRule rule, Alarm alarm, CancellationToken cancellationToken)
    {
        foreach (var action in rule.Actions.Where(a => a.ExecuteOnTrigger))
        {
            try
            {
                // In a real implementation, this would integrate with the Notifications service
                _logger.LogInformation(
                    "Executing alarm action: {ActionType} for alarm {AlarmId}",
                    action.Type,
                    alarm.Id);

                // Example: Queue notification job
                // BackgroundJob.Enqueue(() => notificationService.SendAsync(...));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing alarm action {ActionType}", action.Type);
            }
        }
    }

    private async Task MonitorEscalationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

                foreach (var kvp in _activeAlarms.ToList())
                {
                    var alarm = kvp.Value;

                    if (alarm.State != AlarmState.Active)
                        continue;

                    var rule = await _alarmRepository.GetRuleByIdAsync(alarm.AlarmRuleId, cancellationToken);
                    if (rule?.EscalationPolicy is null)
                        continue;

                    await ProcessEscalationAsync(alarm, rule, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in escalation monitoring");
            }
        }
    }

    private async Task ProcessEscalationAsync(Alarm alarm, AlarmRule rule, CancellationToken cancellationToken)
    {
        var policy = rule.EscalationPolicy!;
        var timeSinceTrigger = DateTime.UtcNow - alarm.TriggeredAt;

        foreach (var level in policy.Levels.OrderBy(l => l.Level))
        {
            if (level.Level <= alarm.EscalationLevel)
                continue;

            if (timeSinceTrigger >= level.DelayAfterTrigger)
            {
                alarm.EscalationLevel = level.Level;
                alarm.LastEscalatedAt = DateTime.UtcNow;

                if (level.UpgradeSeverity.HasValue)
                {
                    alarm.Severity = level.UpgradeSeverity.Value;
                }

                await _alarmRepository.UpdateAlarmAsync(alarm, cancellationToken);

                _logger.LogWarning(
                    "Alarm escalated: {AlarmId} to level {Level} (Severity: {Severity})",
                    alarm.Id,
                    level.Level,
                    alarm.Severity);

                // Notify escalation recipients
                // BackgroundJob.Enqueue(() => notificationService.SendEscalationNotification(...));

                break;
            }
        }
    }
}

public sealed class AlarmOptions
{
    public const string Section = "Alarms";

    public int MonitoringIntervalMs { get; set; } = 1000;
    public int EscalationCheckIntervalMinutes { get; set; } = 1;
    public int MaxActiveAlarms { get; set; } = 10000;
    public bool EnableAutoAcknowledgment { get; set; } = false;
    public TimeSpan AutoAcknowledgmentDelay { get; set; } = TimeSpan.FromHours(24);
}
