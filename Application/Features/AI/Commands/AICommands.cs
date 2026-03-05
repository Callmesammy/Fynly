namespace AiCFO.Application.Features.AI.Commands;

using MediatR;
using AiCFO.Application.Common;
using AiCFO.Application.Features.AI.Dtos;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to trigger anomaly detection analysis on recent transactions.
/// </summary>
public record TriggerAnomalyAnalysisCommand(
    Guid? AccountId = null,
    int LookbackDays = 90,
    AnomalySeverity? MinimumSeverity = null) : IRequest<Result<AnomalyAnalysisResultDto>>;

/// <summary>
/// Handler for TriggerAnomalyAnalysisCommand
/// </summary>
public class TriggerAnomalyAnalysisCommandHandler : IRequestHandler<TriggerAnomalyAnalysisCommand, Result<AnomalyAnalysisResultDto>>
{
    private readonly IAnomalyDetectionService _anomalyDetection;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TriggerAnomalyAnalysisCommandHandler> _logger;

    public TriggerAnomalyAnalysisCommandHandler(
        IAnomalyDetectionService anomalyDetection,
        ITenantContext tenantContext,
        ILogger<TriggerAnomalyAnalysisCommandHandler> logger)
    {
        _anomalyDetection = anomalyDetection ?? throw new ArgumentNullException(nameof(anomalyDetection));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AnomalyAnalysisResultDto>> Handle(TriggerAnomalyAnalysisCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Triggering anomaly analysis for tenant {TenantId}, lookback {Days} days", _tenantContext.TenantId, request.LookbackDays);

            var anomalies = await _anomalyDetection.ScanUnmatchedTransactionsAsync(request.MinimumSeverity ?? AnomalySeverity.Medium);

            if (!anomalies.IsSuccess || anomalies.Value == null)
            {
                _logger.LogWarning("Anomaly detection failed");
                return Result<AnomalyAnalysisResultDto>.Fail("Failed to complete anomaly analysis");
            }

            var stats = await _anomalyDetection.GetAnomalyStatsAsync();
            var statsData = stats.IsSuccess ? stats.Value : null;

            var result = new AnomalyAnalysisResultDto(
                anomalies.Value.Count,
                anomalies.Value,
                statsData?.AverageConfidence ?? 0m,
                statsData?.CalculatedAt ?? DateTime.UtcNow,
                "Anomaly analysis completed successfully");

            _logger.LogInformation("Anomaly analysis completed: {AnomalyCount} anomalies detected", anomalies.Value.Count);
            return Result<AnomalyAnalysisResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering anomaly analysis");
            return Result<AnomalyAnalysisResultDto>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to run financial health assessment.
/// </summary>
public record RunHealthAssessmentCommand : IRequest<Result<HealthAssessmentResultDto>>;

/// <summary>
/// Handler for RunHealthAssessmentCommand
/// </summary>
public class RunHealthAssessmentCommandHandler : IRequestHandler<RunHealthAssessmentCommand, Result<HealthAssessmentResultDto>>
{
    private readonly IHealthScoreService _healthScore;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RunHealthAssessmentCommandHandler> _logger;

    public RunHealthAssessmentCommandHandler(
        IHealthScoreService healthScore,
        ITenantContext tenantContext,
        ILogger<RunHealthAssessmentCommandHandler> logger)
    {
        _healthScore = healthScore ?? throw new ArgumentNullException(nameof(healthScore));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<HealthAssessmentResultDto>> Handle(RunHealthAssessmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Running health assessment for tenant {TenantId}", _tenantContext.TenantId);

            var comprehensiveHealth = await _healthScore.GetComprehensiveHealthAsync();

            if (!comprehensiveHealth.IsSuccess || comprehensiveHealth.Value == null)
            {
                _logger.LogWarning("Health assessment failed");
                return Result<HealthAssessmentResultDto>.Fail("Failed to complete health assessment");
            }

            var result = new HealthAssessmentResultDto(
                comprehensiveHealth.Value.OverallScore.Score,
                comprehensiveHealth.Value.OverallScore.Rating,
                comprehensiveHealth.Value,
                DateTime.UtcNow,
                "Health assessment completed successfully");

            _logger.LogInformation("Health assessment completed: overall score {Score}", comprehensiveHealth.Value.OverallScore.Score);
            return Result<HealthAssessmentResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running health assessment");
            return Result<HealthAssessmentResultDto>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to generate financial predictions.
/// </summary>
public record GeneratePredictionsCommand(
    DateTime? PeriodStart = null,
    DateTime? PeriodEnd = null,
    int ForecastMonths = 3) : IRequest<Result<PredictionResultDto>>;

/// <summary>
/// Handler for GeneratePredictionsCommand
/// </summary>
public class GeneratePredictionsCommandHandler : IRequestHandler<GeneratePredictionsCommand, Result<PredictionResultDto>>
{
    private readonly IPredictionService _prediction;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GeneratePredictionsCommandHandler> _logger;

    public GeneratePredictionsCommandHandler(
        IPredictionService prediction,
        ITenantContext tenantContext,
        ILogger<GeneratePredictionsCommandHandler> logger)
    {
        _prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PredictionResultDto>> Handle(GeneratePredictionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating predictions for tenant {TenantId}", _tenantContext.TenantId);

            var periodStart = request.PeriodStart ?? DateTime.UtcNow;
            var periodEnd = request.PeriodEnd ?? DateTime.UtcNow.AddMonths(request.ForecastMonths);

            var forecast = await _prediction.GenerateTrendForecastAsync(request.ForecastMonths);

            if (!forecast.IsSuccess || forecast.Value == null)
            {
                _logger.LogWarning("Prediction generation failed");
                return Result<PredictionResultDto>.Fail("Failed to generate predictions");
            }

            var result = new PredictionResultDto(
                forecast.Value,
                periodStart,
                periodEnd,
                DateTime.UtcNow,
                "Predictions generated successfully");

            _logger.LogInformation("Predictions generated for period {Start} to {End}", periodStart, periodEnd);
            return Result<PredictionResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating predictions");
            return Result<PredictionResultDto>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to generate AI recommendations.
/// </summary>
public record GenerateRecommendationsCommand(
    int TopCount = 5,
    bool IncludeAnomalies = true,
    bool IncludeHealth = true,
    bool IncludePredictions = true) : IRequest<Result<RecommendationResultDto>>;

/// <summary>
/// Handler for GenerateRecommendationsCommand
/// </summary>
public class GenerateRecommendationsCommandHandler : IRequestHandler<GenerateRecommendationsCommand, Result<RecommendationResultDto>>
{
    private readonly IRecommendationService _recommendation;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GenerateRecommendationsCommandHandler> _logger;

    public GenerateRecommendationsCommandHandler(
        IRecommendationService recommendation,
        ITenantContext tenantContext,
        ILogger<GenerateRecommendationsCommandHandler> logger)
    {
        _recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<RecommendationResultDto>> Handle(GenerateRecommendationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Generating recommendations for tenant {TenantId}", _tenantContext.TenantId);

            var recommendations = new List<AIRecommendation>();

            if (request.IncludeAnomalies)
            {
                var anomalyRecs = await _recommendation.GenerateAnomalyRecommendationsAsync(request.TopCount);
                if (anomalyRecs.IsSuccess && anomalyRecs.Value != null)
                    recommendations.AddRange(anomalyRecs.Value);
            }

            if (request.IncludeHealth)
            {
                var healthRecs = await _recommendation.GenerateHealthImprovementRecommendationsAsync(request.TopCount);
                if (healthRecs.IsSuccess && healthRecs.Value != null)
                    recommendations.AddRange(healthRecs.Value);
            }

            if (request.IncludePredictions)
            {
                var predictionRecs = await _recommendation.GeneratePredictionRecommendationsAsync(request.TopCount);
                if (predictionRecs.IsSuccess && predictionRecs.Value != null)
                    recommendations.AddRange(predictionRecs.Value);
            }

            var result = new RecommendationResultDto(
                recommendations.Count,
                recommendations.OrderByDescending(r => r.Priority).ToList(),
                DateTime.UtcNow,
                "Recommendations generated successfully");

            _logger.LogInformation("Recommendations generated: {Count} total", recommendations.Count);
            return Result<RecommendationResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            return Result<RecommendationResultDto>.Fail($"Error: {ex.Message}");
        }
    }
}
