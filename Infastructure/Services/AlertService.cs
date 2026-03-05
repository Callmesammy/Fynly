using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiCFO.Infrastructure.Services;
/// EF Core implementation of alert notification service.
/// Manages alert lifecycle: creation, retrieval, acknowledgment, resolution.
/// </summary>
public class AlertService : IAlertService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        AppDbContext dbContext,
        ITenantContext tenantContext,
        ILogger<AlertService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AlertNotification>> CreateAlertAsync(
        string anomalyReference,
        AnomalySeverity severity,
        decimal confidenceScore,
        string message,
        int autoDismissHours = 0)
    {
        try
        {
            _logger.LogInformation(
                "Creating alert for anomaly {AnomalyReference} with severity {Severity} for tenant {TenantId}",
                anomalyReference, severity, _tenantContext.TenantId);

            var alert = AlertNotification.Create(
                _tenantContext.TenantId,
                _tenantContext.UserId,
                anomalyReference,
                severity,
                confidenceScore,
                message,
                autoDismissHours);

            _dbContext.Alerts.Add(alert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully created alert {AlertId}", alert.AlertId);
            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert for anomaly {AnomalyReference}", anomalyReference);
            return Result<AlertNotification>.Fail($"Error creating alert: {ex.Message}");
        }
    }

    public async Task<Result<AlertNotification>> GetAlertAsync(Guid alertId)
    {
        try
        {
            _logger.LogInformation("Retrieving alert {AlertId} for tenant {TenantId}", alertId, _tenantContext.TenantId);

            var alert = await _dbContext.Alerts
                .FirstOrDefaultAsync(a => a.AlertId == alertId);

            if (alert == null)
                return Result<AlertNotification>.Fail($"Alert {alertId} not found");

            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", alertId);
            return Result<AlertNotification>.Fail($"Error retrieving alert: {ex.Message}");
        }
    }

    public async Task<Result<List<AlertNotification>>> GetRecentAlertsAsync(
        int daysBack = 7,
        AlertStatus? filterStatus = null)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving recent alerts ({DaysBack} days) for tenant {TenantId}",
                daysBack, _tenantContext.TenantId);

            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            IQueryable<AlertNotification> query = _dbContext.Alerts
                .Where(a => a.CreatedAt >= cutoffDate);

            if (filterStatus.HasValue)
                query = query.Where(a => a.Status == filterStatus.Value);

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Result<List<AlertNotification>>.Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent alerts");
            return Result<List<AlertNotification>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    public async Task<Result<List<AlertNotification>>> GetOpenAlertsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving open alerts for tenant {TenantId}", _tenantContext.TenantId);

            var alerts = await _dbContext.Alerts
                .Where(a => a.Status != AlertStatus.Resolved && a.Status != AlertStatus.Dismissed)
                .OrderByDescending(a => a.Severity)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Result<List<AlertNotification>>.Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open alerts");
            return Result<List<AlertNotification>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    public async Task<Result<List<AlertNotification>>> GetAlertsBySeverityAsync(AnomalySeverity severity)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving alerts with severity {Severity} for tenant {TenantId}",
                severity, _tenantContext.TenantId);

            var alerts = await _dbContext.Alerts
                .Where(a => a.Severity == severity)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return Result<List<AlertNotification>>.Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts by severity {Severity}", severity);
            return Result<List<AlertNotification>>.Fail($"Error retrieving alerts: {ex.Message}");
        }
    }

    public async Task<Result<AlertNotification>> AcknowledgeAlertAsync(Guid alertId, string acknowledgedBy)
    {
        try
        {
            _logger.LogInformation(
                "Acknowledging alert {AlertId} by {AcknowledgedBy} for tenant {TenantId}",
                alertId, acknowledgedBy, _tenantContext.TenantId);

            var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (alert == null)
                return Result<AlertNotification>.Fail($"Alert {alertId} not found");

            alert.Acknowledge(acknowledgedBy);

            _dbContext.Alerts.Update(alert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully acknowledged alert {AlertId}", alertId);
            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return Result<AlertNotification>.Fail($"Error acknowledging alert: {ex.Message}");
        }
    }

    public async Task<Result<AlertNotification>> ResolveAlertAsync(Guid alertId, string resolutionNotes)
    {
        try
        {
            _logger.LogInformation(
                "Resolving alert {AlertId} with notes for tenant {TenantId}",
                alertId, _tenantContext.TenantId);

            var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (alert == null)
                return Result<AlertNotification>.Fail($"Alert {alertId} not found");

            alert.Resolve(resolutionNotes);

            _dbContext.Alerts.Update(alert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully resolved alert {AlertId}", alertId);
            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", alertId);
            return Result<AlertNotification>.Fail($"Error resolving alert: {ex.Message}");
        }
    }

    public async Task<Result<AlertNotification>> DismissAlertAsync(Guid alertId)
    {
        try
        {
            _logger.LogInformation(
                "Dismissing alert {AlertId} for tenant {TenantId}",
                alertId, _tenantContext.TenantId);

            var alert = await _dbContext.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
            if (alert == null)
                return Result<AlertNotification>.Fail($"Alert {alertId} not found");

            alert.Dismiss();

            _dbContext.Alerts.Update(alert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully dismissed alert {AlertId}", alertId);
            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", alertId);
            return Result<AlertNotification>.Fail($"Error dismissing alert: {ex.Message}");
        }
    }

    public async Task<Result<AlertNotification>> AggregateAnomaliesAsync(
        IEnumerable<string> anomalyReferences,
        AnomalySeverity severity,
        string message)
    {
        try
        {
            _logger.LogInformation(
                "Aggregating anomalies into single alert for tenant {TenantId}",
                _tenantContext.TenantId);

            var alertId = Guid.NewGuid().ToString();
            var alert = AlertNotification.Create(
                _tenantContext.TenantId,
                _tenantContext.UserId,
                alertId,
                severity,
                95m,
                message);

            foreach (var anomaly in anomalyReferences)
                alert.AggregateAnomaly(anomaly);

            _dbContext.Alerts.Add(alert);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Successfully aggregated {Count} anomalies into alert {AlertId}",
                anomalyReferences.Count(), alert.AlertId);

            return Result<AlertNotification>.Ok(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aggregating anomalies");
            return Result<AlertNotification>.Fail($"Error aggregating anomalies: {ex.Message}");
        }
    }

    public async Task<Result<AlertStatisticsDto>> GetAlertStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Calculating alert statistics for tenant {TenantId}", _tenantContext.TenantId);

            var alerts = await _dbContext.Alerts.ToListAsync();

            var stats = new AlertStatisticsDto(
                TotalAlerts: alerts.Count,
                NewAlerts: alerts.Count(a => a.Status == AlertStatus.New),
                AcknowledgedAlerts: alerts.Count(a => a.Status == AlertStatus.Acknowledged),
                ResolvedAlerts: alerts.Count(a => a.Status == AlertStatus.Resolved),
                DismissedAlerts: alerts.Count(a => a.Status == AlertStatus.Dismissed),
                CriticalAlerts: alerts.Count(a => a.Severity == AnomalySeverity.Critical),
                HighAlerts: alerts.Count(a => a.Severity == AnomalySeverity.High),
                MediumAlerts: alerts.Count(a => a.Severity == AnomalySeverity.Medium),
                LowAlerts: alerts.Count(a => a.Severity == AnomalySeverity.Low),
                StatisticsGeneratedAt: DateTime.UtcNow);

            return Result<AlertStatisticsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating alert statistics");
            return Result<AlertStatisticsDto>.Fail($"Error calculating statistics: {ex.Message}");
        }
    }

    public async Task<Result<int>> AutoDismissOldAlertsAsync()
    {
        try
        {
            _logger.LogInformation("Auto-dismissing old alerts for tenant {TenantId}", _tenantContext.TenantId);

            var now = DateTime.UtcNow;
            var alertsToUpdate = _dbContext.Alerts
                .Where(a => a.Status == AlertStatus.New && a.AutoDismissHours > 0 &&
                    a.CreatedAt.AddHours(a.AutoDismissHours) <= now)
                .ToList();

            foreach (var alert in alertsToUpdate)
                alert.Dismiss();

            if (alertsToUpdate.Count > 0)
            {
                _dbContext.Alerts.UpdateRange(alertsToUpdate);
                await _dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Auto-dismissed {Count} alerts", alertsToUpdate.Count);
            return Result<int>.Ok(alertsToUpdate.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-dismissing alerts");
            return Result<int>.Fail($"Error auto-dismissing alerts: {ex.Message}");
        }
    }

    public async Task<Result<AlertThreshold>> UpdateThresholdAsync(
        decimal minConfidence,
        AnomalySeverity triggerSeverity,
        int delayHours)
    {
        try
        {
            _logger.LogInformation(
                "Updating alert threshold for tenant {TenantId}",
                _tenantContext.TenantId);

            if (minConfidence < 0 || minConfidence > 100)
                return Result<AlertThreshold>.Fail("Confidence must be between 0 and 100");

            if (delayHours < 0 || delayHours > 168)
                return Result<AlertThreshold>.Fail("Delay hours must be between 0 and 168");

            var threshold = AlertThreshold.Create(minConfidence, triggerSeverity, delayHours);

            // TODO: Store threshold configuration in database (Phase 4.2.3)
            return Result<AlertThreshold>.Ok(threshold);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating alert threshold");
            return Result<AlertThreshold>.Fail($"Error updating threshold: {ex.Message}");
        }
    }

    public async Task<Result<AlertThreshold>> GetThresholdAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving alert threshold for tenant {TenantId}", _tenantContext.TenantId);

            // TODO: Retrieve threshold configuration from database (Phase 4.2.3)
            // For now, return default balanced threshold
            return Result<AlertThreshold>.Ok(AlertThreshold.Balanced);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert threshold");
            return Result<AlertThreshold>.Fail($"Error retrieving threshold: {ex.Message}");
        }
    }
}
