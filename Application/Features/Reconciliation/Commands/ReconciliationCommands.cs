namespace AiCFO.Application.Features.Reconciliation.Commands;

using AiCFO.Application.Features.Reconciliation.Dtos;
using AiCFO.Domain.ValueObjects;
using AiCFO.Domain.Entities;

/// <summary>
/// Command to run auto-matching reconciliation algorithms.
/// </summary>
public record FindAndCreateMatchesCommand : IRequest<Result<MatchingResult>>
{
    public decimal VarianceThreshold { get; init; } = 2m;
    public int DateTolerance { get; init; } = 7;
}

/// <summary>
/// Command handler for FindAndCreateMatchesCommand.
/// </summary>
public class FindAndCreateMatchesCommandHandler : IRequestHandler<FindAndCreateMatchesCommand, Result<MatchingResult>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly IBankService _bankService;
    private readonly ILedgerService _ledgerService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<FindAndCreateMatchesCommandHandler> _logger;

    public FindAndCreateMatchesCommandHandler(
        IReconciliationService reconciliationService,
        IBankService bankService,
        ILedgerService ledgerService,
        ITenantContext tenantContext,
        ILogger<FindAndCreateMatchesCommandHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _bankService = bankService;
        _ledgerService = ledgerService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<MatchingResult>> Handle(FindAndCreateMatchesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting auto-matching for tenant {TenantId}", _tenantContext.TenantId);

            var bankTransactions = (await _bankService.GetUnreconciledTransactionsAsync(_tenantContext.TenantId, cancellationToken)).ToList();
            var journalEntries = await _ledgerService.GetJournalEntriesByDateRangeAsync(
                _tenantContext.TenantId,
                DateTime.UtcNow.AddMonths(-3),
                DateTime.UtcNow.AddDays(1),
                cancellationToken);

            _logger.LogInformation("Found {BankTxCount} bank transactions and {EntryCount} journal entries", 
                bankTransactions.Count, journalEntries.Count);

            // Run exact matching
            var exactMatches = await _reconciliationService.FindExactMatchesAsync(
                _tenantContext.TenantId,
                bankTransactions,
                journalEntries,
                _tenantContext.UserId,
                cancellationToken);

            _logger.LogInformation("Found {ExactMatchCount} exact matches", exactMatches.Count);

            // Run partial matching (skip already matched)
            var partialMatches = await _reconciliationService.FindPartialMatchesAsync(
                _tenantContext.TenantId,
                bankTransactions,
                journalEntries,
                request.VarianceThreshold,
                _tenantContext.UserId,
                cancellationToken);

            _logger.LogInformation("Found {PartialMatchCount} partial matches", partialMatches.Count);

            // Run date range matching
            var dateRangeMatches = await _reconciliationService.FindDateRangeMatchesAsync(
                _tenantContext.TenantId,
                bankTransactions,
                journalEntries,
                request.DateTolerance,
                _tenantContext.UserId,
                cancellationToken);

            _logger.LogInformation("Found {DateRangeMatchCount} date range matches", dateRangeMatches.Count);

            // Update unmatched items
            await _reconciliationService.UpdateUnmatchedItemsAsync(_tenantContext.TenantId, cancellationToken);

            var result = new MatchingResult
            {
                ExactMatchesCreated = exactMatches.Count,
                PartialMatchesCreated = partialMatches.Count,
                DateRangeMatchesCreated = dateRangeMatches.Count,
                UnmatchedBankTransactions = bankTransactions.Count - exactMatches.Count - partialMatches.Count - dateRangeMatches.Count,
                UnmatchedJournalEntries = journalEntries.Count
            };

            _logger.LogInformation("Auto-matching completed: {Result}", result);
            return Result<MatchingResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-matching failed for tenant {TenantId}", _tenantContext.TenantId);
            return Result<MatchingResult>.Fail($"Auto-matching failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to confirm a reconciliation match.
/// </summary>
public record ConfirmReconciliationMatchCommand : IRequest<Result<bool>>
{
    public Guid MatchId { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Command handler for ConfirmReconciliationMatchCommand.
/// </summary>
public class ConfirmReconciliationMatchCommandHandler : IRequestHandler<ConfirmReconciliationMatchCommand, Result<bool>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ConfirmReconciliationMatchCommandHandler> _logger;

    public ConfirmReconciliationMatchCommandHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<ConfirmReconciliationMatchCommandHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(ConfirmReconciliationMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _reconciliationService.ConfirmMatchAsync(
                _tenantContext.TenantId,
                request.MatchId,
                _tenantContext.UserId,
                request.Notes,
                cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to confirm match {MatchId} for tenant {TenantId}", 
                    request.MatchId, _tenantContext.TenantId);
                return Result<bool>.Fail("Match not found or already confirmed");
            }

            _logger.LogInformation("Confirmed match {MatchId}", request.MatchId);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming match {MatchId}", request.MatchId);
            return Result<bool>.Fail($"Failed to confirm match: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to reject a reconciliation match.
/// </summary>
public record RejectReconciliationMatchCommand : IRequest<Result<bool>>
{
    public Guid MatchId { get; init; }
    public string Reason { get; init; } = null!;
}

/// <summary>
/// Command handler for RejectReconciliationMatchCommand.
/// </summary>
public class RejectReconciliationMatchCommandHandler : IRequestHandler<RejectReconciliationMatchCommand, Result<bool>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<RejectReconciliationMatchCommandHandler> _logger;

    public RejectReconciliationMatchCommandHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<RejectReconciliationMatchCommandHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RejectReconciliationMatchCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _reconciliationService.RejectMatchAsync(
                _tenantContext.TenantId,
                request.MatchId,
                _tenantContext.UserId,
                request.Reason,
                cancellationToken);

            if (!success)
            {
                _logger.LogWarning("Failed to reject match {MatchId} for tenant {TenantId}", 
                    request.MatchId, _tenantContext.TenantId);
                return Result<bool>.Fail("Match not found or already rejected");
            }

            _logger.LogInformation("Rejected match {MatchId}: {Reason}", request.MatchId, request.Reason);
            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting match {MatchId}", request.MatchId);
            return Result<bool>.Fail($"Failed to reject match: {ex.Message}");
        }
    }
}

/// <summary>
/// Command to create a reconciliation session.
/// </summary>
public record CreateReconciliationSessionCommand : IRequest<Result<Guid>>
{
    public string SessionName { get; init; } = null!;
    public CurrencyCode CurrencyCode { get; init; } = CurrencyCode.NGN;
}

/// <summary>
/// Command handler for CreateReconciliationSessionCommand.
/// </summary>
public class CreateReconciliationSessionCommandHandler : IRequestHandler<CreateReconciliationSessionCommand, Result<Guid>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<CreateReconciliationSessionCommandHandler> _logger;

    public CreateReconciliationSessionCommandHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<CreateReconciliationSessionCommandHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateReconciliationSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var session = await _reconciliationService.CreateSessionAsync(
                _tenantContext.TenantId,
                request.SessionName,
                _tenantContext.UserId,
                request.CurrencyCode,
                cancellationToken);

            _logger.LogInformation("Created reconciliation session {SessionId}: {SessionName}", 
                session.Id, request.SessionName);

            return Result<Guid>.Ok(session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reconciliation session");
            return Result<Guid>.Fail($"Failed to create session: {ex.Message}");
        }
    }
}
