namespace AiCFO.Application.Features.Ledger.Commands;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to create a chart of accounts for a tenant.
/// </summary>
public class CreateChartOfAccountsCommand : IRequest<Result<Guid>>
{
    public string CompanyName { get; set; } = string.Empty;

    public CreateChartOfAccountsCommand(string companyName)
    {
        CompanyName = companyName;
    }
}

/// <summary>
/// Handler for creating chart of accounts.
/// </summary>
public class CreateChartOfAccountsCommandHandler : IRequestHandler<CreateChartOfAccountsCommand, Result<Guid>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public CreateChartOfAccountsCommandHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<Guid>> Handle(CreateChartOfAccountsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var chartOfAccounts = await _ledgerService.CreateChartOfAccountsAsync(
                _tenantContext.TenantId,
                request.CompanyName,
                _tenantContext.UserId,
                cancellationToken);

            return Result<Guid>.Ok(chartOfAccounts.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"Failed to create chart of accounts: {ex.Message}");
        }
    }
}
