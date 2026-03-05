namespace AiCFO.Infrastructure.Jobs;

using AiCFO.Application.Common;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Background job for recurring anomaly detection and alerting.
/// Scheduled to run at configured intervals (e.g., daily at 2 AM).
/// Scans for anomalies and automatically creates alerts for detected issues.
/// </summary>
public class RecurringAnomalyDetectionJob
{
    private readonly IAnomalyDetectionService _anomalyDetectionService;
    private readonly IAlertService _alertService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RecurringAnomalyDetectionJob> _logger;

    public RecurringAnomalyDetectionJob(
        IAnomalyDetectionService anomalyDetectionService,
        IAlertService alertService,
        ITenantContext tenantContext,
        ILogger<RecurringAnomalyDetectionJob> logger)
    {
        _anomalyDetectionService = anomalyDetectionService ?? throw new ArgumentNullException(nameof(anomalyDetectionService));
        _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute anomaly detection scan and create alerts for detected anomalies.
    /// This method is called by Hangfire at scheduled intervals.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Starting RecurringAnomalyDetectionJob for tenant {TenantId} at {ExecutionTime}",
                _tenantContext.TenantId, DateTime.UtcNow);

            // Scan for recent anomalies with medium and above severity
            var anomalyResult = await _anomalyDetectionService.ScanUnmatchedTransactionsAsync(
                minSeverity: AnomalySeverity.Medium);

            if (!anomalyResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Anomaly detection failed for tenant {TenantId}: {Error}",
                    _tenantContext.TenantId, anomalyResult.Error);
                return;
            }

            var anomalies = anomalyResult.Value;
            if (anomalies.Count == 0)
            {
                _logger.LogInformation("No anomalies detected for tenant {TenantId}", _tenantContext.TenantId);
                return;
            }

            _logger.LogInformation(
                "Detected {Count} anomalies for tenant {TenantId}",
                anomalies.Count, _tenantContext.TenantId);

            // Create alerts for each detected anomaly
            var createdAlerts = 0;
            var failedAlerts = 0;

            foreach (var anomaly in anomalies)
            {
                try
                {
                    var message = $"Anomaly detected: {anomaly.EntityType}. " +
                        $"Amount: {anomaly.Amount}. Confidence: {anomaly.Score.ConfidencePercentage:P}";

                    var result = await _alertService.CreateAlertAsync(
                        anomalyReference: anomaly.Id.ToString(),
                        severity: anomaly.Score.Severity,
                        confidenceScore: anomaly.Score.ConfidencePercentage,
                        message: message,
                        autoDismissHours: anomaly.Score.Severity == AnomalySeverity.Critical ? 0 : 24);

                    if (result.IsSuccess)
                    {
                        createdAlerts++;
                        _logger.LogInformation("Created alert {AlertId} for anomaly {AnomalyId}",
                            result.Value.AlertId, anomaly.Id);
                    }
                    else
                    {
                        failedAlerts++;
                        _logger.LogWarning("Failed to create alert for anomaly {AnomalyId}: {Error}",
                            anomaly.Id, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    failedAlerts++;
                    _logger.LogError(ex, "Exception creating alert for anomaly {AnomalyId}", anomaly.Id);
                }
            }

            _logger.LogInformation(
                "RecurringAnomalyDetectionJob completed for tenant {TenantId}. " +
                "Created {CreatedAlerts} alerts, {FailedAlerts} failed",
                _tenantContext.TenantId, createdAlerts, failedAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error in RecurringAnomalyDetectionJob for tenant {TenantId}",
                _tenantContext.TenantId);
        }
    }
}
