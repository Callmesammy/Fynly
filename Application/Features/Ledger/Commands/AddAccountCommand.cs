namespace AiCFO.Application.Features.Ledger.Commands;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to add an account to the chart of accounts.
/// </summary>
public class AddAccountCommand : IRequest<Result<bool>>
{
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int AccountTypeId { get; set; }
    public int AccountSubTypeId { get; set; }
    public string? Description { get; set; }

    public AddAccountCommand(string accountCode, string accountName, int accountTypeId, int accountSubTypeId, string? description = null)
    {
        AccountCode = accountCode;
        AccountName = accountName;
        AccountTypeId = accountTypeId;
        AccountSubTypeId = accountSubTypeId;
        Description = description;
    }
}

/// <summary>
/// Handler for adding an account to chart of accounts.
/// </summary>
public class AddAccountCommandHandler : IRequestHandler<AddAccountCommand, Result<bool>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public AddAccountCommandHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<bool>> Handle(AddAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var accountCode = new AccountCode(request.AccountCode);
            var accountType = (AccountType)request.AccountTypeId;
            var accountSubType = (AccountSubType)request.AccountSubTypeId;

            var success = await _ledgerService.AddAccountAsync(
                _tenantContext.TenantId,
                accountCode,
                request.AccountName,
                accountType,
                accountSubType,
                request.Description,
                _tenantContext.UserId,
                cancellationToken);

            return success
                ? Result<bool>.Ok(true)
                : Result<bool>.Fail("Failed to add account");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error adding account: {ex.Message}");
        }
    }
}
