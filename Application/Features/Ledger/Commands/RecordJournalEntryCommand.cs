namespace AiCFO.Application.Features.Ledger.Commands;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to record a new journal entry.
/// </summary>
public class RecordJournalEntryCommand : IRequest<Result<Guid>>
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "NGN";

    public RecordJournalEntryCommand(string referenceNumber, DateTime entryDate, string description, string currencyCode = "NGN")
    {
        ReferenceNumber = referenceNumber;
        EntryDate = entryDate;
        Description = description;
        CurrencyCode = currencyCode;
    }
}

/// <summary>
/// Handler for recording a new journal entry.
/// </summary>
public class RecordJournalEntryCommandHandler : IRequestHandler<RecordJournalEntryCommand, Result<Guid>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public RecordJournalEntryCommandHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<Guid>> Handle(RecordJournalEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currencyCode = Enum.Parse<CurrencyCode>(request.CurrencyCode);
            
            var entry = await _ledgerService.CreateJournalEntryAsync(
                _tenantContext.TenantId,
                request.ReferenceNumber,
                request.EntryDate,
                request.Description,
                _tenantContext.UserId,
                currencyCode,
                cancellationToken);

            return Result<Guid>.Ok(entry.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"Failed to record journal entry: {ex.Message}");
        }
    }
}
