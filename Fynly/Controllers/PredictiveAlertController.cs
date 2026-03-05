namespace AiCFO.Controllers;

using AiCFO.Application.Features.PredictiveAlerts.Commands;
using AiCFO.Application.Features.PredictiveAlerts.Queries;

[Authorize]
[Route("api/predictive-alerts")]
[ApiController]
public class PredictiveAlertController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PredictiveAlertController> _logger;

    public PredictiveAlertController(IMediator mediator, ILogger<PredictiveAlertController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("thresholds")]
    public async Task<ActionResult<ApiResponse<PredictiveThresholdDto>>> CreateThreshold(
        [FromBody] CreateThresholdRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("POST: Creating predictive threshold");
            var result = await _mediator.Send(new CreateThresholdCommand(request), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<PredictiveThresholdDto>.Ok(result.Value) 
                : ApiResponse<PredictiveThresholdDto>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating threshold");
            return StatusCode(500, ApiResponse<PredictiveThresholdDto>.Failure("Internal server error"));
        }
    }

    [HttpGet("thresholds")]
    public async Task<ActionResult<ApiResponse<List<PredictiveThresholdDto>>>> GetThresholds(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET: Retrieving predictive thresholds");
            var result = await _mediator.Send(new GetThresholdsQuery(), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<List<PredictiveThresholdDto>>.Ok(result.Value) 
                : ApiResponse<List<PredictiveThresholdDto>>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thresholds");
            return StatusCode(500, ApiResponse<List<PredictiveThresholdDto>>.Failure("Internal server error"));
        }
    }

    [HttpGet("thresholds/{thresholdId}")]
    public async Task<ActionResult<ApiResponse<PredictiveThresholdDto>>> GetThreshold(
        Guid thresholdId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET: Retrieving threshold: {ThresholdId}", thresholdId);
            var result = await _mediator.Send(new GetThresholdQuery(thresholdId), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<PredictiveThresholdDto>.Ok(result.Value) 
                : ApiResponse<PredictiveThresholdDto>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting threshold");
            return StatusCode(500, ApiResponse<PredictiveThresholdDto>.Failure("Internal server error"));
        }
    }

    [HttpPut("thresholds/{thresholdId}")]
    public async Task<ActionResult<ApiResponse<PredictiveThresholdDto>>> UpdateThreshold(
        Guid thresholdId, [FromBody] UpdateThresholdRequest request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("PUT: Updating threshold: {ThresholdId}", thresholdId);
            var result = await _mediator.Send(new UpdateThresholdCommand(thresholdId, request), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<PredictiveThresholdDto>.Ok(result.Value) 
                : ApiResponse<PredictiveThresholdDto>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating threshold");
            return StatusCode(500, ApiResponse<PredictiveThresholdDto>.Failure("Internal server error"));
        }
    }

    [HttpPost("evaluate")]
    public async Task<ActionResult<ApiResponse<List<PredictiveAlertDto>>>> EvaluateThresholds(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("POST: Evaluating all predictive thresholds");
            var result = await _mediator.Send(new EvaluateThresholdsCommand(), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<List<PredictiveAlertDto>>.Ok(result.Value) 
                : ApiResponse<List<PredictiveAlertDto>>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating thresholds");
            return StatusCode(500, ApiResponse<List<PredictiveAlertDto>>.Failure("Internal server error"));
        }
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<ApiResponse<List<PredictiveAlertDto>>>> GetAlerts(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET: Retrieving predictive alerts");
            var result = await _mediator.Send(new GetAlertsQuery(), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<List<PredictiveAlertDto>>.Ok(result.Value) 
                : ApiResponse<List<PredictiveAlertDto>>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting alerts");
            return StatusCode(500, ApiResponse<List<PredictiveAlertDto>>.Failure("Internal server error"));
        }
    }

    [HttpGet("alerts/active")]
    public async Task<ActionResult<ApiResponse<List<PredictiveAlertDto>>>> GetActiveAlerts(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET: Retrieving active alerts");
            var result = await _mediator.Send(new GetActiveAlertsQuery(), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<List<PredictiveAlertDto>>.Ok(result.Value) 
                : ApiResponse<List<PredictiveAlertDto>>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active alerts");
            return StatusCode(500, ApiResponse<List<PredictiveAlertDto>>.Failure("Internal server error"));
        }
    }

    [HttpPost("alerts/{alertId}/acknowledge")]
    public async Task<ActionResult<ApiResponse<string>>> AcknowledgeAlert(
        Guid alertId, [FromBody] string? notes = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST: Acknowledging alert: {AlertId}", alertId);
            var result = await _mediator.Send(new AcknowledgeAlertCommand(alertId, notes), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<string>.Ok(result.Value) 
                : ApiResponse<string>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert");
            return StatusCode(500, ApiResponse<string>.Failure("Internal server error"));
        }
    }

    [HttpPost("alerts/{alertId}/resolve")]
    public async Task<ActionResult<ApiResponse<string>>> ResolveAlert(
        Guid alertId, [FromBody] string? notes = null, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("POST: Resolving alert: {AlertId}", alertId);
            var result = await _mediator.Send(new ResolveAlertCommand(alertId, notes), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<string>.Ok(result.Value) 
                : ApiResponse<string>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert");
            return StatusCode(500, ApiResponse<string>.Failure("Internal server error"));
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<PredictiveAlertStatisticsDto>>> GetStatistics(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("GET: Retrieving predictive alert statistics");
            var result = await _mediator.Send(new GetPredictiveAlertStatisticsQuery(), ct);
            return Ok(result.IsSuccess 
                ? ApiResponse<PredictiveAlertStatisticsDto>.Ok(result.Value) 
                : ApiResponse<PredictiveAlertStatisticsDto>.Failure(result.Error));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, ApiResponse<PredictiveAlertStatisticsDto>.Failure("Internal server error"));
        }
    }
}
