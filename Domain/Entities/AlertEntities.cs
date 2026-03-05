namespace AiCFO.Domain.Entities;

/// <summary>
/// Represents an alert threshold configuration for anomaly-based triggering.
/// Immutable value object with validation rules.
/// </summary>
public sealed record AlertThreshold
{
    /// <summary>
    /// Minimum confidence level (0-100) to trigger alert.
    /// </summary>
    public decimal MinConfidencePercentage { get; init; }

    /// <summary>
    /// Severity level that triggers alert (Low, Medium, High, Critical).
    /// </summary>
    public AnomalySeverity TriggerSeverity { get; init; }

    /// <summary>
    /// Maximum number of hours to wait before alerting (for batching).
    /// </summary>
    public int DelayHours { get; init; }

    /// <summary>
    /// Whether to aggregate multiple anomalies into single alert.
    /// </summary>
    public bool AggregateAnomalies { get; init; }

    /// <summary>
    /// Private constructor for immutability via factory methods.
    /// </summary>
    private AlertThreshold() { }

    /// <summary>
    /// Factory method to create threshold with validation.
    /// </summary>
    public static AlertThreshold Create(
        decimal minConfidence,
        AnomalySeverity triggerSeverity,
        int delayHours = 0,
        bool aggregateAnomalies = true)
    {
        if (minConfidence < 0 || minConfidence > 100)
            throw new ArgumentException("Confidence must be between 0 and 100", nameof(minConfidence));

        if (delayHours < 0 || delayHours > 168) // Max 7 days
            throw new ArgumentException("Delay hours must be between 0 and 168", nameof(delayHours));

        return new AlertThreshold
        {
            MinConfidencePercentage = minConfidence,
            TriggerSeverity = triggerSeverity,
            DelayHours = delayHours,
            AggregateAnomalies = aggregateAnomalies
        };
    }

    /// <summary>
    /// Factory method for high-sensitivity alerting.
    /// </summary>
    public static AlertThreshold HighSensitivity => new()
    {
        MinConfidencePercentage = 70,
        TriggerSeverity = AnomalySeverity.High,
        DelayHours = 0,
        AggregateAnomalies = false
    };

    /// <summary>
    /// Factory method for balanced alerting.
    /// </summary>
    public static AlertThreshold Balanced => new()
    {
        MinConfidencePercentage = 80,
        TriggerSeverity = AnomalySeverity.Medium,
        DelayHours = 1,
        AggregateAnomalies = true
    };

    /// <summary>
    /// Factory method for critical-only alerting.
    /// </summary>
    public static AlertThreshold CriticalOnly => new()
    {
        MinConfidencePercentage = 90,
        TriggerSeverity = AnomalySeverity.Critical,
        DelayHours = 0,
        AggregateAnomalies = true
    };
}

/// <summary>
/// Represents an alert notification for an anomaly detection.
/// Aggregate root tracking alert lifecycle and management.
/// </summary>
public class AlertNotification : AggregateRoot
{
    /// <summary>
    /// Unique identifier for the alert.
    /// </summary>
    public Guid AlertId { get; private set; }

    /// <summary>
    /// Reference to the anomaly that triggered the alert.
    /// </summary>
    public string AnomalyReference { get; private set; }

    /// <summary>
    /// Alert severity level.
    /// </summary>
    public AnomalySeverity Severity { get; private set; }

    /// <summary>
    /// Confidence score that triggered the alert (0-100).
    /// </summary>
    public decimal ConfidenceScore { get; private set; }

    /// <summary>
    /// Human-readable alert message.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Alert status (New, Acknowledged, Resolved, Dismissed).
    /// </summary>
    public AlertStatus Status { get; private set; }

    /// <summary>
    /// User who acknowledged the alert (if any).
    /// </summary>
    public string? AcknowledgedBy { get; private set; }

    /// <summary>
    /// Timestamp when alert was acknowledged.
    /// </summary>
    public DateTime? AcknowledgedAt { get; private set; }

    /// <summary>
    /// Resolution notes added by user.
    /// </summary>
    public string? ResolutionNotes { get; private set; }

    /// <summary>
    /// Timestamp when alert was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; private set; }

    /// <summary>
    /// Number of hours until auto-dismiss (0 = never).
    /// </summary>
    public int AutoDismissHours { get; private set; }

    /// <summary>
    /// Linked anomalies if aggregated.
    /// </summary>
    public List<string> AggregatedAnomalies { get; private set; } = new();

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
#pragma warning disable CS8618
    private AlertNotification() { }
#pragma warning restore CS8618

    /// <summary>
    /// Constructor for creating alert with full initialization.
    /// </summary>
    private AlertNotification(Guid id, Guid tenantId, Guid createdBy, string anomalyReference, AnomalySeverity severity, 
        decimal confidenceScore, string message, int autoDismissHours)
        : base(id, tenantId, createdBy)
    {
        AlertId = id;
        AnomalyReference = anomalyReference;
        Severity = severity;
        ConfidenceScore = confidenceScore;
        Message = message;
        Status = AlertStatus.New;
        AutoDismissHours = autoDismissHours;
        AggregatedAnomalies = new();
    }

    /// <summary>
    /// Factory method to create new alert notification.
    /// </summary>
    public static AlertNotification Create(
        Guid tenantId,
        Guid createdBy,
        string anomalyReference,
        AnomalySeverity severity,
        decimal confidenceScore,
        string message,
        int autoDismissHours = 0)
    {
        return new AlertNotification(
            Guid.NewGuid(),
            tenantId,
            createdBy,
            anomalyReference,
            severity,
            confidenceScore,
            message,
            autoDismissHours);
    }

    /// <summary>
    /// Acknowledge the alert (user has seen it).
    /// </summary>
    public void Acknowledge(string acknowledgedBy)
    {
        if (Status == AlertStatus.Resolved || Status == AlertStatus.Dismissed)
            throw new InvalidOperationException("Cannot acknowledge resolved or dismissed alerts");

        Status = AlertStatus.Acknowledged;
        AcknowledgedBy = acknowledgedBy;
        AcknowledgedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolve the alert with notes.
    /// </summary>
    public void Resolve(string resolutionNotes)
    {
        if (Status == AlertStatus.Dismissed)
            throw new InvalidOperationException("Cannot resolve dismissed alerts");

        Status = AlertStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Dismiss the alert without resolution.
    /// </summary>
    public void Dismiss()
    {
        Status = AlertStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add anomaly to aggregated list.
    /// </summary>
    public void AggregateAnomaly(string anomalyReference)
    {
        if (!AggregatedAnomalies.Contains(anomalyReference))
            AggregatedAnomalies.Add(anomalyReference);
    }
}

/// <summary>
/// Alert status enumeration.
/// </summary>
public enum AlertStatus
{
    /// <summary>
    /// Alert newly created.
    /// </summary>
    New = 0,

    /// <summary>
    /// Alert acknowledged by user.
    /// </summary>
    Acknowledged = 1,

    /// <summary>
    /// Alert resolved with actions taken.
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Alert dismissed without action.
    /// </summary>
    Dismissed = 3
}
