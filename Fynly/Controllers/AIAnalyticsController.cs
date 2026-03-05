namespace AiCFO.Fynly.Controllers;

using AiCFO.Application.Features.AI.Commands;
using AiCFO.Application.Features.AI.Dtos;
using AiCFO.Application.Features.AI.Queries;
using AiCFO.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API controller for AI-powered financial analytics.
/// Provides endpoints for anomaly detection, health assessment, predictions, and recommendations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIAnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AIAnalyticsController> _logger;

    public AIAnalyticsController(
        IMediator mediator,
        ILogger<AIAnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Trigger anomaly detection analysis on unmatched transactions.
    /// Scans for unusual patterns, outliers, and reconciliation issues.
    /// </summary>
    /// <param name="command">Anomaly analysis parameters (lookback days, severity threshold)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Anomaly analysis results with severity scores</returns>
    /// <response code="200">Anomaly analysis completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during analysis</response>
    [HttpPost("analyze/anomalies")]
    [ProducesResponseType(typeof(ApiResponse<AnomalyAnalysisResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<AnomalyAnalysisResultDto>> TriggerAnomalyAnalysis(
        [FromBody] TriggerAnomalyAnalysisCommand? command = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Anomaly analysis request received");

            var analyzeCommand = command ?? new TriggerAnomalyAnalysisCommand
            {
                LookbackDays = 30,
                MinimumSeverity = AnomalySeverity.Medium
            };

            var result = await _mediator.Send(analyzeCommand, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<AnomalyAnalysisResultDto>.Failure(result.Error);

            _logger.LogInformation("Anomaly analysis completed: {Count} anomalies detected", 
                result.Value?.TotalAnomalies ?? 0);
            return ApiResponse<AnomalyAnalysisResultDto>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anomaly analysis error");
            return ApiResponse<AnomalyAnalysisResultDto>.Failure($"Anomaly analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Run comprehensive financial health assessment.
    /// Evaluates liquidity, profitability, solvency, efficiency, and growth dimensions.
    /// </summary>
    /// <param name="command">Health assessment parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive health assessment with multi-dimensional scores</returns>
    /// <response code="200">Health assessment completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during assessment</response>
    [HttpPost("health")]
    [ProducesResponseType(typeof(ApiResponse<HealthAssessmentResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<HealthAssessmentResultDto>> RunHealthAssessment(
        [FromBody] RunHealthAssessmentCommand? command = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Health assessment request received");

            var healthCommand = command ?? new RunHealthAssessmentCommand();

            var result = await _mediator.Send(healthCommand, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<HealthAssessmentResultDto>.Failure(result.Error);

            _logger.LogInformation("Health assessment completed with overall score: {Score}", 
                result.Value?.OverallScore ?? 0);
            return ApiResponse<HealthAssessmentResultDto>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health assessment error");
            return ApiResponse<HealthAssessmentResultDto>.Failure($"Health assessment failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate financial predictions and forecasts.
    /// Projects future account balances, cash flow, revenue, and expenses.
    /// </summary>
    /// <param name="command">Prediction parameters (months to forecast)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Financial predictions with confidence intervals</returns>
    /// <response code="200">Predictions generated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during prediction</response>
    [HttpPost("predictions")]
    [ProducesResponseType(typeof(ApiResponse<PredictionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<PredictionResultDto>> GeneratePredictions(
        [FromBody] GeneratePredictionsCommand? command = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Prediction request received");

            var predictionCommand = command ?? new GeneratePredictionsCommand
            {
                ForecastMonths = 3
            };

            var result = await _mediator.Send(predictionCommand, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<PredictionResultDto>.Failure(result.Error);

            _logger.LogInformation("Predictions generated for {Months} months", 
                predictionCommand.ForecastMonths);
            return ApiResponse<PredictionResultDto>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Prediction generation error");
            return ApiResponse<PredictionResultDto>.Failure($"Prediction generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate AI-powered financial recommendations.
    /// Provides actionable recommendations based on anomalies, health, and predictions.
    /// </summary>
    /// <param name="command">Recommendation parameters (priority filter)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of AI-generated recommendations with priority and rationale</returns>
    /// <response code="200">Recommendations generated successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during recommendation generation</response>
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<RecommendationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<RecommendationResultDto>> GenerateRecommendations(
        [FromBody] GenerateRecommendationsCommand? command = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Recommendation request received");

            var recommendationCommand = command ?? new GenerateRecommendationsCommand();

            var result = await _mediator.Send(recommendationCommand, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<RecommendationResultDto>.Failure(result.Error);

            _logger.LogInformation("Recommendations generated: {Count} recommendations", 
                result.Value?.TotalCount ?? 0);
            return ApiResponse<RecommendationResultDto>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recommendation generation error");
            return ApiResponse<RecommendationResultDto>.Failure($"Recommendation generation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get comprehensive AI analytics dashboard.
    /// Aggregates anomalies, health, predictions, and recommendations into a single view.
    /// </summary>
    /// <param name="topAnomalies">Number of top anomalies to include (default: 5)</param>
    /// <param name="topRecommendations">Number of top recommendations to include (default: 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive AI analytics dashboard</returns>
    /// <response code="200">Dashboard retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<AIDashboardDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<AIDashboardDto>> GetDashboard(
        [FromQuery] int topAnomalies = 5,
        [FromQuery] int topRecommendations = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Dashboard request received");

            var query = new GetAIDashboardQuery(topAnomalies, topRecommendations);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<AIDashboardDto>.Failure(result.Error);

            _logger.LogInformation("Dashboard retrieved successfully");
            return ApiResponse<AIDashboardDto>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard retrieval error");
            return ApiResponse<AIDashboardDto>.Failure($"Dashboard retrieval failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get recent anomalies with optional filtering and pagination.
    /// </summary>
    /// <param name="daysToLookback">Number of days to look back (default: 30)</param>
    /// <param name="minimumConfidence">Minimum confidence level filter (0-100, default: 0)</param>
    /// <param name="skip">Number of records to skip (pagination, default: 0)</param>
    /// <param name="take">Number of records to return (pagination, default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent anomalies</returns>
    /// <response code="200">Anomalies retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(ApiResponse<List<object>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> GetRecentAnomalies(
        [FromQuery] int days = 30,
        [FromQuery] string? severityFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get anomalies request: days={Days}, severity={Severity}", 
                days, severityFilter ?? "all");

            var severity = severityFilter != null && Enum.TryParse<AnomalySeverity>(severityFilter, out var s) ? (AnomalySeverity?)s : null;
            var query = new GetRecentAnomaliesQuery
            {
                Days = days,
                SeverityFilter = severity
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<object>.Failure(result.Error);

            _logger.LogInformation("Anomalies retrieved: {Count} records", 
                (result.Value as ICollection<object>)?.Count ?? 0);
            return ApiResponse<object>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get anomalies error");
            return ApiResponse<object>.Failure($"Failed to retrieve anomalies: {ex.Message}");
        }
    }

    /// <summary>
    /// Get financial health status with optional filtering.
    /// </summary>
    /// <param name="includeDetails">Include detailed scoring breakdown (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive health status</returns>
    /// <response code="200">Health status retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> GetHealthStatus(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get health status request");

            var query = new GetFinancialHealthQuery();

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<object>.Failure(result.Error);

            _logger.LogInformation("Health status retrieved successfully");
            return ApiResponse<object>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get health status error");
            return ApiResponse<object>.Failure($"Failed to retrieve health status: {ex.Message}");
        }
    }

    /// <summary>
    /// Get financial predictions with optional configuration.
    /// </summary>
    /// <param name="forecastMonths">Number of months to forecast (default: 3)</param>
    /// <param name="includeConfidenceIntervals">Include confidence intervals (default: true)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Financial predictions and trends</returns>
    /// <response code="200">Predictions retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("predictions")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> GetPredictions(
        [FromQuery] int forecastMonths = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get predictions request: months={Months}", forecastMonths);

            var query = new GetFinancialPredictionsQuery
            {
                ForecastMonths = forecastMonths
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<object>.Failure(result.Error);

            _logger.LogInformation("Predictions retrieved successfully");
            return ApiResponse<object>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get predictions error");
            return ApiResponse<object>.Failure($"Failed to retrieve predictions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get AI recommendations with optional priority filtering.
    /// </summary>
    /// <param name="priorityFilter">Filter by priority level (Critical, High, Medium, Low; default: all)</param>
    /// <param name="skip">Number of records to skip (pagination, default: 0)</param>
    /// <param name="take">Number of records to return (pagination, default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of AI recommendations</returns>
    /// <response code="200">Recommendations retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<object>> GetRecommendations(
        [FromQuery] int topCount = 5,
        [FromQuery] string? priorityFilter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Get recommendations request: priority={Priority}", 
                priorityFilter ?? "all");

            var priority = priorityFilter != null && Enum.TryParse<RecommendationPriority>(priorityFilter, out var p) ? (RecommendationPriority?)p : null;
            var query = new GetAIRecommendationsQuery
            {
                TopCount = topCount,
                PriorityFilter = priority
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<object>.Failure(result.Error);

            _logger.LogInformation("Recommendations retrieved successfully");
            return ApiResponse<object>.Ok(result.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get recommendations error");
            return ApiResponse<object>.Failure($"Failed to retrieve recommendations: {ex.Message}");
        }
    }
}
