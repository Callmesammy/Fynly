namespace AiCFO.Controllers;

using AiCFO.Application.Common;
using AiCFO.Application.Features.HealthReports.Commands;
using AiCFO.Application.Features.HealthReports.Queries;
using AiCFO.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

/// <summary>
/// API endpoints for health report management and operations.
/// </summary>
[Authorize]
[ApiController]
[Route("api/reports")]
public class HealthReportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<HealthReportController> _logger;

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    public HealthReportController(IMediator mediator, ILogger<HealthReportController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Generate a new health report.
    /// </summary>
    /// <param name="request">Report generation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated health report</returns>
    [HttpPost("health")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateHealthReport(
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating health report, type: {ReportType}", request.ReportType);

        var command = new GenerateHealthReportCommand(request.ReportType);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<HealthReportDetailDto>.Ok(result.Value))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Retrieve a specific health report by ID.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health report detail</returns>
    [HttpGet("health/{reportId}")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHealthReport(
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving health report {ReportId}", reportId);

        var query = new GetHealthReportQuery(reportId);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<HealthReportDetailDto>.Ok(result.Value))
            : NotFound(ApiResponse<object>.Failure("Report not found"));
    }

    /// <summary>
    /// List all health reports with optional filtering.
    /// </summary>
    /// <param name="statusFilter">Filter by report status (optional)</param>
    /// <param name="fromDate">Filter by from date (optional)</param>
    /// <param name="toDate">Filter by to date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of health report summaries</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<List<HealthReportSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListHealthReports(
        [FromQuery] ReportStatus? statusFilter = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing health reports, status filter: {StatusFilter}", statusFilter?.ToString() ?? "None");

        var query = new ListHealthReportsQuery(statusFilter, fromDate, toDate);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<HealthReportSummaryDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Schedule a health report for recurring delivery.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="request">Schedule request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduled report details</returns>
    [HttpPost("health/{reportId}/schedule")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleHealthReport(
        [FromRoute] Guid reportId,
        [FromBody] ScheduleReportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Scheduling health report {ReportId} for {ScheduledFor}", reportId, request.ScheduledFor);

        var command = new ScheduleHealthReportCommand(reportId, request);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<HealthReportDetailDto>.Ok(result.Value))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Send a health report to specified recipients.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="recipients">Recipient email addresses (comma-separated)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("health/{reportId}/send")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendHealthReport(
        [FromRoute] Guid reportId,
        [FromBody] SendReportRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending health report {ReportId} to {Recipients}", reportId, request.Recipients);

        var command = new SendHealthReportCommand(reportId, request.Recipients);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok("Report sent successfully"))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Archive a health report.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    [HttpPost("health/{reportId}/archive")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ArchiveHealthReport(
        [FromRoute] Guid reportId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Archiving health report {ReportId}", reportId);

        var command = new ArchiveHealthReportCommand(reportId);
        var result = await _mediator.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<object>.Ok("Report archived successfully"))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Get all scheduled health reports.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scheduled reports</returns>
    [HttpGet("health/scheduled")]
    [ProducesResponseType(typeof(ApiResponse<List<ScheduledReportDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScheduledReports(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving scheduled health reports");

        var query = new GetScheduledHealthReportsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<ScheduledReportDto>>.Ok(result.Value))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }

    /// <summary>
    /// Get health report statistics.
    /// </summary>
    /// <param name="fromDate">Filter statistics from date (optional)</param>
    /// <param name="toDate">Filter statistics to date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health report statistics</returns>
    [HttpGet("health/statistics")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving health report statistics");

        var query = new GetHealthReportStatisticsQuery(fromDate, toDate);
        var result = await _mediator.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<HealthReportStatisticsDto>.Ok(result.Value))
            : BadRequest(ApiResponse<object>.Failure(result.Error));
    }
}

/// <summary>
/// Request DTO for sending a report.
/// </summary>
public class SendReportRequest
{
    /// <summary>
    /// Recipient email addresses (comma-separated or JSON array format).
    /// </summary>
    public string Recipients { get; set; } = string.Empty;
}
