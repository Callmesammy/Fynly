namespace AiCFO.Application.Features.Bank.Commands;

using AiCFO.Application.Common;

/// <summary>
/// Command to synchronize transactions from a bank.
/// </summary>
public class SyncBankTransactionsCommand : IRequest<Result<string>>
{
    public Guid ConnectionId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public SyncBankTransactionsCommand(Guid connectionId, DateTime startDate, DateTime endDate)
    {
        ConnectionId = connectionId;
        StartDate = startDate;
        EndDate = endDate;
    }
}

/// <summary>
/// Handler for syncing bank transactions.
/// </summary>
public class SyncBankTransactionsCommandHandler : IRequestHandler<SyncBankTransactionsCommand, Result<string>>
{
    private readonly IBankService _bankService;
    private readonly ITenantContext _tenantContext;

    public SyncBankTransactionsCommandHandler(IBankService bankService, ITenantContext tenantContext)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<string>> Handle(SyncBankTransactionsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var (success, message) = await _bankService.SyncBankTransactionsAsync(
                _tenantContext.TenantId,
                request.ConnectionId,
                request.StartDate,
                request.EndDate,
                cancellationToken);

            return success
                ? Result<string>.Ok(message)
                : Result<string>.Fail(message);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Error syncing bank transactions: {ex.Message}");
        }
    }
}
