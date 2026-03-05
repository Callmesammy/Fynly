namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using AiCFO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// EF Core implementation of health report service.
/// Provides persistence operations for health reports including generation, scheduling, and distribution.
/// </summary>
public class HealthReportService : IHealthReportService
{
    private readonly AppDbContext _context;
    private readonly IHealthScoreService _healthScoreService;
    private readonly IRecommendationService _recommendationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<HealthReportService> _logger;

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public HealthReportService(
        AppDbContext context,
        IHealthScoreService healthScoreService,
        IRecommendationService recommendationService,
        ITenantContext tenantContext,
        ILogger<HealthReportService> logger)
    {
        _context = context;
        _healthScoreService = healthScoreService;
        _recommendationService = recommendationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new health report from current financial health scores.
    /// </summary>
    public async Task<Result<HealthReportDetailDto>> GenerateReportAsync(
        Guid tenantId,
        ReportType reportType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating health report for tenant {TenantId}, type: {ReportType}", tenantId, reportType);

            // Get current health scores
            var healthResult = await _healthScoreService.GetComprehensiveHealthAsync();
            if (!healthResult.IsSuccess)
            {
                _logger.LogError("Failed to retrieve health scores for report generation");
                return Result<HealthReportDetailDto>.Fail(healthResult.Error);
            }

            var healthDto = healthResult.Value;

            // Get recommendations
            var recommendationsResult = await _recommendationService.GetUrgentRecommendationsAsync();

            var recommendations = recommendationsResult.IsSuccess ? recommendationsResult.Value : new List<AIRecommendation>();
            var criticalCount = recommendations.Count(r => r.Priority == RecommendationPriority.Critical);
            var highCount = recommendations.Count(r => r.Priority == RecommendationPriority.High);

            // Get recent anomalies count - default to 0
            int anomalyCount = 0;

            // Extract scores from DTOs - default to 0 if not available
            decimal liquidityScore = healthDto.Liquidity?.Score ?? 0m;
            decimal profitabilityScore = healthDto.Profitability?.Score ?? 0m;
            decimal solvencyScore = healthDto.Solvency?.Score ?? 0m;
            decimal efficiencyScore = healthDto.Efficiency?.Score ?? 0m;
            decimal growthScore = healthDto.Growth?.Score ?? 0m;

            // Get overall score
            decimal overallScore = healthDto.OverallScore?.Score ?? 0m;

            // Determine rating based on overall score
            string rating = DetermineRating(overallScore);

            // Create report entity
            var report = HealthReport.Create(
                tenantId: tenantId,
                createdBy: _tenantContext.UserId,
                reportType: reportType,
                overallScore: overallScore,
                rating: rating,
                liquidityScore: liquidityScore,
                profitabilityScore: profitabilityScore,
                solvencyScore: solvencyScore,
                efficiencyScore: efficiencyScore,
                growthScore: growthScore,
                criticalRecommendations: criticalCount,
                highRecommendations: highCount,
                anomalies: anomalyCount);

            // Set summary
            var summary = GenerateSummary(healthDto, criticalCount, highCount);
            report.SetSummary(summary);

            // Save to database
            await _context.Reports.AddAsync(report, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Health report generated successfully: {ReportId}", report.ReportId);

            return Result<HealthReportDetailDto>.Ok(MapToDetailDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health report for tenant {TenantId}", tenantId);
            return Result<HealthReportDetailDto>.Fail($"Error generating health report: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieve a specific health report by ID.
    /// </summary>
    public async Task<Result<HealthReportDetailDto>> GetReportAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving health report {ReportId} for tenant {TenantId}", reportId, tenantId);

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == reportId, cancellationToken);

            if (report == null)
            {
                _logger.LogWarning("Health report not found: {ReportId}", reportId);
                return Result<HealthReportDetailDto>.Fail("Health report not found");
            }

            return Result<HealthReportDetailDto>.Ok(MapToDetailDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health report {ReportId}", reportId);
            return Result<HealthReportDetailDto>.Fail($"Error retrieving health report: {ex.Message}");
        }
    }

    /// <summary>
    /// List all health reports with optional filtering.
    /// </summary>
    public async Task<Result<List<HealthReportSummaryDto>>> ListReportsAsync(
        Guid tenantId,
        ReportStatus? statusFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Listing health reports for tenant {TenantId}", tenantId);

            var query = _context.Reports
                .Where(r => r.TenantId == tenantId)
                .AsQueryable();

            if (statusFilter.HasValue)
                query = query.Where(r => r.Status == statusFilter.Value);

            if (fromDate.HasValue)
                query = query.Where(r => r.GeneratedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.GeneratedAt <= toDate.Value);

            var reports = await query
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync(cancellationToken);

            var summaries = reports.Select(MapToSummaryDto).ToList();

            _logger.LogInformation("Retrieved {Count} health reports for tenant {TenantId}", summaries.Count, tenantId);
            return Result<List<HealthReportSummaryDto>>.Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing health reports for tenant {TenantId}", tenantId);
            return Result<List<HealthReportSummaryDto>>.Fail($"Error listing health reports: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedule a report for recurring delivery.
    /// </summary>
    public async Task<Result<HealthReportDetailDto>> ScheduleReportAsync(
        Guid tenantId,
        Guid reportId,
        ScheduleReportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling health report {ReportId} for tenant {TenantId}", reportId, tenantId);

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == reportId, cancellationToken);

            if (report == null)
                return Result<HealthReportDetailDto>.Fail("Health report not found");

            report.Schedule(request.ScheduledFor, request.Frequency);
            if (!string.IsNullOrWhiteSpace(request.Recipients))
                report.SetSummary($"Scheduled for {request.Recipients}"); // Store recipients info

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Health report scheduled successfully: {ReportId}", reportId);
            return Result<HealthReportDetailDto>.Ok(MapToDetailDto(report));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling health report {ReportId}", reportId);
            return Result<HealthReportDetailDto>.Fail($"Error scheduling health report: {ex.Message}");
        }
    }

    /// <summary>
    /// Send a report to specified recipients.
    /// </summary>
    public async Task<Result<bool>> SendReportAsync(
        Guid tenantId,
        Guid reportId,
        string recipients,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending health report {ReportId} to recipients: {Recipients}", reportId, recipients);

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == reportId, cancellationToken);

            if (report == null)
                return Result<bool>.Fail("Health report not found");

            report.MarkAsSent(recipients);

            // TODO: Implement actual email sending via notification service
            _logger.LogInformation("Health report marked as sent: {ReportId}", reportId);

            await _context.SaveChangesAsync(cancellationToken);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending health report {ReportId}", reportId);
            return Result<bool>.Fail($"Error sending health report: {ex.Message}");
        }
    }

    /// <summary>
    /// Archive a report (for retention/cleanup).
    /// </summary>
    public async Task<Result<bool>> ArchiveReportAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Archiving health report {ReportId}", reportId);

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Id == reportId, cancellationToken);

            if (report == null)
                return Result<bool>.Fail("Health report not found");

            report.Archive(expirationDays: 90);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Health report archived: {ReportId}", reportId);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving health report {ReportId}", reportId);
            return Result<bool>.Fail($"Error archiving health report: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all scheduled reports for the tenant.
    /// </summary>
    public async Task<Result<List<ScheduledReportDto>>> GetScheduledReportsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving scheduled reports for tenant {TenantId}", tenantId);

            var reports = await _context.Reports
                .Where(r => r.TenantId == tenantId && r.Status == ReportStatus.Scheduled)
                .OrderBy(r => r.ScheduledFor)
                .ToListAsync(cancellationToken);

            var scheduledReports = reports
                .Select(r => new ScheduledReportDto
                {
                    ReportId = r.ReportId,
                    ReportType = r.ReportType,
                    ScheduledFor = r.ScheduledFor ?? DateTime.UtcNow,
                    Frequency = r.Frequency ?? ReportFrequency.OnDemand,
                    Recipients = r.Recipients,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} scheduled reports", scheduledReports.Count);
            return Result<List<ScheduledReportDto>>.Ok(scheduledReports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled reports");
            return Result<List<ScheduledReportDto>>.Fail($"Error retrieving scheduled reports: {ex.Message}");
        }
    }

    /// <summary>
    /// Get aggregated health report statistics.
    /// </summary>
    public async Task<Result<HealthReportStatisticsDto>> GetReportStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating health report statistics for tenant {TenantId}", tenantId);

            var query = _context.Reports
                .Where(r => r.TenantId == tenantId)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(r => r.GeneratedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(r => r.GeneratedAt <= toDate.Value);

            var reports = await query.ToListAsync(cancellationToken);

            if (!reports.Any())
            {
                return Result<HealthReportStatisticsDto>.Ok(new HealthReportStatisticsDto
                {
                    TotalReportsGenerated = 0,
                    AnalysisPeriodStart = fromDate,
                    AnalysisPeriodEnd = toDate
                });
            }

            var stats = new HealthReportStatisticsDto
            {
                TotalReportsGenerated = reports.Count,
                AverageOverallScore = reports.Average(r => r.OverallScore),
                AverageLiquidityScore = reports.Average(r => r.LiquidityScore),
                AverageProfitabilityScore = reports.Average(r => r.ProfitabilityScore),
                AverageSolvencyScore = reports.Average(r => r.SolvencyScore),
                AverageEfficiencyScore = reports.Average(r => r.EfficiencyScore),
                AverageGrowthScore = reports.Average(r => r.GrowthScore),
                HighestScore = reports.Max(r => r.OverallScore),
                LowestScore = reports.Min(r => r.OverallScore),
                MostCommonRating = reports.GroupBy(r => r.Rating)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? "Unknown",
                ScheduledReportsCount = reports.Count(r => r.Status == ReportStatus.Scheduled),
                AnalysisPeriodStart = fromDate,
                AnalysisPeriodEnd = toDate
            };

            _logger.LogInformation("Statistics calculated: Average score = {AverageScore}", stats.AverageOverallScore);
            return Result<HealthReportStatisticsDto>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating health report statistics");
            return Result<HealthReportStatisticsDto>.Fail($"Error calculating statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to determine rating from score.
    /// </summary>
    private static string DetermineRating(decimal score)
    {
        return score switch
        {
            >= 80m => "Excellent",
            >= 60m => "Good",
            >= 40m => "Fair",
            _ => "Poor"
        };
    }

    /// <summary>
    /// Helper method to generate summary text.
    /// </summary>
    private static string GenerateSummary(
        ComprehensiveHealthScoreDto health,
        int criticalRecommendations,
        int highRecommendations)
    {
        var overallScore = health.OverallScore?.Score ?? 0m;
        var liquidityScore = health.Liquidity?.Score ?? 0m;
        var profitabilityScore = health.Profitability?.Score ?? 0m;
        var solvencyScore = health.Solvency?.Score ?? 0m;
        var efficiencyScore = health.Efficiency?.Score ?? 0m;
        var growthScore = health.Growth?.Score ?? 0m;

        var summary = $"Overall Score: {overallScore:F1}. ";
        summary += $"Liquidity: {liquidityScore:F1}, ";
        summary += $"Profitability: {profitabilityScore:F1}, ";
        summary += $"Solvency: {solvencyScore:F1}, ";
        summary += $"Efficiency: {efficiencyScore:F1}, ";
        summary += $"Growth: {growthScore:F1}. ";

        if (criticalRecommendations > 0)
            summary += $"Critical items: {criticalRecommendations}. ";

        if (highRecommendations > 0)
            summary += $"High priority items: {highRecommendations}.";

        return summary;
    }

    /// <summary>
    /// Helper method to map entity to summary DTO.
    /// </summary>
    private static HealthReportSummaryDto MapToSummaryDto(HealthReport report)
    {
        return new HealthReportSummaryDto
        {
            ReportId = report.ReportId,
            ReportType = report.ReportType,
            Status = report.Status,
            OverallScore = report.OverallScore,
            Rating = report.Rating,
            GeneratedAt = report.GeneratedAt,
            SentAt = report.SentAt
        };
    }

    /// <summary>
    /// Helper method to map entity to detail DTO.
    /// </summary>
    private static HealthReportDetailDto MapToDetailDto(HealthReport report)
    {
        return new HealthReportDetailDto
        {
            ReportId = report.ReportId,
            ReportType = report.ReportType,
            Status = report.Status,
            OverallScore = report.OverallScore,
            Rating = report.Rating,
            LiquidityScore = report.LiquidityScore,
            ProfitabilityScore = report.ProfitabilityScore,
            SolvencyScore = report.SolvencyScore,
            EfficiencyScore = report.EfficiencyScore,
            GrowthScore = report.GrowthScore,
            CriticalRecommendationsCount = report.CriticalRecommendationsCount,
            HighRecommendationsCount = report.HighRecommendationsCount,
            AnomaliesDetected = report.AnomaliesDetected,
            GeneratedAt = report.GeneratedAt,
            ScheduledFor = report.ScheduledFor,
            SentAt = report.SentAt,
            Frequency = report.Frequency,
            Recipients = report.Recipients,
            Summary = report.Summary
        };
    }
}
