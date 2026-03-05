namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Threshold type enum - represents different financial metrics that can trigger alerts.
/// </summary>
public enum ThresholdType
{
    Revenue = 1,
    Expense = 2,
    CashFlow = 3,
    Liquidity = 4,
    Profitability = 5,
    Solvency = 6,
    GrowthRate = 7,
    DebtRatio = 8
}

/// <summary>
/// Threshold operator enum - comparison operations for threshold evaluation.
/// </summary>
public enum ThresholdOperator
{
    GreaterThan = 1,
    LessThan = 2,
    GreaterThanOrEqual = 3,
    LessThanOrEqual = 4,
    Equals = 5,
    Between = 6
}

/// <summary>
/// Alert severity enum - indicates severity level of triggered alerts.
/// </summary>
public enum AlertSeverity
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4,
    Info = 5
}

/// <summary>
/// Immutable value object representing a predictive threshold value with comparison logic.
/// Supports factory methods for common threshold scenarios.
/// </summary>
public sealed record PredictiveThresholdValue
{
    public ThresholdType Type { get; init; }
    public ThresholdOperator Operator { get; init; }
    public decimal Value { get; init; }
    public decimal? MaxValue { get; init; }
    public AlertSeverity Severity { get; init; }

    private PredictiveThresholdValue(ThresholdType type, ThresholdOperator op, decimal value, decimal? maxValue, AlertSeverity severity)
    {
        Type = type;
        Operator = op;
        Value = value;
        MaxValue = maxValue;
        Severity = severity;
    }

    /// <summary>
    /// Factory method for GreaterThan threshold.
    /// Throws ArgumentException if validation fails.
    /// </summary>
    public static PredictiveThresholdValue CreateGreaterThan(
        ThresholdType type, decimal value, AlertSeverity severity = AlertSeverity.Medium)
    {
        if (value < 0)
            throw new ArgumentException("Threshold value cannot be negative", nameof(value));
        return new PredictiveThresholdValue(type, ThresholdOperator.GreaterThan, value, null, severity);
    }

    /// <summary>
    /// Factory method for LessThan threshold.
    /// Throws ArgumentException if validation fails.
    /// </summary>
    public static PredictiveThresholdValue CreateLessThan(
        ThresholdType type, decimal value, AlertSeverity severity = AlertSeverity.Medium)
    {
        if (value < 0)
            throw new ArgumentException("Threshold value cannot be negative", nameof(value));
        return new PredictiveThresholdValue(type, ThresholdOperator.LessThan, value, null, severity);
    }

    /// <summary>
    /// Factory method for Between threshold (requires both min and max values).
    /// Throws ArgumentException if validation fails.
    /// </summary>
    public static PredictiveThresholdValue CreateBetween(
        ThresholdType type, decimal minValue, decimal maxValue, AlertSeverity severity = AlertSeverity.Medium)
    {
        if (minValue < 0 || maxValue < 0)
            throw new ArgumentException("Threshold values cannot be negative");
        if (minValue >= maxValue)
            throw new ArgumentException("Minimum value must be less than maximum value");
        return new PredictiveThresholdValue(type, ThresholdOperator.Between, minValue, maxValue, severity);
    }

    /// <summary>
    /// Evaluates whether a given metric value meets this threshold.
    /// </summary>
    public bool EvaluateMetric(decimal metricValue)
    {
        return Operator switch
        {
            ThresholdOperator.GreaterThan => metricValue > Value,
            ThresholdOperator.LessThan => metricValue < Value,
            ThresholdOperator.GreaterThanOrEqual => metricValue >= Value,
            ThresholdOperator.LessThanOrEqual => metricValue <= Value,
            ThresholdOperator.Equals => metricValue == Value,
            ThresholdOperator.Between => MaxValue.HasValue && metricValue >= Value && metricValue <= MaxValue.Value,
            _ => false
        };
    }
}

/// <summary>
/// Immutable value object representing threshold evaluation results.
/// </summary>
public sealed record ThresholdEvaluationResult
{
    public bool IsTriggered { get; init; }
    public decimal MetricValue { get; init; }
    public ThresholdType Type { get; init; }
    public DateTime EvaluatedAt { get; init; }
    public string? Details { get; init; }

    public ThresholdEvaluationResult(bool isTriggered, decimal metricValue, ThresholdType type, string? details = null)
    {
        IsTriggered = isTriggered;
        MetricValue = metricValue;
        Type = type;
        EvaluatedAt = DateTime.UtcNow;
        Details = details;
    }
}
