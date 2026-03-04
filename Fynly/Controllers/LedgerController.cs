namespace AiCFO.API.Controllers;

using AiCFO.Application.Common;
using AiCFO.Application.Features.Ledger.Commands;
using AiCFO.Application.Features.Ledger.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Ledger management endpoints - chart of accounts, journal entries, trial balance.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LedgerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITenantContext _tenantContext;

    public LedgerController(IMediator mediator, ITenantContext tenantContext)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Chart of Accounts Endpoints

    /// <summary>
    /// Create a new chart of accounts for the tenant.
    /// </summary>
    [HttpPost("chart-of-accounts")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateChartOfAccounts(
        [FromBody] CreateChartOfAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateChartOfAccountsCommand(request.CompanyName);
        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<Guid>.Success success => Ok(ApiResponse<Guid>.Ok(success.Data)),
            Result<Guid>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Add a new account to chart of accounts.
    /// </summary>
    [HttpPost("chart-of-accounts/accounts")]
    public async Task<ActionResult<ApiResponse<string>>> AddAccount(
        [FromBody] AddAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddAccountCommand(
            request.AccountCode,
            request.AccountName,
            request.AccountTypeId,
            request.AccountSubTypeId,
            request.Description);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<bool>.Success => Ok(ApiResponse<string>.Ok("Account added successfully")),
            Result<bool>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    #endregion

    #region Journal Entry Endpoints

    /// <summary>
    /// Record a new journal entry (initially in Draft state).
    /// </summary>
    [HttpPost("journal-entries")]
    public async Task<ActionResult<ApiResponse<Guid>>> RecordJournalEntry(
        [FromBody] RecordJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordJournalEntryCommand(
            request.ReferenceNumber,
            request.EntryDate,
            request.Description,
            request.CurrencyCode);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<Guid>.Success success => CreatedAtAction(nameof(RecordJournalEntry), new { id = success.Data }, ApiResponse<Guid>.Ok(success.Data)),
            Result<Guid>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Add a line item (debit or credit) to a journal entry.
    /// </summary>
    [HttpPost("journal-entries/{entryId:guid}/lines")]
    public async Task<ActionResult<ApiResponse<string>>> AddJournalLine(
        [FromRoute] Guid entryId,
        [FromBody] AddJournalLineRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddJournalLineCommand(
            entryId,
            request.AccountCode,
            request.Description,
            request.Amount,
            request.IsDebit);

        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<bool>.Success => Ok(ApiResponse<string>.Ok("Line added to journal entry")),
            Result<bool>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Post a journal entry (transition from Draft to Posted).
    /// Validates that debits equal credits before posting.
    /// </summary>
    [HttpPost("journal-entries/{entryId:guid}/post")]
    public async Task<ActionResult<ApiResponse<string>>> PostJournalEntry(
        [FromRoute] Guid entryId,
        CancellationToken cancellationToken)
    {
        var command = new PostJournalEntryCommand(entryId);
        var result = await _mediator.Send(command, cancellationToken);

        return result switch
        {
            Result<bool>.Success => Ok(ApiResponse<string>.Ok("Journal entry posted successfully")),
            Result<bool>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    #endregion

    #region Reporting Endpoints

    /// <summary>
    /// Get trial balance for the tenant.
    /// Lists all accounts with their debit and credit balances.
    /// </summary>
    [HttpGet("trial-balance")]
    public async Task<ActionResult<ApiResponse<List<TrialBalanceLineDto>>>> GetTrialBalance(CancellationToken cancellationToken)
    {
        var query = new GetTrialBalanceQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result switch
        {
            Result<List<TrialBalanceLineDto>>.Success success => Ok(ApiResponse<List<TrialBalanceLineDto>>.Ok(success.Data)),
            Result<List<TrialBalanceLineDto>>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    /// <summary>
    /// Get balance for a specific account.
    /// </summary>
    [HttpGet("accounts/{accountCode}/balance")]
    public async Task<ActionResult<ApiResponse<AccountBalanceDto>>> GetAccountBalance(
        [FromRoute] string accountCode,
        CancellationToken cancellationToken)
    {
        var query = new GetAccountBalanceQuery(accountCode);
        var result = await _mediator.Send(query, cancellationToken);

        return result switch
        {
            Result<AccountBalanceDto>.Success success => Ok(ApiResponse<AccountBalanceDto>.Ok(success.Data)),
            Result<AccountBalanceDto>.Failure failure => BadRequest(ApiResponse<string>.Failure(failure.Message)),
            _ => StatusCode(500, ApiResponse<string>.Failure("Unexpected error"))
        };
    }

    #endregion

}

#region Request DTOs

public class CreateChartOfAccountsRequest
{
    public string CompanyName { get; set; } = string.Empty;
}

public class AddAccountRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int AccountTypeId { get; set; }
    public int AccountSubTypeId { get; set; }
    public string? Description { get; set; }
}

public class RecordJournalEntryRequest
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "NGN";
}

public class AddJournalLineRequest
{
    public string AccountCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsDebit { get; set; }
}

#endregion
