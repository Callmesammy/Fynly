namespace AiCFO.Application.Features.Ledger.Commands;

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
/// </summary>
public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, Result<bool>>
{
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;

    public PostJournalEntryCommandHandler(ILedgerService ledgerService, ITenantContext tenantContext)
    {
        _ledgerService = ledgerService ?? throw new ArgumentNullException(nameof(ledgerService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task<Result<bool>> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
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
