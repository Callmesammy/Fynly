namespace AiCFO.Application.Features.Bank.Queries;

using AiCFO.Application.Common;

/// <summary>
/// DTO for bank transaction.
/// </summary>
public class BankTransactionDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? CounterpartyName { get; set; }
    public bool IsReconciled { get; set; }
}

/// <summary>
/// Query to get unreconciled bank transactions.
/// </summary>
public class GetUnreconciledBankTransactionsQuery : IRequest<Result<List<BankTransactionDto>>>
{
}

/// <summary>
/// Handler for getting unreconciled transactions.
/// </summary>
public class GetUnreconciledBankTransactionsQueryHandler : IRequestHandler<GetUnreconciledBankTransactionsQuery, Result<List<BankTransactionDto>>>
{
    private readonly IBankService _bankService;
    private readonly ITenantContext _tenantContext;

    public GetUnreconciledBankTransactionsQueryHandler(IBankService bankService, ITenantContext tenantContext)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<List<BankTransactionDto>>> Handle(GetUnreconciledBankTransactionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var transactions = await _bankService.GetUnreconciledTransactionsAsync(_tenantContext.TenantId, cancellationToken);

            var dtos = transactions
                .Select(t => new BankTransactionDto
                {
                    Id = t.Id,
                    TransactionDate = t.TransactionDate,
                    Amount = t.Amount.Amount,
                    Currency = t.Amount.Currency.Code.ToString(),
                    TransactionType = (int)t.TransactionType,
                    Description = t.Description,
                    Reference = t.Reference,
                    CounterpartyName = t.CounterpartyName,
                    IsReconciled = t.LinkedJournalLineId.HasValue
                })
                .ToList();

            return Result<List<BankTransactionDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            return Result<List<BankTransactionDto>>.Fail($"Failed to retrieve unreconciled transactions: {ex.Message}");
        }
    }
}
