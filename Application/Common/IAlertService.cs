using AiCFO.Domain.Entities;

namespace AiCFO.Application.Common;

/// <summary>
/// Service interface for alert notification management.
/// Handles creation, retrieval, acknowledgment, and resolution of alerts.
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Create a new alert notification for an anomaly.
    /// </summary>
    Task<Result<AlertNotification>> CreateAlertAsync(
        string anomalyReference,
        AnomalySeverity severity,
        decimal confidenceScore,
        string message,
        int autoDismissHours = 0);

    /// <summary>
    /// Get alert by ID.
    /// </summary>
    Task<Result<AlertNotification>> GetAlertAsync(Guid alertId);

    /// <summary>
    /// Get all recent alerts for the current tenant.
    /// </summary>
    Task<Result<List<AlertNotification>>> GetRecentAlertsAsync(
        int daysBack = 7,
        AlertStatus? filterStatus = null);

    /// <summary>
    /// Get open alerts (not resolved or dismissed).
    /// </summary>
    Task<Result<List<AlertNotification>>> GetOpenAlertsAsync();

    /// <summary>
    /// Get alerts by severity level.
    /// </summary>
    Task<Result<List<AlertNotification>>> GetAlertsBySeverityAsync(AnomalySeverity severity);

    /// <summary>
    /// Acknowledge an alert (user has seen it).
    /// </summary>
    Task<Result<AlertNotification>> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy);

    /// <summary>
    /// Resolve an alert with notes.
    /// </summary>
    Task<Result<AlertNotification>> ResolveAlertAsync(Guid alertId, string resolutionNotes);

    /// <summary>
    /// Dismiss an alert without resolution.
    /// </summary>
    Task<Result<AlertNotification>> DismissAlertAsync(Guid alertId);

    /// <summary>
    /// Aggregate multiple anomalies into a single alert.
    /// </summary>
    Task<Result<AlertNotification>> AggregateAnomaliesAsync(
        IEnumerable<string> anomalyReferences,
        AnomalySeverity severity,
        string message);

    /// <summary>
    /// Get alert statistics for the current tenant.
    /// </summary>
    Task<Result<AlertStatisticsDto>> GetAlertStatisticsAsync();

    /// <summary>
    /// Auto-dismiss old alerts based on configured delay.
    /// </summary>
    Task<Result<int>> AutoDismissOldAlertsAsync();

    /// <summary>
    /// Update alert threshold configuration.
    /// </summary>
    Task<Result<AlertThreshold>> UpdateThresholdAsync(
        decimal minConfidence,
        AnomalySeverity triggerSeverity,
        int delayHours);

    /// <summary>
    /// Get current alert threshold configuration.
    /// </summary>
    Task<Result<AlertThreshold>> GetThresholdAsync();
}

/// <summary>
/// Data transfer object for alert statistics.
/// </summary>
public record AlertStatisticsDto(
    int TotalAlerts,
    int NewAlerts,
    int AcknowledgedAlerts,
    int ResolvedAlerts,
    int DismissedAlerts,
    int CriticalAlerts,
    int HighAlerts,
    int MediumAlerts,
    int LowAlerts,
    DateTime StatisticsGeneratedAt);

/// <summary>
/// Data transfer object for alert summary.
/// </summary>
public record AlertSummaryDto(
    Guid AlertId,
    string AnomalyReference,
    AnomalySeverity Severity,
    AlertStatus Status,
    string Message,
    decimal ConfidenceScore,
    DateTime CreatedAt,
    DateTime? AcknowledgedAt,
    DateTime? ResolvedAt,
    int AggregatedAnomalyCount);

/// <summary>
/// Data transfer object for alert detail view.
/// </summary>
public record AlertDetailDto(
    Guid AlertId,
    string AnomalyReference,
    AnomalySeverity Severity,
    AlertStatus Status,
    string Message,
    decimal ConfidenceScore,
    DateTime CreatedAt,
    string? AcknowledgedBy,
    DateTime? AcknowledgedAt,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    List<string> AggregatedAnomalies);

/// <summary>
/// Data transfer object for creating an alert.
/// </summary>
public record CreateAlertRequest(
    string AnomalyReference,
    AnomalySeverity Severity,
    decimal ConfidenceScore,
    string Message,
    int AutoDismissHours = 0);

/// <summary>
/// Data transfer object for acknowledging an alert.
/// </summary>
public record AcknowledgeAlertRequest(
    Guid AlertId,
    string AcknowledgedBy);

/// <summary>
/// Data transfer object for resolving an alert.
/// </summary>
public record ResolveAlertRequest(
    Guid AlertId,
    string ResolutionNotes);

/// <summary>
/// Data transfer object for aggregating anomalies.
/// </summary>
public record AggregateAnomaliesRequest(
    IEnumerable<string> AnomalyReferences,
    AnomalySeverity Severity,
    string Message);
