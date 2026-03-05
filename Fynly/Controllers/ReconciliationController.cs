namespace AiCFO.Fynly.Controllers;

using AiCFO.Application.Features.Reconciliation.Commands;
using AiCFO.Application.Features.Reconciliation.Dtos;
using AiCFO.Application.Features.Reconciliation.Queries;
using AiCFO.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API controller for reconciliation operations.
/// Handles bank transaction to journal entry matching and reconciliation management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReconciliationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReconciliationController> _logger;

    public ReconciliationController(
        IMediator mediator,
        ILogger<ReconciliationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Run auto-matching algorithms to find reconciliation matches.
    /// </summary>
    /// <param name="request">Auto-matching parameters (variance threshold, date tolerance)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching results with counts</returns>
    /// <response code="200">Auto-matching completed successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Server error during matching</response>
    [HttpPost("auto-match")]
    [ProducesResponseType(typeof(ApiResponse<MatchingResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<MatchingResult>> RunAutoMatching(
        [FromBody] AutoMatchingRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Auto-matching request received");

            var command = new FindAndCreateMatchesCommand
            {
                VarianceThreshold = request?.VarianceThreshold ?? 2m,
                DateTolerance = request?.DateTolerance ?? 7
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<MatchingResult>.Failure(result.Error);

            _logger.LogInformation("Auto-matching completed: {Result}", result.Value);
            return ApiResponse<MatchingResult>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-matching error");
            return ApiResponse<MatchingResult>.Failure($"Auto-matching failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get reconciliation matches with optional filtering by status.
    /// </summary>
    /// <param name="status">Filter by match status (Proposed, Confirmed, Rejected, Manual)</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to return (pagination)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of reconciliation matches</returns>
    /// <response code="200">Matches retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(ApiResponse<List<ReconciliationMatchDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<ReconciliationMatchDto>>> GetMatches(
        [FromQuery] string? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parsedStatus = string.IsNullOrEmpty(status) 
                ? null 
                : (ReconciliationStatus?)Enum.Parse(typeof(ReconciliationStatus), status);

            var query = new GetReconciliationMatchesQuery
            {
                Status = parsedStatus,
                Skip = skip,
                Take = take
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<List<ReconciliationMatchDto>>.Failure(result.Error);

            return ApiResponse<List<ReconciliationMatchDto>>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving matches");
            return ApiResponse<List<ReconciliationMatchDto>>.Failure($"Failed to retrieve matches: {ex.Message}");
        }
    }

    /// <summary>
    /// Confirm a reconciliation match (user approval).
    /// </summary>
    /// <param name="matchId">ID of the match to confirm</param>
    /// <param name="request">Confirmation details (optional notes)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation result</returns>
    /// <response code="200">Match confirmed successfully</response>
    /// <response code="404">Match not found</response>
    /// <response code="500">Server error</response>
    [HttpPost("matches/{matchId}/confirm")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ApiResponse<bool>> ConfirmMatch(
        [FromRoute] Guid matchId,
        [FromBody] ConfirmMatchRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ConfirmReconciliationMatchCommand
            {
                MatchId = matchId,
                Notes = request?.Notes
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<bool>.Failure(result.Error);

            _logger.LogInformation("Match {MatchId} confirmed", matchId);
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming match {MatchId}", matchId);
            return ApiResponse<bool>.Failure($"Failed to confirm match: {ex.Message}");
        }
    }

    /// <summary>
    /// Reject a reconciliation match.
    /// </summary>
    /// <param name="matchId">ID of the match to reject</param>
    /// <param name="request">Rejection details (reason required)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rejection result</returns>
    /// <response code="200">Match rejected successfully</response>
    /// <response code="400">Invalid request (missing reason)</response>
    /// <response code="404">Match not found</response>
    [HttpPost("matches/{matchId}/reject")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<bool>> RejectMatch(
        [FromRoute] Guid matchId,
        [FromBody] RejectMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<bool>.Failure("Reason is required");

            var command = new RejectReconciliationMatchCommand
            {
                MatchId = matchId,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<bool>.Failure(result.Error);

            _logger.LogInformation("Match {MatchId} rejected: {Reason}", matchId, request.Reason);
            return ApiResponse<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting match {MatchId}", matchId);
            return ApiResponse<bool>.Failure($"Failed to reject match: {ex.Message}");
        }
    }

    /// <summary>
    /// Get unmatched bank transactions.
    /// </summary>
    /// <param name="minDaysOld">Filter for items older than N days (0 = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unmatched bank transactions</returns>
    /// <response code="200">Unmatched transactions retrieved</response>
    /// <response code="500">Server error</response>
    [HttpGet("unmatched/bank")]
    [ProducesResponseType(typeof(ApiResponse<List<UnmatchedBankTransactionDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<UnmatchedBankTransactionDto>>> GetUnmatchedBankTransactions(
        [FromQuery] int minDaysOld = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUnmatchedBankTransactionsQuery { MinDaysOld = minDaysOld };
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<List<UnmatchedBankTransactionDto>>.Failure(result.Error);

            return ApiResponse<List<UnmatchedBankTransactionDto>>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unmatched bank transactions");
            return ApiResponse<List<UnmatchedBankTransactionDto>>.Failure($"Failed to retrieve unmatched transactions: {ex.Message}");
        }
    }

    /// <summary>
    /// Get unmatched journal entries.
    /// </summary>
    /// <param name="minDaysOld">Filter for items older than N days (0 = all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unmatched journal entries</returns>
    /// <response code="200">Unmatched entries retrieved</response>
    /// <response code="500">Server error</response>
    [HttpGet("unmatched/entries")]
    [ProducesResponseType(typeof(ApiResponse<List<UnmatchedJournalEntryDto>>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<List<UnmatchedJournalEntryDto>>> GetUnmatchedJournalEntries(
        [FromQuery] int minDaysOld = 0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetUnmatchedJournalEntriesQuery { MinDaysOld = minDaysOld };
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<List<UnmatchedJournalEntryDto>>.Failure(result.Error);

            return ApiResponse<List<UnmatchedJournalEntryDto>>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unmatched journal entries");
            return ApiResponse<List<UnmatchedJournalEntryDto>>.Failure($"Failed to retrieve unmatched entries: {ex.Message}");
        }
    }

    /// <summary>
    /// Get reconciliation statistics.
    /// </summary>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reconciliation statistics</returns>
    /// <response code="200">Stats retrieved successfully</response>
    /// <response code="500">Server error</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationStatsDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<ReconciliationStatsDto>> GetStats(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetReconciliationStatsQuery
            {
                FromDate = fromDate,
                ToDate = toDate
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<ReconciliationStatsDto>.Failure(result.Error);

            return ApiResponse<ReconciliationStatsDto>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation stats");
            return ApiResponse<ReconciliationStatsDto>.Failure($"Failed to retrieve stats: {ex.Message}");
        }
    }

    /// <summary>
    /// Get reconciliation health report with diagnostics and recommendations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health report with status and recommendations</returns>
    /// <response code="200">Health report retrieved</response>
    /// <response code="500">Server error</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationHealthReportDto>), StatusCodes.Status200OK)]
    public async Task<ApiResponse<ReconciliationHealthReportDto>> GetHealth(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetReconciliationHealthQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<ReconciliationHealthReportDto>.Failure(result.Error);

            _logger.LogInformation("Reconciliation health: {Status}", result.Value.HealthStatus);
            return ApiResponse<ReconciliationHealthReportDto>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation health");
            return ApiResponse<ReconciliationHealthReportDto>.Failure($"Failed to retrieve health report: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new reconciliation session.
    /// </summary>
    /// <param name="request">Session creation details (name, currency)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created session ID</returns>
    /// <response code="200">Session created successfully</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ApiResponse<Guid>> CreateSession(
        [FromBody] CreateSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionName))
                return ApiResponse<Guid>.Failure("Session name is required");

            var command = new CreateReconciliationSessionCommand
            {
                SessionName = request.SessionName,
                CurrencyCode = request.CurrencyCode
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess)
                return ApiResponse<Guid>.Failure(result.Error);

            _logger.LogInformation("Created session {SessionId}", result.Value);
            return ApiResponse<Guid>.Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reconciliation session");
            return ApiResponse<Guid>.Failure($"Failed to create session: {ex.Message}");
        }
    }
}
