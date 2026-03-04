namespace AiCFO.Application.Features.Ledger.Queries;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// DTO for trial balance line item.
/// </summary>
public class TrialBalanceLineDto
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitBalance { get; set; }
    public decimal CreditBalance { get; set; }
}

/// <summary>
/// Query to retrieve trial balance.
/// </summary>
public class GetTrialBalanceQuery : IRequest<Result<List<TrialBalanceLineDto>>>
{
}

/// <summary>
/// Handler for getting trial balance.
/// </summary>
public class GetTrialBalanceQueryHandler : IRequestHandler<GetTrialBalanceQuery, Result<List<TrialBalanceLineDto>>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public GetTrialBalanceQueryHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<List<TrialBalanceLineDto>>> Handle(GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var trialBalance = await _ledgerService.GetTrialBalanceAsync(_tenantContext.TenantId, cancellationToken);

            var lines = trialBalance
                .Select(tb => new TrialBalanceLineDto
                {
                    AccountCode = tb.Code.ToString(),
                    AccountName = tb.Name,
                    DebitBalance = tb.Balance.Amount, // TODO: Determine debit vs credit based on account type
                    CreditBalance = 0
                })
                .ToList();

            return Result<List<TrialBalanceLineDto>>.Ok(lines);
        }
        catch (Exception ex)
        {
            return Result<List<TrialBalanceLineDto>>.Fail($"Failed to retrieve trial balance: {ex.Message}");
        }
    }
}
