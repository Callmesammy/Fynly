namespace AiCFO.Domain.Entities;

/// <summary>
/// Aggregate root for predictive thresholds - manages threshold configuration and evaluation scheduling.
/// Supports multiple threshold types with configurable severity levels.
/// </summary>
public class PredictiveThreshold : Entity
{
    public Guid PredictiveThresholdId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PredictiveThresholdValue ThresholdValue { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime? LastEvaluatedAt { get; private set; }
    public int AlertCountSinceLastReset { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Factory method to create new predictive threshold.
    /// </summary>
    public static PredictiveThreshold Create(
        Guid tenantId, 
        string name, 
        string description, 
        PredictiveThresholdValue thresholdValue)
    {
        return new PredictiveThreshold
        {
            PredictiveThresholdId = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            ThresholdValue = thresholdValue,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Records threshold evaluation and updates state.
    /// </summary>
    public void RecordEvaluation(DateTime evaluatedAt)
    {
        LastEvaluatedAt = evaluatedAt;
    }

    /// <summary>
    /// Increments alert count when threshold is triggered.
    /// </summary>
    public void IncrementAlertCount()
    {
        AlertCountSinceLastReset++;
    }

    /// <summary>
    /// Determines if threshold should be evaluated now based on scheduling.
    /// </summary>
    public bool ShouldEvaluate()
    {
        if (!IsActive || IsDeleted) return false;

        // Evaluate if never evaluated or more than 1 hour since last evaluation
        if (LastEvaluatedAt == null) return true;

        return (DateTime.UtcNow - LastEvaluatedAt.Value).TotalHours >= 1;
    }

    /// <summary>
    /// Updates the threshold name.
    /// </summary>
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the threshold description.
    /// </summary>
    public void UpdateDescription(string description)
    {
        Description = description ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the active status of the threshold.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Aggregate root for predictive alerts - represents triggered threshold alerts.
/// Tracks state transitions from Active → Acknowledged → Resolved or Dismissed.
/// </summary>
public class PredictiveAlert : Entity
{
    public enum AlertStatus
    {
        Active = 1,
        Acknowledged = 2,
        Resolved = 3,
        Dismissed = 4
    }

    public Guid PredictiveAlertId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ThresholdId { get; private set; }
    public AlertStatus Status { get; private set; }
    public decimal TriggeredValue { get; private set; }
    public DateTime TriggeredAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgmentNotes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Factory method to create new predictive alert.
    /// </summary>
    public static PredictiveAlert Create(Guid tenantId, Guid thresholdId, decimal triggeredValue)
    {
        return new PredictiveAlert
        {
            PredictiveAlertId = Guid.NewGuid(),
            TenantId = tenantId,
            ThresholdId = thresholdId,
            Status = AlertStatus.Active,
            TriggeredValue = triggeredValue,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Acknowledges the alert with optional notes.
    /// Returns false if alert is not in Active status.
    /// </summary>
    public bool Acknowledge(string? notes = null)
    {
        if (Status != AlertStatus.Active)
            return false;

        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgmentNotes = notes;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Resolves the alert with optional resolution notes.
    /// Returns false if alert is already resolved or dismissed.
    /// </summary>
    public bool Resolve(string? notes = null)
    {
        if (Status == AlertStatus.Resolved || Status == AlertStatus.Dismissed)
            return false;

        Status = AlertStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = notes;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Dismisses the alert as false positive or not actionable.
    /// Returns false if alert is already resolved or dismissed.
    /// </summary>
    public bool Dismiss(string? reason = null)
    {
        if (Status == AlertStatus.Resolved || Status == AlertStatus.Dismissed)
            return false;

        Status = AlertStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = reason ?? "Dismissed by user";
        UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
