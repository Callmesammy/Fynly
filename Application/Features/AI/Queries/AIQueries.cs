namespace AiCFO.Application.Features.AI.Queries;

using MediatR;
using AiCFO.Application.Common;
using AiCFO.Application.Features.AI.Dtos;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Query to retrieve recent anomalies.
/// </summary>
public record GetRecentAnomaliesQuery(
    int Days = 30,
    AnomalySeverity? SeverityFilter = null) : IRequest<Result<List<AnomalyDto>>>;

/// <summary>
/// Handler for GetRecentAnomaliesQuery
/// </summary>
public class GetRecentAnomaliesQueryHandler : IRequestHandler<GetRecentAnomaliesQuery, Result<List<AnomalyDto>>>
{
    private readonly IAnomalyDetectionService _anomalyDetection;
    private readonly ILogger<GetRecentAnomaliesQueryHandler> _logger;

    public GetRecentAnomaliesQueryHandler(
        IAnomalyDetectionService anomalyDetection,
        ILogger<GetRecentAnomaliesQueryHandler> logger)
    {
        _anomalyDetection = anomalyDetection ?? throw new ArgumentNullException(nameof(anomalyDetection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<AnomalyDto>>> Handle(GetRecentAnomaliesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving recent anomalies for last {Days} days", request.Days);

            var result = await _anomalyDetection.GetRecentAnomaliesAsync(request.Days, request.SeverityFilter);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to retrieve anomalies");
                return Result<List<AnomalyDto>>.Fail("Failed to retrieve anomalies");
            }

            _logger.LogInformation("Retrieved {Count} anomalies", result.Value.Count);
            return Result<List<AnomalyDto>>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomalies");
            return Result<List<AnomalyDto>>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to retrieve financial health score.
/// </summary>
public record GetFinancialHealthQuery : IRequest<Result<ComprehensiveHealthScoreDto>>;

/// <summary>
/// Handler for GetFinancialHealthQuery
/// </summary>
public class GetFinancialHealthQueryHandler : IRequestHandler<GetFinancialHealthQuery, Result<ComprehensiveHealthScoreDto>>
{
    private readonly IHealthScoreService _healthScore;
    private readonly ILogger<GetFinancialHealthQueryHandler> _logger;

    public GetFinancialHealthQueryHandler(
        IHealthScoreService healthScore,
        ILogger<GetFinancialHealthQueryHandler> logger)
    {
        _healthScore = healthScore ?? throw new ArgumentNullException(nameof(healthScore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ComprehensiveHealthScoreDto>> Handle(GetFinancialHealthQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving financial health score");

            var result = await _healthScore.GetComprehensiveHealthAsync();

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to retrieve health score");
                return Result<ComprehensiveHealthScoreDto>.Fail("Failed to retrieve health score");
            }

            _logger.LogInformation("Retrieved health score: {Score}", result.Value.OverallScore.Score);
            return Result<ComprehensiveHealthScoreDto>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health score");
            return Result<ComprehensiveHealthScoreDto>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to retrieve financial predictions.
/// </summary>
public record GetFinancialPredictionsQuery(
    int ForecastMonths = 3) : IRequest<Result<ComprehensiveForecastDto>>;

/// <summary>
/// Handler for GetFinancialPredictionsQuery
/// </summary>
public class GetFinancialPredictionsQueryHandler : IRequestHandler<GetFinancialPredictionsQuery, Result<ComprehensiveForecastDto>>
{
    private readonly IPredictionService _prediction;
    private readonly ILogger<GetFinancialPredictionsQueryHandler> _logger;

    public GetFinancialPredictionsQueryHandler(
        IPredictionService prediction,
        ILogger<GetFinancialPredictionsQueryHandler> logger)
    {
        _prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ComprehensiveForecastDto>> Handle(GetFinancialPredictionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving financial predictions for {Months} months", request.ForecastMonths);

            var result = await _prediction.GenerateTrendForecastAsync(request.ForecastMonths);

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to retrieve predictions");
                return Result<ComprehensiveForecastDto>.Fail("Failed to retrieve predictions");
            }

            _logger.LogInformation("Retrieved predictions for {Months} months", request.ForecastMonths);
            return Result<ComprehensiveForecastDto>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving predictions");
            return Result<ComprehensiveForecastDto>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to retrieve AI recommendations.
/// </summary>
public record GetAIRecommendationsQuery(
    int TopCount = 5,
    RecommendationPriority? PriorityFilter = null) : IRequest<Result<List<AIRecommendation>>>;

/// <summary>
/// Handler for GetAIRecommendationsQuery
/// </summary>
public class GetAIRecommendationsQueryHandler : IRequestHandler<GetAIRecommendationsQuery, Result<List<AIRecommendation>>>
{
    private readonly IRecommendationService _recommendation;
    private readonly ILogger<GetAIRecommendationsQueryHandler> _logger;

    public GetAIRecommendationsQueryHandler(
        IRecommendationService recommendation,
        ILogger<GetAIRecommendationsQueryHandler> logger)
    {
        _recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<AIRecommendation>>> Handle(GetAIRecommendationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving AI recommendations");

            var result = await _recommendation.GetUrgentRecommendationsAsync();

            if (!result.IsSuccess || result.Value == null)
            {
                _logger.LogWarning("Failed to retrieve recommendations");
                return Result<List<AIRecommendation>>.Fail("Failed to retrieve recommendations");
            }

            var filtered = result.Value
                .Where(r => request.PriorityFilter == null || r.Priority >= request.PriorityFilter)
                .Take(request.TopCount)
                .ToList();

            _logger.LogInformation("Retrieved {Count} recommendations", filtered.Count);
            return Result<List<AIRecommendation>>.Ok(filtered);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommendations");
            return Result<List<AIRecommendation>>.Fail($"Error: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to retrieve comprehensive AI dashboard.
/// </summary>
public record GetAIDashboardQuery(
    int TopAnomalies = 5,
    int TopRecommendations = 5) : IRequest<Result<AIDashboardDto>>;

/// <summary>
/// Handler for GetAIDashboardQuery
/// </summary>
public class GetAIDashboardQueryHandler : IRequestHandler<GetAIDashboardQuery, Result<AIDashboardDto>>
{
    private readonly IAnomalyDetectionService _anomalyDetection;
    private readonly IHealthScoreService _healthScore;
    private readonly IPredictionService _prediction;
    private readonly IRecommendationService _recommendation;
    private readonly ILogger<GetAIDashboardQueryHandler> _logger;

    public GetAIDashboardQueryHandler(
        IAnomalyDetectionService anomalyDetection,
        IHealthScoreService healthScore,
        IPredictionService prediction,
        IRecommendationService recommendation,
        ILogger<GetAIDashboardQueryHandler> logger)
    {
        _anomalyDetection = anomalyDetection ?? throw new ArgumentNullException(nameof(anomalyDetection));
        _healthScore = healthScore ?? throw new ArgumentNullException(nameof(healthScore));
        _prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        _recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AIDashboardDto>> Handle(GetAIDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Building AI dashboard");

            // Get anomalies
            var anomalies = await _anomalyDetection.GetRecentAnomaliesAsync(30);
            var anomalyList = anomalies.IsSuccess && anomalies.Value != null 
                ? anomalies.Value.Take(request.TopAnomalies).ToList() 
                : new();

            // Get health score
            var health = await _healthScore.GetComprehensiveHealthAsync();
            var healthData = health.IsSuccess ? health.Value : null;

            // Get predictions
            var forecast = await _prediction.GenerateTrendForecastAsync(3);
            var forecastData = forecast.IsSuccess ? forecast.Value : null;

            // Get recommendations
            var recs = await _recommendation.GetUrgentRecommendationsAsync();
            var recList = recs.IsSuccess && recs.Value != null 
                ? recs.Value.Take(request.TopRecommendations).ToList() 
                : new();

            var stats = await _anomalyDetection.GetAnomalyStatsAsync();

            var dashboard = new AIDashboardDto(
                DateTime.UtcNow,
                anomalyList.Count,
                anomalyList,
                healthData?.OverallScore.Score ?? 0m,
                healthData?.OverallScore.Rating ?? "Unknown",
                forecastData,
                recList,
                stats.IsSuccess ? stats.Value : null);

            _logger.LogInformation("AI dashboard built successfully");
            return Result<AIDashboardDto>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building AI dashboard");
            return Result<AIDashboardDto>.Fail($"Error: {ex.Message}");
        }
    }
}
