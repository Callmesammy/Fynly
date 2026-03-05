using AiCFO.Domain.ValueObjects;

namespace AiCFO.Application.Common;

/// <summary>
/// Service abstraction for comprehensive financial health assessment.
/// Evaluates overall financial position using multiple dimensions.
/// </summary>
public interface IHealthScoreService
{
    /// <summary>
    /// Calculate overall financial health score (0-100).
    /// Considers liquidity, profitability, solvency, efficiency.
    /// </summary>
    Task<Result<FinancialHealthScore>> CalculateOverallHealthAsync();

    /// <summary>
    /// Calculate liquidity health dimension.
    /// Evaluates cash availability and short-term obligations.
    /// </summary>
    Task<Result<DimensionHealthScoreDto>> CalculateLiquidityHealthAsync();

    /// <summary>
    /// Calculate profitability health dimension.
    /// Evaluates revenue, expenses, and profit margins.
    /// </summary>
    Task<Result<DimensionHealthScoreDto>> CalculateProfitabilityHealthAsync();

    /// <summary>
    /// Calculate solvency health dimension.
    /// Evaluates debt levels and long-term obligations.
    /// </summary>
    Task<Result<DimensionHealthScoreDto>> CalculateSolvencyHealthAsync();

    /// <summary>
    /// Calculate efficiency health dimension.
    /// Evaluates asset utilization and operational efficiency.
    /// </summary>
    Task<Result<DimensionHealthScoreDto>> CalculateEfficiencyHealthAsync();

    /// <summary>
    /// Calculate growth health dimension.
    /// Evaluates revenue growth and trend trajectories.
    /// </summary>
    Task<Result<DimensionHealthScoreDto>> CalculateGrowthHealthAsync();

    /// <summary>
    /// Get comprehensive multi-dimensional health assessment.
    /// Returns all 5 dimensions plus overall score.
    /// </summary>
    Task<Result<ComprehensiveHealthScoreDto>> GetComprehensiveHealthAsync();

    /// <summary>
    /// Get health score history over time.
    /// Useful for trend analysis and dashboard display.
    /// </summary>
    Task<Result<List<HealthScoreHistoryDto>>> GetHealthHistoryAsync(
        int days = 90);

    /// <summary>
    /// Identify health improvement opportunities.
    /// Prioritized recommendations for strengthening financial position.
    /// </summary>
    Task<Result<List<HealthImprovementDto>>> GetImprovementOpportunitiesAsync();

    /// <summary>
    /// Get financial health alerts and warnings.
    /// Flags critical issues requiring immediate attention.
    /// </summary>
    Task<Result<List<HealthAlertDto>>> GetHealthAlertsAsync(
        HealthAlertSeverity? minSeverity = null);

    /// <summary>
    /// Compare health score to industry benchmarks.
    /// Provides context on relative financial position.
    /// </summary>
    Task<Result<HealthBenchmarkDto>> CompareToIndustryBenchmarkAsync(
        string? industry = null);

    /// <summary>
    /// Drill down into specific dimension.
    /// Returns detailed metrics and key performance indicators.
    /// </summary>
    Task<Result<DimensionDetailDto>> GetDimensionDetailsAsync(
        HealthDimension dimension);

    /// <summary>
    /// Calculate stress test scenarios.
    /// Shows health score impact of various scenarios (20% revenue drop, etc).
    /// </summary>
    Task<Result<List<StressTestResultDto>>> RunStressTestAsync();
}

/// <summary>
/// Health dimension (one of five major categories).
/// </summary>
public enum HealthDimension
{
    Liquidity,
    Profitability,
    Solvency,
    Efficiency,
    Growth
}

/// <summary>
/// Health alert severity levels.
/// </summary>
public enum HealthAlertSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Single dimension health score.
/// </summary>
public record DimensionHealthScoreDto(
    HealthDimension Dimension,
    decimal Score,              // 0-100
    string Rating,              // "Excellent" | "Good" | "Fair" | "Poor" | "Critical"
    List<HealthMetricDto> Metrics,
    List<string> TopConcerns,
    decimal WeightInOverallScore); // This dimension's contribution to overall health

/// <summary>
/// Individual health metric.
/// </summary>
public record HealthMetricDto(
    string MetricName,
    decimal Value,
    string Unit,
    decimal Threshold,          // Target/benchmark
    decimal Variance,           // % difference from threshold
    string Status,              // "Good" | "Warning" | "Critical"
    DateTime LastCalculated);

/// <summary>
/// Comprehensive health assessment across all dimensions.
/// </summary>
public record ComprehensiveHealthScoreDto(
    FinancialHealthScore OverallScore,
    DimensionHealthScoreDto Liquidity,
    DimensionHealthScoreDto Profitability,
    DimensionHealthScoreDto Solvency,
    DimensionHealthScoreDto Efficiency,
    DimensionHealthScoreDto Growth,
    List<HealthAlertDto> ActiveAlerts,
    List<string> TopRecommendations,
    DateTime CalculatedAt,
    DateTime? NextReviewDate);

/// <summary>
/// Health score at a point in time.
/// </summary>
public record HealthScoreHistoryDto(
    DateTime CalculatedAt,
    decimal Score,
    string Rating,
    decimal LiquidityScore,
    decimal ProfitabilityScore,
    decimal SolvencyScore,
    decimal EfficiencyScore,
    decimal GrowthScore);

/// <summary>
/// Opportunity to improve financial health.
/// </summary>
public record HealthImprovementDto(
    string Title,
    string Description,
    HealthDimension ImpactedDimension,
    decimal PotentialScoreImprovement,
    int PriorityRank,
    string ActionItem,
    string Complexity); // "Easy" | "Medium" | "Complex"

/// <summary>
/// Health alert (warning or issue).
/// </summary>
public record HealthAlertDto(
    Guid Id,
    string Title,
    string Description,
    HealthAlertSeverity Severity,
    HealthDimension RelatedDimension,
    string RecommendedAction,
    DateTime AlertedAt,
    bool IsResolved,
    string? ResolutionNotes);

/// <summary>
/// Health benchmark comparison.
/// </summary>
public record HealthBenchmarkDto(
    decimal CompanyScore,
    decimal BenchmarkScore,
    decimal BenchmarkPercentile,   // What % of companies do we exceed
    string ComparisonGrade,        // A+ | A | B | C | D | F
    List<DimensionBenchmarkDto> DimensionComparisons,
    List<string> BestInClassAreas,
    List<string> AreasNeedingImprovement);

/// <summary>
/// Single dimension benchmark comparison.
/// </summary>
public record DimensionBenchmarkDto(
    HealthDimension Dimension,
    decimal CompanyScore,
    decimal BenchmarkScore,
    decimal Variance,
    string Comparison); // "Ahead" | "In line" | "Behind"

/// <summary>
/// Detailed metrics for a specific dimension.
/// </summary>
public record DimensionDetailDto(
    HealthDimension Dimension,
    decimal Score,
    List<HealthMetricDto> DetailedMetrics,
    List<FinancialRatioDto> KeyRatios,
    List<TrendLineDto> TrendAnalysis,
    List<string> Recommendations);

/// <summary>
/// Financial ratio (e.g., Current Ratio, Debt-to-Equity).
/// </summary>
public record FinancialRatioDto(
    string RatioName,
    decimal CurrentRatio,
    decimal BenchmarkRatio,
    string Interpretation);

/// <summary>
/// Trend line data point.
/// </summary>
public record TrendLineDto(
    DateTime Date,
    decimal Value,
    string Direction); // "Up" | "Down" | "Stable"

/// <summary>
/// Stress test scenario result.
/// </summary>
public record StressTestResultDto(
    string Scenario,                // "20% Revenue Decline" | "50% Expense Increase" | etc.
    decimal ImpactScore,
    decimal ProjectedHealthScore,
    string ImpactLevel,             // "Minimal" | "Moderate" | "Severe"
    List<string> MostAffectedAreas,
    string Recommendation);
