using Stateless;
using RapidScada.Alarms.Models;

namespace RapidScada.Alarms.StateMachines;

/// <summary>
/// State machine for alarm lifecycle management
/// </summary>
public sealed class AlarmStateMachine
{
    private readonly StateMachine<AlarmState, AlarmTrigger> _stateMachine;
    private readonly Alarm _alarm;
    private readonly ILogger<AlarmStateMachine> _logger;

    public AlarmStateMachine(Alarm alarm, ILogger<AlarmStateMachine> logger)
    {
        _alarm = alarm;
        _logger = logger;

        _stateMachine = new StateMachine<AlarmState, AlarmTrigger>(
            () => _alarm.State,
            state => _alarm.State = state);

        ConfigureStateMachine();
    }

    private void ConfigureStateMachine()
    {
        // Inactive → Active (Alarm triggers)
        _stateMachine.Configure(AlarmState.Inactive)
            .Permit(AlarmTrigger.Trigger, AlarmState.Active);

        // Active → Acknowledged (Operator acknowledges)
        // Active → Cleared (Condition clears before acknowledgment)
        // Active → Suppressed (Operator suppresses)
        _stateMachine.Configure(AlarmState.Active)
            .OnEntry(OnAlarmActivated)
            .Permit(AlarmTrigger.Acknowledge, AlarmState.Acknowledged)
            .Permit(AlarmTrigger.Clear, AlarmState.Cleared)
            .Permit(AlarmTrigger.Suppress, AlarmState.Suppressed)
            .Permit(AlarmTrigger.Escalate, AlarmState.Active) // Stay active but escalate
            .Permit(AlarmTrigger.Expire, AlarmState.Expired);

        // Acknowledged → Cleared (Condition clears after acknowledgment)
        _stateMachine.Configure(AlarmState.Acknowledged)
            .OnEntry(OnAlarmAcknowledged)
            .Permit(AlarmTrigger.Clear, AlarmState.Cleared)
            .Permit(AlarmTrigger.Suppress, AlarmState.Suppressed);

        // Suppressed → Active (Unsuppress)
        // Suppressed → Cleared (Condition clears while suppressed)
        _stateMachine.Configure(AlarmState.Suppressed)
            .OnEntry(OnAlarmSuppressed)
            .Permit(AlarmTrigger.Trigger, AlarmState.Active)
            .Permit(AlarmTrigger.Clear, AlarmState.Cleared);

        // Cleared → Inactive (Return to normal)
        // Cleared → Active (Re-trigger)
        _stateMachine.Configure(AlarmState.Cleared)
            .OnEntry(OnAlarmCleared)
            .PermitReentry(AlarmTrigger.Clear) // Allow multiple clears
            .Permit(AlarmTrigger.Trigger, AlarmState.Active);

        // Expired (terminal state)
        _stateMachine.Configure(AlarmState.Expired)
            .OnEntry(OnAlarmExpired);
    }

    public void Fire(AlarmTrigger trigger)
    {
        if (_stateMachine.CanFire(trigger))
        {
            var previousState = _alarm.State;
            _stateMachine.Fire(trigger);

            _logger.LogInformation(
                "Alarm {AlarmId} state transition: {From} → {To} (Trigger: {Trigger})",
                _alarm.Id,
                previousState,
                _alarm.State,
                trigger);
        }
        else
        {
            _logger.LogWarning(
                "Invalid state transition for alarm {AlarmId}: Cannot fire {Trigger} from state {State}",
                _alarm.Id,
                trigger,
                _alarm.State);
        }
    }

    public bool CanFire(AlarmTrigger trigger) => _stateMachine.CanFire(trigger);

    public AlarmState CurrentState => _stateMachine.State;

    private void OnAlarmActivated()
    {
        _alarm.TriggeredAt = DateTime.UtcNow;
        _logger.LogWarning(
            "Alarm activated: {AlarmId} - {Message} (Severity: {Severity})",
            _alarm.Id,
            _alarm.Message,
            _alarm.Severity);
    }

    private void OnAlarmAcknowledged()
    {
        _alarm.AcknowledgedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "Alarm acknowledged: {AlarmId} by {User}",
            _alarm.Id,
            _alarm.AcknowledgedBy ?? "System");
    }

    private void OnAlarmSuppressed()
    {
        _logger.LogInformation(
            "Alarm suppressed: {AlarmId}",
            _alarm.Id);
    }

    private void OnAlarmCleared()
    {
        _alarm.ClearedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "Alarm cleared: {AlarmId} - {Reason}",
            _alarm.Id,
            _alarm.ClearReason ?? "Condition no longer met");
    }

    private void OnAlarmExpired()
    {
        _logger.LogInformation(
            "Alarm expired: {AlarmId}",
            _alarm.Id);
    }
}
