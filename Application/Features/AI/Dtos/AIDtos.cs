namespace AiCFO.Application.Features.AI.Dtos;

using AiCFO.Application.Common;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Result of anomaly analysis command.
/// </summary>
public record AnomalyAnalysisResultDto(
    int TotalAnomalies,
    List<AnomalyDto> Anomalies,
    decimal AverageConfidence,
    DateTime AnalyzedAt,
    string Message);

/// <summary>
/// Result of health assessment command.
/// </summary>
public record HealthAssessmentResultDto(
    decimal OverallScore,
    string Rating,
    ComprehensiveHealthScoreDto HealthDetails,
    DateTime AssessedAt,
    string Message);

/// <summary>
/// Result of predictions generation command.
/// </summary>
public record PredictionResultDto(
    ComprehensiveForecastDto Forecast,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime GeneratedAt,
    string Message);

/// <summary>
/// Result of recommendations generation command.
/// </summary>
public record RecommendationResultDto(
    int TotalCount,
    List<AIRecommendation> Recommendations,
    DateTime GeneratedAt,
    string Message);

/// <summary>
/// Comprehensive AI dashboard combining all AI insights.
/// </summary>
public record AIDashboardDto(
    DateTime GeneratedAt,
    int AnomalyCount,
    List<AnomalyDto> TopAnomalies,
    decimal HealthScore,
    string HealthRating,
    ComprehensiveForecastDto? Forecast,
    List<AIRecommendation> TopRecommendations,
    AnomalyStatsDto? AnomalyStats);
