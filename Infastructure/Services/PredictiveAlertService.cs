namespace AiCFO.Infrastructure.Services;

using AiCFO.Infrastructure.Persistence;
using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// EF Core implementation of IPredictiveAlertService.
/// Manages predictive thresholds and triggered alerts in the database.
/// </summary>
public class PredictiveAlertService : IPredictiveAlertService
{
    private readonly AppDbContext _dbContext;
    private readonly IPredictionService _predictionService;
    private readonly ILogger<PredictiveAlertService> _logger;

    public PredictiveAlertService(
        AppDbContext dbContext,
        IPredictionService predictionService,
        ILogger<PredictiveAlertService> logger)
    {
        _dbContext = dbContext;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<Result<PredictiveThresholdDto>> CreateThresholdAsync(
        Guid tenantId, CreateThresholdRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating predictive threshold: {ThresholdName}", request.Name);

            PredictiveThresholdValue thresholdValue = request.Operator switch
            {
                ThresholdOperator.Between => PredictiveThresholdValue.CreateBetween(
                    request.Type, request.Value, request.MaxValue ?? 0, request.Severity),
                ThresholdOperator.GreaterThan => PredictiveThresholdValue.CreateGreaterThan(
                    request.Type, request.Value, request.Severity),
                ThresholdOperator.LessThan => PredictiveThresholdValue.CreateLessThan(
                    request.Type, request.Value, request.Severity),
                _ => PredictiveThresholdValue.CreateGreaterThan(request.Type, request.Value, request.Severity)
            };

            var threshold = PredictiveThreshold.Create(tenantId, request.Name, request.Description, thresholdValue);
            _dbContext.PredictiveThresholds.Add(threshold);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Threshold created: {ThresholdId}", threshold.PredictiveThresholdId);
            return Result<PredictiveThresholdDto>.Ok(MapToDto(threshold));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating predictive threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error creating threshold: {ex.Message}");
        }
    }

    public async Task<Result<PredictiveThresholdDto>> GetThresholdAsync(
        Guid tenantId, Guid thresholdId, CancellationToken ct = default)
    {
        try
        {
            var threshold = await _dbContext.PredictiveThresholds
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PredictiveThresholdId == thresholdId && !t.IsDeleted, ct);

            if (threshold == null)
                return Result<PredictiveThresholdDto>.Fail("Threshold not found");

            return Result<PredictiveThresholdDto>.Ok(MapToDto(threshold));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error retrieving threshold: {ex.Message}");
        }
    }

    public async Task<Result<List<PredictiveThresholdDto>>> GetThresholdsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var thresholds = await _dbContext.PredictiveThresholds
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .ToListAsync(ct);

            return Result<List<PredictiveThresholdDto>>.Ok(thresholds.Select(MapToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thresholds");
            return Result<List<PredictiveThresholdDto>>.Fail($"Error retrieving thresholds: {ex.Message}");
        }
    }

    public async Task<Result<PredictiveThresholdDto>> UpdateThresholdAsync(
        Guid tenantId, Guid thresholdId, UpdateThresholdRequest request, CancellationToken ct = default)
    {
        try
        {
            var threshold = await _dbContext.PredictiveThresholds
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PredictiveThresholdId == thresholdId && !t.IsDeleted, ct);

            if (threshold == null)
                return Result<PredictiveThresholdDto>.Fail("Threshold not found");

            threshold.UpdateName(request.Name);
            threshold.UpdateDescription(request.Description);
            threshold.SetActive(request.IsActive);

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Threshold updated: {ThresholdId}", thresholdId);
            return Result<PredictiveThresholdDto>.Ok(MapToDto(threshold));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating threshold");
            return Result<PredictiveThresholdDto>.Fail($"Error updating threshold: {ex.Message}");
        }
    }

    public async Task<Result<string>> DeleteThresholdAsync(
        Guid tenantId, Guid thresholdId, CancellationToken ct = default)
    {
        try
        {
            var threshold = await _dbContext.PredictiveThresholds
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PredictiveThresholdId == thresholdId && !t.IsDeleted, ct);

            if (threshold == null)
                return Result<string>.Fail("Threshold not found");

            threshold.IsDeleted = true;
            threshold.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Threshold deleted: {ThresholdId}", thresholdId);
            return Result<string>.Ok("Threshold deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting threshold");
            return Result<string>.Fail($"Error deleting threshold: {ex.Message}");
        }
    }

    public async Task<Result<string>> EnableThresholdAsync(
        Guid tenantId, Guid thresholdId, CancellationToken ct = default)
    {
        try
        {
            var threshold = await _dbContext.PredictiveThresholds
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PredictiveThresholdId == thresholdId && !t.IsDeleted, ct);

            if (threshold == null)
                return Result<string>.Fail("Threshold not found");

            threshold.SetActive(true);

            await _dbContext.SaveChangesAsync(ct);
            return Result<string>.Ok("Threshold enabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling threshold");
            return Result<string>.Fail($"Error enabling threshold: {ex.Message}");
        }
    }

    public async Task<Result<string>> DisableThresholdAsync(
        Guid tenantId, Guid thresholdId, CancellationToken ct = default)
    {
        try
        {
            var threshold = await _dbContext.PredictiveThresholds
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.PredictiveThresholdId == thresholdId && !t.IsDeleted, ct);

            if (threshold == null)
                return Result<string>.Fail("Threshold not found");

            threshold.SetActive(false);

            await _dbContext.SaveChangesAsync(ct);
            return Result<string>.Ok("Threshold disabled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling threshold");
            return Result<string>.Fail($"Error disabling threshold: {ex.Message}");
        }
    }

    public async Task<Result<List<PredictiveAlertDto>>> EvaluateThresholdsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Evaluating predictive thresholds for tenant: {TenantId}", tenantId);

            var thresholds = await _dbContext.PredictiveThresholds
                .Where(t => t.TenantId == tenantId && t.IsActive && !t.IsDeleted)
                .ToListAsync(ct);

            var alerts = new List<PredictiveAlert>();

            foreach (var threshold in thresholds)
            {
                if (!threshold.ShouldEvaluate())
                    continue;

                try
                {
                    // Get prediction for threshold type (integration point with IPredictionService)
                    var prediction = await _predictionService.GenerateTrendForecastAsync(1);

                    if (prediction.IsSuccess && prediction.Value != null)
                    {
                        var latestValue = prediction.Value.OverallConfidence;

                        if (threshold.ThresholdValue.EvaluateMetric(latestValue))
                        {
                            var alert = PredictiveAlert.Create(tenantId, threshold.PredictiveThresholdId, latestValue);
                            alerts.Add(alert);
                            threshold.IncrementAlertCount();

                            _logger.LogWarning(
                                "Threshold triggered - Type: {ThresholdType}, Value: {Value}, Severity: {Severity}",
                                threshold.ThresholdValue.Type,
                                latestValue,
                                threshold.ThresholdValue.Severity);
                        }
                    }

                    threshold.RecordEvaluation(DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating threshold: {ThresholdId}", threshold.PredictiveThresholdId);
                }
            }

            if (alerts.Any())
            {
                _dbContext.PredictiveAlerts.AddRange(alerts);
                await _dbContext.SaveChangesAsync(ct);
            }

            return Result<List<PredictiveAlertDto>>.Ok(alerts.Select(MapAlertToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating thresholds");
            return Result<List<PredictiveAlertDto>>.Fail($"Error evaluating thresholds: {ex.Message}");
        }
    }

    public async Task<Result<List<PredictiveAlertDto>>> GetAlertsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var alerts = await _dbContext.PredictiveAlerts
                .Where(a => a.TenantId == tenantId && !a.IsDeleted)
                .ToListAsync(ct);

            return Result<List<PredictiveAlertDto>>.Ok(alerts.Select(MapAlertToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return Result<List<PredictiveAlertDto>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    public async Task<Result<List<PredictiveAlertDto>>> GetActiveAlertsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var alerts = await _dbContext.PredictiveAlerts
                .Where(a => a.TenantId == tenantId && a.Status == PredictiveAlert.AlertStatus.Active && !a.IsDeleted)
                .ToListAsync(ct);

            return Result<List<PredictiveAlertDto>>.Ok(alerts.Select(MapAlertToDto).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts");
            return Result<List<PredictiveAlertDto>>.Fail($"Error retrieving active alerts: {ex.Message}");
        }
    }

    public async Task<Result<string>> AcknowledgeAlertAsync(
        Guid tenantId, Guid alertId, string? notes = null, CancellationToken ct = default)
    {
        try
        {
            var alert = await _dbContext.PredictiveAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PredictiveAlertId == alertId && !a.IsDeleted, ct);

            if (alert == null)
                return Result<string>.Fail("Alert not found");

            if (!alert.Acknowledge(notes))
                return Result<string>.Fail($"Can only acknowledge Active alerts. Current status: {alert.Status}");

            await _dbContext.SaveChangesAsync(ct);
            return Result<string>.Ok("Alert acknowledged successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert");
            return Result<string>.Fail($"Error acknowledging alert: {ex.Message}");
        }
    }

    public async Task<Result<string>> ResolveAlertAsync(
        Guid tenantId, Guid alertId, string? notes = null, CancellationToken ct = default)
    {
        try
        {
            var alert = await _dbContext.PredictiveAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PredictiveAlertId == alertId && !a.IsDeleted, ct);

            if (alert == null)
                return Result<string>.Fail("Alert not found");

            if (!alert.Resolve(notes))
                return Result<string>.Fail($"Alert already {alert.Status}. Cannot resolve");

            await _dbContext.SaveChangesAsync(ct);
            return Result<string>.Ok("Alert resolved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert");
            return Result<string>.Fail($"Error resolving alert: {ex.Message}");
        }
    }

    public async Task<Result<string>> DismissAlertAsync(
        Guid tenantId, Guid alertId, string? reason = null, CancellationToken ct = default)
    {
        try
        {
            var alert = await _dbContext.PredictiveAlerts
                .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PredictiveAlertId == alertId && !a.IsDeleted, ct);

            if (alert == null)
                return Result<string>.Fail("Alert not found");

            if (!alert.Dismiss(reason))
                return Result<string>.Fail($"Alert already {alert.Status}. Cannot dismiss");

            await _dbContext.SaveChangesAsync(ct);
            return Result<string>.Ok("Alert dismissed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert");
            return Result<string>.Fail($"Error dismissing alert: {ex.Message}");
        }
    }

    public async Task<Result<PredictiveAlertStatisticsDto>> GetStatisticsAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var thresholds = await _dbContext.PredictiveThresholds
                .Where(t => t.TenantId == tenantId && !t.IsDeleted)
                .ToListAsync(ct);

            var alerts = await _dbContext.PredictiveAlerts
                .Where(a => a.TenantId == tenantId && !a.IsDeleted)
                .ToListAsync(ct);

            var stats = new PredictiveAlertStatisticsDto
            {
                TotalThresholds = thresholds.Count,
                ActiveThresholds = thresholds.Count(t => t.IsActive),
                TotalAlerts = alerts.Count,
                ActiveAlerts = alerts.Count(a => a.Status == PredictiveAlert.AlertStatus.Active),
                AcknowledgedAlerts = alerts.Count(a => a.Status == PredictiveAlert.AlertStatus.Acknowledged),
                ResolvedAlerts = alerts.Count(a => a.Status == PredictiveAlert.AlertStatus.Resolved),
                LastAlertAt = alerts.OrderByDescending(a => a.TriggeredAt).FirstOrDefault()?.TriggeredAt
            };

            return Result<PredictiveAlertStatisticsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics");
            return Result<PredictiveAlertStatisticsDto>.Fail($"Error retrieving statistics: {ex.Message}");
        }
    }

    private PredictiveThresholdDto MapToDto(PredictiveThreshold threshold)
    {
        return new PredictiveThresholdDto
        {
            ThresholdId = threshold.PredictiveThresholdId,
            Name = threshold.Name,
            Description = threshold.Description,
            Type = threshold.ThresholdValue.Type,
            Operator = threshold.ThresholdValue.Operator,
            Value = threshold.ThresholdValue.Value,
            MaxValue = threshold.ThresholdValue.MaxValue,
            Severity = threshold.ThresholdValue.Severity,
            IsActive = threshold.IsActive,
            LastEvaluatedAt = threshold.LastEvaluatedAt,
            AlertCount = threshold.AlertCountSinceLastReset
        };
    }

    private PredictiveAlertDto MapAlertToDto(PredictiveAlert alert)
    {
        return new PredictiveAlertDto
        {
            AlertId = alert.PredictiveAlertId,
            ThresholdId = alert.ThresholdId,
            Status = alert.Status.ToString(),
            TriggeredValue = alert.TriggeredValue,
            TriggeredAt = alert.TriggeredAt,
            AcknowledgmentNotes = alert.AcknowledgmentNotes,
            AcknowledgedAt = alert.AcknowledgedAt
        };
    }
}
