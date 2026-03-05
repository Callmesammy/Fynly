namespace AiCFO.Infrastructure.Jobs;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using Microsoft.Extensions.Logging;

/// <summary>
/// Background job for generating and sending scheduled health reports.
/// Integrates with Hangfire for recurring execution.
/// </summary>
public class ScheduledHealthReportJob
{
    private readonly IHealthReportService _healthReportService;
    private readonly IHealthScoreService _healthScoreService;
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<ScheduledHealthReportJob> _logger;

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public ScheduledHealthReportJob(
        IHealthReportService healthReportService,
        IHealthScoreService healthScoreService,
        IRecommendationService recommendationService,
        ILogger<ScheduledHealthReportJob> logger)
    {
        _healthReportService = healthReportService;
        _healthScoreService = healthScoreService;
        _recommendationService = recommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Generate and send a health report for a specific tenant.
    /// Scheduled to run at configured intervals (daily, weekly, monthly, etc.).
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting scheduled health report job for tenant {TenantId}", tenantId);

            // Get current health scores
            var healthResult = await _healthScoreService.GetComprehensiveHealthAsync(tenantId, cancellationToken);
            if (!healthResult.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve health scores for tenant {TenantId}: {Error}", tenantId, healthResult.Error);
                return;
            }

            // Get urgent recommendations
            var recommendationsResult = await _recommendationService.GetUrgentRecommendationsAsync(
                tenantId,
                topCount: 10,
                cancellationToken: cancellationToken);

            var recommendations = recommendationsResult.IsSuccess ? recommendationsResult.Value : new List<RecommendationDto>();

            // Generate report
            var reportResult = await _healthReportService.GenerateReportAsync(
                tenantId,
                ReportType.Overall,
                cancellationToken);

            if (!reportResult.IsSuccess)
            {
                _logger.LogError("Failed to generate health report for tenant {TenantId}: {Error}", tenantId, reportResult.Error);
                return;
            }

            var report = reportResult.Value;
            _logger.LogInformation(
                "Health report generated successfully for tenant {TenantId}, report ID: {ReportId}, score: {OverallScore}",
                tenantId,
                report.ReportId,
                report.OverallScore);

            // Log job completion
            _logger.LogInformation(
                "Scheduled health report job completed for tenant {TenantId} - Generated report with score: {Score}, Rating: {Rating}",
                tenantId,
                report.OverallScore,
                report.Rating);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled health report job for tenant {TenantId}", tenantId);
        }
    }
}
