using AiCFO.Domain.ValueObjects;

namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for AI-powered financial recommendations.
/// Generates actionable insights and suggestions based on financial analysis.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Generate recommendations based on detected anomalies.
    /// Prioritizes by severity and potential impact.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateAnomalyRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Generate recommendations based on predictions.
    /// Suggests actions to address forecasted challenges or opportunities.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GeneratePredictionRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Generate recommendations to improve financial health.
    /// Targeted at weaknesses identified in health score.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateHealthImprovementRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Generate cost optimization recommendations.
    /// Analyzes expenses and identifies reduction opportunities.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateCostOptimizationRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Generate revenue growth recommendations.
    /// Analyzes income patterns and suggests expansion opportunities.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateRevenueGrowthRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Generate cash flow optimization recommendations.
    /// Suggests timing and strategy improvements for liquidity.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateCashFlowRecommendationsAsync(
        int topCount = 5);

    /// <summary>
    /// Get all active recommendations across all categories.
    /// Sorted by priority and recency.
    /// </summary>
    Task<Result<List<RecommendationDto>>> GetAllRecommendationsAsync(
        RecommendationPriority? priorityFilter = null);

    /// <summary>
    /// Get recommendations relevant to a specific account.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GetAccountRecommendationsAsync(
        Guid accountId);

    /// <summary>
    /// Get critical/urgent recommendations (Priority = Critical).
    /// Requires immediate attention.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GetUrgentRecommendationsAsync();

    /// <summary>
    /// Track recommendation acceptance/implementation.
    /// Marks recommendation as viewed/acknowledged.
    /// </summary>
    Task<Result<bool>> AcknowledgeRecommendationAsync(
        Guid recommendationId,
        string? notes = null);

    /// <summary>
    /// Track recommendation dismissal.
    /// Records why user declined recommendation (helps ML improve).
    /// </summary>
    Task<Result<bool>> DismissRecommendationAsync(
        Guid recommendationId,
        string reason);

    /// <summary>
    /// Record successful implementation of recommendation.
    /// Validates and confirms recommendation was acted upon.
    /// </summary>
    Task<Result<bool>> MarkAsImplementedAsync(
        Guid recommendationId,
        string? outcomes = null);

    /// <summary>
    /// Generate personalized dashboard recommendations.
    /// Limited set (3-5) for home page display.
    /// </summary>
    Task<Result<List<AIRecommendation>>> GenerateDashboardRecommendationsAsync(
        int count = 5);

    /// <summary>
    /// Get recommendation statistics and effectiveness metrics.
    /// Tracks acceptance rate, implementation rate, impact achieved.
    /// </summary>
    Task<Result<RecommendationStatsDto>> GetRecommendationStatsAsync();

    /// <summary>
    /// Export recommendations report.
    /// Generates formatted report for stakeholders.
    /// </summary>
    Task<Result<RecommendationReportDto>> GenerateRecommendationReportAsync(
        DateTime periodStart,
        DateTime periodEnd);
}

/// <summary>
/// Recommendation data transfer object.
/// </summary>
public record RecommendationDto(
    Guid Id,
    AIRecommendation Recommendation,
    string Category,                 // "Anomaly" | "Prediction" | "Health" | "CostOptimization" | "Revenue" | "CashFlow"
    RecommendationStatus Status,     // Active | Acknowledged | Dismissed | Implemented
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    string? RelatedEntityId,
    int ViewCount);

/// <summary>
/// Recommendation status tracking.
/// </summary>
public enum RecommendationStatus
{
    Active,
    Acknowledged,
    Dismissed,
    Implemented,
    Expired
}

/// <summary>
/// Recommendation statistics.
/// </summary>
public record RecommendationStatsDto(
    int TotalRecommendations,
    int ActiveCount,
    int AcknowledgedCount,
    int ImplementedCount,
    int DismissedCount,
    decimal AcceptanceRate,          // % of recommendations viewed/acknowledged
    decimal ImplementationRate,      // % of recommendations actually implemented
    decimal AverageTimeToImplement,  // Days from creation to implementation
    RecommendationCategoryStatsDto ByCategory);

/// <summary>
/// Recommendations grouped by category.
/// </summary>
public record RecommendationCategoryStatsDto(
    int AnomalyCount,
    int PredictionCount,
    int HealthCount,
    int CostOptimizationCount,
    int RevenueCount,
    int CashFlowCount);

/// <summary>
/// Comprehensive recommendations report.
/// </summary>
public record RecommendationReportDto(
    DateTime ReportDate,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    List<RecommendationDto> AllRecommendations,
    List<ImplementedRecommationDto> ImplementedRecommendations,
    RecommendationStatsDto Stats,
    List<ImpactAssessmentDto> ImpactAssessments,
    string ExecutiveSummary);

/// <summary>
/// Implemented recommendation with outcomes.
/// </summary>
public record ImplementedRecommationDto(
    Guid RecommendationId,
    string Title,
    DateTime ImplementedDate,
    string EstimatedImpact,
    string ActualOutcome,
    decimal MeasuredBenefit,
    string BenefitUnit); // "USD" | "%" | "Days" | etc.

/// <summary>
/// Impact assessment of implemented recommendations.
    /// </summary>
public record ImpactAssessmentDto(
    string Category,
    decimal TotalEstimatedImpact,
    decimal TotalActualImpact,
    decimal AccuracyOfEstimates,    // How close were estimates to actual?
    string PerformanceNote);
