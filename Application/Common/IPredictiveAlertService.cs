namespace AiCFO.Application.Common;

/// <summary>
/// DTO for creating a new predictive threshold.
/// </summary>
public record CreateThresholdRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ThresholdType Type { get; init; }
    public required ThresholdOperator Operator { get; init; }
    public required decimal Value { get; init; }
    public decimal? MaxValue { get; init; }
    public required AlertSeverity Severity { get; init; }
}

/// <summary>
/// DTO for updating an existing predictive threshold.
/// </summary>
public record UpdateThresholdRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required AlertSeverity Severity { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// DTO for predictive threshold response.
/// </summary>
public record PredictiveThresholdDto
{
    public Guid ThresholdId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ThresholdType Type { get; init; }
    public ThresholdOperator Operator { get; init; }
    public decimal Value { get; init; }
    public decimal? MaxValue { get; init; }
    public AlertSeverity Severity { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastEvaluatedAt { get; init; }
    public int AlertCount { get; init; }
}

/// <summary>
/// DTO for predictive alert response.
/// </summary>
public record PredictiveAlertDto
{
    public Guid AlertId { get; init; }
    public Guid ThresholdId { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal TriggeredValue { get; init; }
    public DateTime TriggeredAt { get; init; }
    public string? AcknowledgmentNotes { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
}

/// <summary>
/// Service abstraction for predictive alert management.
/// Handles threshold CRUD operations, alert tracking, and statistics.
/// </summary>
public interface IPredictiveAlertService
{
    // Threshold management
    Task<Result<PredictiveThresholdDto>> CreateThresholdAsync(Guid tenantId, CreateThresholdRequest request, CancellationToken ct = default);
    Task<Result<PredictiveThresholdDto>> GetThresholdAsync(Guid tenantId, Guid thresholdId, CancellationToken ct = default);
    Task<Result<List<PredictiveThresholdDto>>> GetThresholdsAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<PredictiveThresholdDto>> UpdateThresholdAsync(Guid tenantId, Guid thresholdId, UpdateThresholdRequest request, CancellationToken ct = default);
    Task<Result<string>> DeleteThresholdAsync(Guid tenantId, Guid thresholdId, CancellationToken ct = default);
    Task<Result<string>> EnableThresholdAsync(Guid tenantId, Guid thresholdId, CancellationToken ct = default);
    Task<Result<string>> DisableThresholdAsync(Guid tenantId, Guid thresholdId, CancellationToken ct = default);

    // Threshold evaluation (called by background jobs)
    Task<Result<List<PredictiveAlertDto>>> EvaluateThresholdsAsync(Guid tenantId, CancellationToken ct = default);

    // Alert management
    Task<Result<List<PredictiveAlertDto>>> GetAlertsAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<List<PredictiveAlertDto>>> GetActiveAlertsAsync(Guid tenantId, CancellationToken ct = default);
    Task<Result<string>> AcknowledgeAlertAsync(Guid tenantId, Guid alertId, string? notes = null, CancellationToken ct = default);
    Task<Result<string>> ResolveAlertAsync(Guid tenantId, Guid alertId, string? notes = null, CancellationToken ct = default);
    Task<Result<string>> DismissAlertAsync(Guid tenantId, Guid alertId, string? reason = null, CancellationToken ct = default);

    // Statistics
    Task<Result<PredictiveAlertStatisticsDto>> GetStatisticsAsync(Guid tenantId, CancellationToken ct = default);
}

/// <summary>
/// DTO for predictive alert statistics.
/// </summary>
public record PredictiveAlertStatisticsDto
{
    public int TotalThresholds { get; init; }
    public int ActiveThresholds { get; init; }
    public int TotalAlerts { get; init; }
    public int ActiveAlerts { get; init; }
    public int AcknowledgedAlerts { get; init; }
    public int ResolvedAlerts { get; init; }
    public DateTime? LastAlertAt { get; init; }
}
