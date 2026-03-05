namespace AiCFO.Application.Features.Ledger.Commands;

using AiCFO.Application.Services;
using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to add a line item (debit/credit) to a journal entry.
/// </summary>
public class AddJournalLineCommand : IRequest<Result<bool>>
{
    public Guid JournalEntryId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsDebit { get; set; }

    public AddJournalLineCommand(Guid journalEntryId, string accountCode, string description, decimal amount, bool isDebit)
    {
        JournalEntryId = journalEntryId;
        AccountCode = accountCode;
        Description = description;
        Amount = amount;
        IsDebit = isDebit;
    }
}

/// <summary>
/// Handler for adding a line to a journal entry.
/// Validates line data according to accounting rules before adding.
/// </summary>
public class AddJournalLineCommandHandler : IRequestHandler<AddJournalLineCommand, Result<bool>>
{
    private readonly ILedgerService _ledgerService;
    private readonly IAccountingValidationService _validationService;
    private readonly ITenantContext _tenantContext;

    public AddJournalLineCommandHandler(
        ILedgerService ledgerService,
        IAccountingValidationService validationService,
        ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<bool>> Handle(AddJournalLineCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var accountCode = new AccountCode(request.AccountCode);
            var money = Money.Create(request.Amount, CurrencyCode.NGN); // TODO: Get currency from journal entry

            // Validate line data
            var lineValidation = _validationService.ValidateTransactionLine(money, request.Description);
            if (!lineValidation.IsValid)
                return Result<bool>.Fail(lineValidation.ErrorMessage);

            var success = await _ledgerService.AddJournalLineAsync(
                _tenantContext.TenantId,
                request.JournalEntryId,
                accountCode,
                request.Description,
                money,
                request.IsDebit,
                _tenantContext.UserId,
                cancellationToken);

            return success
                ? Result<bool>.Ok(true)
                : Result<bool>.Fail("Failed to add journal line");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error adding journal line: {ex.Message}");
        }
    }
}
