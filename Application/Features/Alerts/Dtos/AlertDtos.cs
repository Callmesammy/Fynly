namespace AiCFO.Application.Features.Alerts.Dtos;

using AiCFO.Domain.Entities;

/// <summary>
/// Data transfer object for alert summary information.
/// </summary>
public record AlertDto(
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
