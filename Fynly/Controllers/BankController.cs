namespace AiCFO.API.Controllers;

using AiCFO.Application.Features.Bank.Commands;
using AiCFO.Application.Features.Bank.Queries;
using AiCFO.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Bank integration endpoints - manage bank connections and transactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BankController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankController(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    #region Bank Connection Endpoints

    /// <summary>
    /// Initiate a bank connection (start OAuth2 flow).
    /// Returns connection ID and OAuth2 authorization URL to redirect user to.
    /// </summary>
    [HttpPost("connections/initiate")]
    public async Task<ActionResult<ApiResponse<BankConnectionInitiationResponse>>> InitiateBankConnection(
        [FromBody] InitiateBankConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/bank/connections/oauth-callback";

        var command = new InitiateBankConnectionCommand(
            (BankProvider)request.Provider,
            request.BankCode,
            request.BankName,
            callbackUrl);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<BankConnectionInitiationResponse>.Success success => Ok(ApiResponse<BankConnectionInitiationResponse>.Ok(success.Data)),
            Result<BankConnectionInitiationResponse>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// OAuth2 callback endpoint - handles redirect from bank authorization.
    /// Exchange authorization code for access token.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("connections/oauth-callback")]
    public async Task<ActionResult<ApiResponse<string>>> HandleOAuthCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error,
        [FromQuery] string? error_description,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest(ApiResponse<string>.Failure($"Authorization failed: {error_description}"));

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest(ApiResponse<string>.Failure("Missing authorization code or state"));

        // Parse state to get connection ID (format: "{tenantId}:{userId}:{connectionId}")
        var stateParts = state.Split(':');
        if (stateParts.Length != 3 || !Guid.TryParse(stateParts[2], out var connectionId))
            return BadRequest(ApiResponse<string>.Failure("Invalid state parameter"));

        var callbackUrl = $"{Request.Scheme}://{Request.Host}/api/bank/connections/oauth-callback";
        var command = new ExchangeOAuthCodeCommand(connectionId, code, callbackUrl);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<bool>.Success => Ok(ApiResponse<string>.Ok("Bank connection authorized successfully")),
            Result<bool>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Store OAuth2 credentials directly (for testing/direct API calls).
    /// In production, use the OAuth callback endpoint instead.
    /// </summary>
    [HttpPost("connections/{connectionId:guid}/oauth-credentials")]
    public async Task<ActionResult<ApiResponse<string>>> StoreOAuthCredentials(
        [FromRoute] Guid connectionId,
        [FromBody] OAuthCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var command = new StoreBankOAuthCredentialsCommand(
            connectionId,
            request.AccessToken,
            request.ExpiresAt,
            request.RefreshToken);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<bool>.Success => Ok(ApiResponse<string>.Ok("Bank connection authorized successfully")),
            Result<bool>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Get all bank connections for the tenant.
    /// </summary>
    [HttpGet("connections")]
    public async Task<ActionResult<ApiResponse<List<BankConnectionDto>>>> GetBankConnections(
        CancellationToken cancellationToken)
    {
        var query = new GetBankConnectionsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result switch
        {
            Result<List<BankConnectionDto>>.Success success => Ok(ApiResponse<List<BankConnectionDto>>.Ok(success.Data)),
            Result<List<BankConnectionDto>>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    #endregion

    #region Bank Transaction Endpoints

    /// <summary>
    /// Synchronize transactions from a bank connection.
    /// Can be called manually or via scheduled background job.
    /// </summary>
    [HttpPost("sync")]
    public async Task<ActionResult<ApiResponse<string>>> SyncBankTransactions(
        [FromBody] SyncBankTransactionsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SyncBankTransactionsCommand(
            request.ConnectionId,
            request.StartDate,
            request.EndDate);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<string>.Success success => Ok(ApiResponse<string>.Ok(success.Data)),
            Result<string>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Get unreconciled bank transactions pending reconciliation.
    /// </summary>
    [HttpGet("transactions/unreconciled")]
    public async Task<ActionResult<ApiResponse<List<BankTransactionDto>>>> GetUnreconciledTransactions(
        CancellationToken cancellationToken)
    {
        var query = new GetUnreconciledBankTransactionsQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result switch
        {
            Result<List<BankTransactionDto>>.Success success => Ok(ApiResponse<List<BankTransactionDto>>.Ok(success.Data)),
            Result<List<BankTransactionDto>>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    #endregion
}

#region Request DTOs

/// <summary>
/// Request to initiate a bank connection.
/// </summary>
public class InitiateBankConnectionRequest
{
    public int Provider { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
}

/// <summary>
/// OAuth2 callback from bank with authorization.
/// </summary>
public class OAuthCallbackRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Request to synchronize bank transactions.
/// </summary>
public class SyncBankTransactionsRequest
{
    public Guid ConnectionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

#endregion
