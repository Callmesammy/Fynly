namespace AiCFO.Application.Features.Ledger.Commands;

using AiCFO.Application.Services;
using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Command to post a journal entry (transition from Draft to Posted).
/// </summary>
public class PostJournalEntryCommand : IRequest<Result<bool>>
{
    public Guid JournalEntryId { get; set; }

    public PostJournalEntryCommand(Guid journalEntryId)
    {
        JournalEntryId = journalEntryId;
    }
}

/// <summary>
/// Handler for posting a journal entry.
/// Uses accounting validation rules to ensure entry is compliant before posting.
/// </summary>
public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, Result<bool>>
{
    private readonly ILedgerService _ledgerService;
    private readonly IAccountingValidationService _validationService;
    private readonly ITenantContext _tenantContext;

    public PostJournalEntryCommandHandler(
        ILedgerService ledgerService,
        IAccountingValidationService validationService,
        ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<bool>> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Retrieve the journal entry
            var entry = await _ledgerService.GetJournalEntryAsync(
                _tenantContext.TenantId,
                request.JournalEntryId,
                cancellationToken);

            if (entry == null)
                return Result<bool>.Fail("Journal entry not found");

            // Validate using accounting rules
            var validationResult = _validationService.ValidateJournalEntry(
                entry.TotalDebits,
                entry.TotalCredits,
                entry.Lines.Count);

            if (!validationResult.IsValid)
                return Result<bool>.Fail(validationResult.ErrorMessage);

            // Post the entry
            var success = await _ledgerService.PostJournalEntryAsync(
                _tenantContext.TenantId,
                request.JournalEntryId,
                _tenantContext.UserId,
                cancellationToken);

            return success
                ? Result<bool>.Ok(true)
                : Result<bool>.Fail("Failed to post journal entry");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error posting journal entry: {ex.Message}");
        }
    }
}
