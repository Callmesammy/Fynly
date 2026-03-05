namespace AiCFO.Fynly.Controllers;

using AiCFO.Application.Common;
using AiCFO.Application.Features.Alerts.Commands;
using AiCFO.Application.Features.Alerts.Dtos;
using AiCFO.Application.Features.Alerts.Queries;
using AiCFO.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API controller for alert management operations.
/// Handles alert creation, acknowledgment, resolution, and querying.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlertController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AlertController> _logger;

    public AlertController(IMediator mediator, ILogger<AlertController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new alert notification.
    /// POST /api/alerts
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAlert([FromBody] Application.Common.CreateAlertRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received CreateAlert request for anomaly {AnomalyReference}", request.AnomalyReference);

            if (string.IsNullOrWhiteSpace(request.AnomalyReference))
                return BadRequest(ApiResponse<AlertDto>.Failure("Anomaly reference is required"));

            if (request.ConfidenceScore < 0 || request.ConfidenceScore > 100)
                return BadRequest(ApiResponse<AlertDto>.Failure("Confidence score must be between 0 and 100"));

            var command = new CreateAlertCommand(
                request.AnomalyReference,
                request.Severity,
                request.ConfidenceScore,
                request.Message,
                request.AutoDismissHours);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create alert: {Error}", result.Error);
                return BadRequest(ApiResponse<AlertDto>.Failure(result.Error));
            }

            _logger.LogInformation("Successfully created alert {AlertId}", result.Value.AlertId);
            return CreatedAtAction(nameof(GetAlert), new { alertId = result.Value.AlertId }, ApiResponse<AlertDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating alert for anomaly {AnomalyReference}", request.AnomalyReference);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertDto>.Failure($"Error creating alert: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get alert details by ID.
    /// GET /api/alerts/{alertId}
    /// </summary>
    [HttpGet("{alertId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlert(Guid alertId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received GetAlert request for alert {AlertId}", alertId);

            if (alertId == Guid.Empty)
                return BadRequest(ApiResponse<AlertDto>.Failure("Invalid alert ID"));

            var query = new GetAlertQuery(alertId);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Alert {AlertId} not found: {Error}", alertId, result.Error);
                return NotFound(ApiResponse<AlertDto>.Failure(result.Error));
            }

            return Ok(ApiResponse<AlertDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertDto>.Failure($"Error retrieving alert: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get recent alerts with optional filtering.
    /// GET /api/alerts?daysBack=7&status=New
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentAlerts(
        [FromQuery] int daysBack = 7,
        [FromQuery] AlertStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Received GetRecentAlerts request (days={DaysBack}, status={Status})", daysBack, status);

            if (daysBack < 1 || daysBack > 365)
                return BadRequest(ApiResponse<List<AlertDto>>.Failure("Days back must be between 1 and 365"));

            var query = new GetRecentAlertsQuery(daysBack, status);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve recent alerts: {Error}", result.Error);
                return BadRequest(ApiResponse<List<AlertDto>>.Failure(result.Error));
            }

            _logger.LogInformation("Retrieved {Count} recent alerts", result.Value.Count);
            return Ok(ApiResponse<List<AlertDto>>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent alerts");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<List<AlertDto>>.Failure($"Error retrieving alerts: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get open (unresolved and not dismissed) alerts.
    /// GET /api/alerts/open
    /// </summary>
    [HttpGet("open")]
    [ProducesResponseType(typeof(ApiResponse<List<AlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenAlerts(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received GetOpenAlerts request");

            var query = new GetOpenAlertsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve open alerts: {Error}", result.Error);
                return BadRequest(ApiResponse<List<AlertDto>>.Failure(result.Error));
            }

            _logger.LogInformation("Retrieved {Count} open alerts", result.Value.Count);
            return Ok(ApiResponse<List<AlertDto>>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open alerts");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<List<AlertDto>>.Failure($"Error retrieving alerts: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get alert statistics and metrics.
    /// GET /api/alerts/statistics
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<AlertStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlertStatistics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received GetAlertStatistics request");

            var query = new GetAlertStatisticsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to retrieve alert statistics: {Error}", result.Error);
                return BadRequest(ApiResponse<AlertStatisticsDto>.Failure(result.Error));
            }

            _logger.LogInformation("Retrieved alert statistics: {Total} total alerts", result.Value.TotalAlerts);
            return Ok(ApiResponse<AlertStatisticsDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert statistics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertStatisticsDto>.Failure($"Error retrieving statistics: {ex.Message}"));
        }
    }

    /// <summary>
    /// Acknowledge an alert (mark as seen).
    /// POST /api/alerts/{alertId}/acknowledge
    /// </summary>
    [HttpPost("{alertId:guid}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcknowledgeAlert(
        Guid alertId,
        [FromBody] Application.Common.AcknowledgeAlertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received AcknowledgeAlert request for alert {AlertId}", alertId);

            if (alertId == Guid.Empty)
                return BadRequest(ApiResponse<AlertDto>.Failure("Invalid alert ID"));

            if (alertId != request.AlertId)
                return BadRequest(ApiResponse<AlertDto>.Failure("Alert ID mismatch"));

            if (string.IsNullOrWhiteSpace(request.AcknowledgedBy))
                return BadRequest(ApiResponse<AlertDto>.Failure("Acknowledged by user is required"));

            var command = new AcknowledgeAlertCommand(request.AlertId, request.AcknowledgedBy);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to acknowledge alert {AlertId}: {Error}", alertId, result.Error);
                return NotFound(ApiResponse<AlertDto>.Failure(result.Error));
            }

            _logger.LogInformation("Successfully acknowledged alert {AlertId}", alertId);
            return Ok(ApiResponse<AlertDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging alert {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertDto>.Failure($"Error acknowledging alert: {ex.Message}"));
        }
    }

    /// <summary>
    /// Resolve an alert with resolution notes.
    /// POST /api/alerts/{alertId}/resolve
    /// </summary>
    [HttpPost("{alertId:guid}/resolve")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveAlert(
        Guid alertId,
        [FromBody] Application.Common.ResolveAlertRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received ResolveAlert request for alert {AlertId}", alertId);

            if (alertId == Guid.Empty)
                return BadRequest(ApiResponse<AlertDto>.Failure("Invalid alert ID"));

            if (alertId != request.AlertId)
                return BadRequest(ApiResponse<AlertDto>.Failure("Alert ID mismatch"));

            if (string.IsNullOrWhiteSpace(request.ResolutionNotes))
                return BadRequest(ApiResponse<AlertDto>.Failure("Resolution notes are required"));

            var command = new ResolveAlertCommand(request.AlertId, request.ResolutionNotes);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to resolve alert {AlertId}: {Error}", alertId, result.Error);
                return NotFound(ApiResponse<AlertDto>.Failure(result.Error));
            }

            _logger.LogInformation("Successfully resolved alert {AlertId}", alertId);
            return Ok(ApiResponse<AlertDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertDto>.Failure($"Error resolving alert: {ex.Message}"));
        }
    }

    /// <summary>
    /// Dismiss an alert without resolution.
    /// POST /api/alerts/{alertId}/dismiss
    /// </summary>
    [HttpPost("{alertId:guid}/dismiss")]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AlertDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissAlert(
        Guid alertId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received DismissAlert request for alert {AlertId}", alertId);

            if (alertId == Guid.Empty)
                return BadRequest(ApiResponse<AlertDto>.Failure("Invalid alert ID"));

            var command = new DismissAlertCommand(alertId);
            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to dismiss alert {AlertId}: {Error}", alertId, result.Error);
                return NotFound(ApiResponse<AlertDto>.Failure(result.Error));
            }

            _logger.LogInformation("Successfully dismissed alert {AlertId}", alertId);
            return Ok(ApiResponse<AlertDto>.Ok(result.Value));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dismissing alert {AlertId}", alertId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<AlertDto>.Failure($"Error dismissing alert: {ex.Message}"));
        }
    }
}
