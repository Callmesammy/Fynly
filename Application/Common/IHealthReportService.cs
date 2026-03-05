namespace AiCFO.Application.Common;

using AiCFO.Domain.Entities;

/// <summary>
/// Service abstraction for health report operations.
/// Handles report generation, scheduling, distribution, and analysis.
/// </summary>
public interface IHealthReportService
{
    /// <summary>
    /// Generate a new health report from current financial health scores.
    /// </summary>
    Task<Result<HealthReportDetailDto>> GenerateReportAsync(
        Guid tenantId,
        ReportType reportType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a specific health report by ID.
    /// </summary>
    Task<Result<HealthReportDetailDto>> GetReportAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all health reports with optional filtering.
    /// </summary>
    Task<Result<List<HealthReportSummaryDto>>> ListReportsAsync(
        Guid tenantId,
        ReportStatus? statusFilter = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedule a report for recurring delivery.
    /// </summary>
    Task<Result<HealthReportDetailDto>> ScheduleReportAsync(
        Guid tenantId,
        Guid reportId,
        ScheduleReportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a report to specified recipients.
    /// </summary>
    Task<Result<bool>> SendReportAsync(
        Guid tenantId,
        Guid reportId,
        string recipients,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive a report (for retention/cleanup).
    /// </summary>
    Task<Result<bool>> ArchiveReportAsync(
        Guid tenantId,
        Guid reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all scheduled reports for the tenant.
    /// </summary>
    Task<Result<List<ScheduledReportDto>>> GetScheduledReportsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get aggregated health report statistics.
    /// </summary>
    Task<Result<HealthReportStatisticsDto>> GetReportStatisticsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request DTO for generating a new health report.
/// </summary>
public class GenerateReportRequest
{
    /// <summary>
    /// Type of report to generate (Overall, ByDimension, Trend, Executive).
    /// </summary>
    public ReportType ReportType { get; set; }
}

/// <summary>
/// Request DTO for scheduling a report.
/// </summary>
public class ScheduleReportRequest
{
    /// <summary>
    /// When to schedule the report for delivery.
    /// </summary>
    public DateTime ScheduledFor { get; set; }

    /// <summary>
    /// Frequency of recurring delivery.
    /// </summary>
    public ReportFrequency Frequency { get; set; }

    /// <summary>
    /// Email recipients (comma-separated or JSON array).
    /// </summary>
    public string? Recipients { get; set; }
}

/// <summary>
/// Summary DTO for listing health reports (minimal fields).
/// </summary>
public class HealthReportSummaryDto
{
    /// <summary>
    /// Unique report ID.
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Report type.
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Current report status.
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Overall health score.
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Health rating (e.g., "Excellent", "Good", "Fair", "Poor").
    /// </summary>
    public string Rating { get; set; } = string.Empty;

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When the report was sent (if applicable).
    /// </summary>
    public DateTime? SentAt { get; set; }
}

/// <summary>
/// Detailed DTO for health report with all dimensions and recommendations.
/// </summary>
public class HealthReportDetailDto
{
    /// <summary>
    /// Unique report ID.
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Report type.
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Current report status.
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Overall health score (0-100).
    /// </summary>
    public decimal OverallScore { get; set; }

    /// <summary>
    /// Overall rating.
    /// </summary>
    public string Rating { get; set; } = string.Empty;

    /// <summary>
    /// Liquidity score (0-100).
    /// </summary>
    public decimal LiquidityScore { get; set; }

    /// <summary>
    /// Profitability score (0-100).
    /// </summary>
    public decimal ProfitabilityScore { get; set; }

    /// <summary>
    /// Solvency score (0-100).
    /// </summary>
    public decimal SolvencyScore { get; set; }

    /// <summary>
    /// Efficiency score (0-100).
    /// </summary>
    public decimal EfficiencyScore { get; set; }

    /// <summary>
    /// Growth score (0-100).
    /// </summary>
    public decimal GrowthScore { get; set; }

    /// <summary>
    /// Count of critical recommendations.
    /// </summary>
    public int CriticalRecommendationsCount { get; set; }

    /// <summary>
    /// Count of high-priority recommendations.
    /// </summary>
    public int HighRecommendationsCount { get; set; }

    /// <summary>
    /// Total anomalies detected.
    /// </summary>
    public int AnomaliesDetected { get; set; }

    /// <summary>
    /// When the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// When scheduled for delivery (if applicable).
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// When actually sent (if applicable).
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Recurring frequency (if scheduled).
    /// </summary>
    public ReportFrequency? Frequency { get; set; }

    /// <summary>
    /// Recipients list.
    /// </summary>
    public string? Recipients { get; set; }

    /// <summary>
    /// Report summary/highlights.
    /// </summary>
    public string? Summary { get; set; }
}

/// <summary>
/// DTO for scheduled report information.
/// </summary>
public class ScheduledReportDto
{
    /// <summary>
    /// Unique report ID.
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Report type.
    /// </summary>
    public ReportType ReportType { get; set; }

    /// <summary>
    /// Next scheduled delivery time.
    /// </summary>
    public DateTime ScheduledFor { get; set; }

    /// <summary>
    /// Recurring frequency.
    /// </summary>
    public ReportFrequency Frequency { get; set; }

    /// <summary>
    /// Recipients for delivery.
    /// </summary>
    public string? Recipients { get; set; }

    /// <summary>
    /// When the schedule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the schedule was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO for aggregated health report statistics.
/// </summary>
public class HealthReportStatisticsDto
{
    /// <summary>
    /// Total number of reports generated.
    /// </summary>
    public int TotalReportsGenerated { get; set; }

    /// <summary>
    /// Average overall health score across all reports.
    /// </summary>
    public decimal AverageOverallScore { get; set; }

    /// <summary>
    /// Average liquidity score.
    /// </summary>
    public decimal AverageLiquidityScore { get; set; }

    /// <summary>
    /// Average profitability score.
    /// </summary>
    public decimal AverageProfitabilityScore { get; set; }

    /// <summary>
    /// Average solvency score.
    /// </summary>
    public decimal AverageSolvencyScore { get; set; }

    /// <summary>
    /// Average efficiency score.
    /// </summary>
    public decimal AverageEfficiencyScore { get; set; }

    /// <summary>
    /// Average growth score.
    /// </summary>
    public decimal AverageGrowthScore { get; set; }

    /// <summary>
    /// Highest overall score recorded.
    /// </summary>
    public decimal HighestScore { get; set; }

    /// <summary>
    /// Lowest overall score recorded.
    /// </summary>
    public decimal LowestScore { get; set; }

    /// <summary>
    /// Most common rating.
    /// </summary>
    public string MostCommonRating { get; set; } = string.Empty;

    /// <summary>
    /// Number of reports currently scheduled.
    /// </summary>
    public int ScheduledReportsCount { get; set; }

    /// <summary>
    /// Date range analyzed.
    /// </summary>
    public DateTime? AnalysisPeriodStart { get; set; }

    /// <summary>
    /// Date range analyzed.
    /// </summary>
    public DateTime? AnalysisPeriodEnd { get; set; }
}
