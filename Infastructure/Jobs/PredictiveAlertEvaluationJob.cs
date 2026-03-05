namespace AiCFO.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for recurring predictive alert threshold evaluation.
/// Runs periodically (configurable) to evaluate all active thresholds and generate alerts.
/// </summary>
public class PredictiveAlertEvaluationJob
{
    private readonly IPredictiveAlertService _service;
    private readonly ILogger<PredictiveAlertEvaluationJob> _logger;

    public PredictiveAlertEvaluationJob(
        IPredictiveAlertService service,
        ILogger<PredictiveAlertEvaluationJob> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Executes threshold evaluation for a specific tenant.
    /// Called by Hangfire background job scheduler.
    /// </summary>
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting predictive alert evaluation job for tenant: {TenantId}", tenantId);

            var result = await _service.EvaluateThresholdsAsync(tenantId, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Predictive alert evaluation completed. Alerts triggered: {AlertCount}",
                    result.Value?.Count ?? 0);
            }
            else
            {
                _logger.LogError("Predictive alert evaluation failed: {Error}", result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in predictive alert evaluation job for tenant: {TenantId}", tenantId);
        }
    }
}
