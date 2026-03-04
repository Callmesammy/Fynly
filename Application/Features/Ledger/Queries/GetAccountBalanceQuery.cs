namespace AiCFO.Application.Features.Ledger.Queries;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// DTO for account balance.
/// </summary>
public class AccountBalanceDto
{
    public string AccountCode { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "NGN";
}

/// <summary>
/// Query to retrieve balance for a specific account.
/// </summary>
public class GetAccountBalanceQuery : IRequest<Result<AccountBalanceDto>>
{
    public string AccountCode { get; set; } = string.Empty;

    public GetAccountBalanceQuery(string accountCode)
    {
        AccountCode = accountCode;
    }
}

/// <summary>
/// Handler for getting account balance.
/// </summary>
public class GetAccountBalanceQueryHandler : IRequestHandler<GetAccountBalanceQuery, Result<AccountBalanceDto>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public GetAccountBalanceQueryHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<AccountBalanceDto>> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var accountCode = new AccountCode(request.AccountCode);
            var balance = await _ledgerService.GetAccountBalanceAsync(_tenantContext.TenantId, accountCode, cancellationToken);

            var dto = new AccountBalanceDto
            {
                AccountCode = accountCode.ToString(),
                Balance = balance.Amount,
                Currency = balance.Currency.Code.ToString()
            };

            return Result<AccountBalanceDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<AccountBalanceDto>.Fail($"Failed to retrieve account balance: {ex.Message}");
        }
    }
}
