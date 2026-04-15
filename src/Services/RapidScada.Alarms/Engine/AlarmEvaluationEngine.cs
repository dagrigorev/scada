using RapidScada.Alarms.Models;

namespace RapidScada.Alarms.Engine;

/// <summary>
/// Evaluates alarm conditions against tag values
/// </summary>
public sealed class AlarmEvaluationEngine
{
    private readonly ILogger<AlarmEvaluationEngine> _logger;
    private readonly Dictionary<int, List<double>> _valueHistory = new();

    public AlarmEvaluationEngine(ILogger<AlarmEvaluationEngine> logger)
    {
        _logger = logger;
    }

    public bool EvaluateCondition(AlarmRule rule, double currentValue, double? previousValue = null)
    {
        try
        {
            var result = rule.Condition.Type switch
            {
                ConditionType.GreaterThan => EvaluateGreaterThan(currentValue, rule.Condition),
                ConditionType.LessThan => EvaluateLessThan(currentValue, rule.Condition),
                ConditionType.EqualTo => EvaluateEqualTo(currentValue, rule.Condition),
                ConditionType.OutOfRange => EvaluateOutOfRange(currentValue, rule.Condition),
                ConditionType.InRange => EvaluateInRange(currentValue, rule.Condition),
                ConditionType.RateOfChange => EvaluateRateOfChange(currentValue, previousValue, rule.Condition),
                ConditionType.Deviation => EvaluateDeviation(currentValue, rule.Condition),
                ConditionType.TimeInState => EvaluateTimeInState(rule.TagId, currentValue, rule.Condition),
                ConditionType.Custom => EvaluateCustomExpression(currentValue, rule.Condition),
                _ => false
            };

            if (result && rule.MinimumDuration.HasValue)
            {
                return EvaluateMinimumDuration(rule, currentValue);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating alarm condition for rule {RuleId}", rule.Id);
            return false;
        }
    }

    private bool EvaluateGreaterThan(double value, AlarmCondition condition)
    {
        if (!condition.Threshold.HasValue)
            return false;

        return value > condition.Threshold.Value;
    }

    private bool EvaluateLessThan(double value, AlarmCondition condition)
    {
        if (!condition.Threshold.HasValue)
            return false;

        return value < condition.Threshold.Value;
    }

    private bool EvaluateEqualTo(double value, AlarmCondition condition)
    {
        if (!condition.Threshold.HasValue)
            return false;

        const double epsilon = 0.0001;
        return Math.Abs(value - condition.Threshold.Value) < epsilon;
    }

    private bool EvaluateOutOfRange(double value, AlarmCondition condition)
    {
        if (!condition.LowLimit.HasValue || !condition.HighLimit.HasValue)
            return false;

        return value < condition.LowLimit.Value || value > condition.HighLimit.Value;
    }

    private bool EvaluateInRange(double value, AlarmCondition condition)
    {
        if (!condition.LowLimit.HasValue || !condition.HighLimit.HasValue)
            return false;

        return value >= condition.LowLimit.Value && value <= condition.HighLimit.Value;
    }

    private bool EvaluateRateOfChange(double currentValue, double? previousValue, AlarmCondition condition)
    {
        if (!previousValue.HasValue || !condition.Threshold.HasValue)
            return false;

        var rateOfChange = Math.Abs(currentValue - previousValue.Value);
        return rateOfChange > condition.Threshold.Value;
    }

    private bool EvaluateDeviation(double value, AlarmCondition condition)
    {
        if (!condition.Threshold.HasValue || !condition.DeviationPercent.HasValue)
            return false;

        var setpoint = condition.Threshold.Value;
        var deviationPercent = Math.Abs((value - setpoint) / setpoint * 100);

        return deviationPercent > condition.DeviationPercent.Value;
    }

    private bool EvaluateTimeInState(int tagId, double value, AlarmCondition condition)
    {
        if (!condition.Threshold.HasValue || !condition.TimeWindow.HasValue)
            return false;

        // Track value history
        if (!_valueHistory.ContainsKey(tagId))
        {
            _valueHistory[tagId] = new List<double>();
        }

        var history = _valueHistory[tagId];
        history.Add(value);

        // Keep only values within time window (approximate by count)
        var maxSamples = (int)(condition.TimeWindow.Value.TotalSeconds / 1); // Assuming 1s sampling
        if (history.Count > maxSamples)
        {
            history.RemoveAt(0);
        }

        // Check if all values in window match condition
        return history.Count >= maxSamples && 
               history.All(v => Math.Abs(v - condition.Threshold.Value) < 0.0001);
    }

    private bool EvaluateCustomExpression(double value, AlarmCondition condition)
    {
        if (string.IsNullOrEmpty(condition.Expression))
            return false;

        // Simple expression evaluation (for production, use a proper expression parser)
        try
        {
            var expression = condition.Expression
                .Replace("{value}", value.ToString())
                .Replace("{threshold}", condition.Threshold?.ToString() ?? "0");

            // This is a simplified example - use NCalc or similar for production
            _logger.LogWarning("Custom expressions not fully implemented: {Expression}", expression);
            return false;
        }
        catch
        {
            return false;
        }
    }

    private bool EvaluateMinimumDuration(AlarmRule rule, double currentValue)
    {
        // Track how long condition has been true
        // This is simplified - in production, use proper time tracking
        return true; // Placeholder
    }

    public bool ShouldApplyDeadband(AlarmRule rule, double currentValue, double? lastTriggerValue)
    {
        if (!rule.Deadband.HasValue || !lastTriggerValue.HasValue)
            return false;

        // Convert time-based deadband to value-based (simplified)
        // In production, this should be time-aware
        return Math.Abs(currentValue - lastTriggerValue.Value) < 0.5;
    }
}
