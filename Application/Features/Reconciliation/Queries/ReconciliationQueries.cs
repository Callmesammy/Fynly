namespace AiCFO.Application.Features.Reconciliation.Queries;

using AiCFO.Application.Features.Reconciliation.Dtos;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Query to get reconciliation matches with filtering.
/// </summary>
public record GetReconciliationMatchesQuery : IRequest<Result<List<ReconciliationMatchDto>>>
{
    public ReconciliationStatus? Status { get; init; }
    public int Skip { get; init; } = 0;
    public int Take { get; init; } = 50;
}

/// <summary>
/// Query handler for GetReconciliationMatchesQuery.
/// </summary>
public class GetReconciliationMatchesQueryHandler : IRequestHandler<GetReconciliationMatchesQuery, Result<List<ReconciliationMatchDto>>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetReconciliationMatchesQueryHandler> _logger;

    public GetReconciliationMatchesQueryHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<GetReconciliationMatchesQueryHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<ReconciliationMatchDto>>> Handle(GetReconciliationMatchesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<ReconciliationMatch> matches;

            if (request.Status.HasValue)
            {
                matches = await _reconciliationService.GetReconciliationMatchesByStatusAsync(
                    _tenantContext.TenantId,
                    request.Status.Value,
                    cancellationToken);
            }
            else
            {
                matches = await _reconciliationService.GetUnconfirmedMatchesAsync(
                    _tenantContext.TenantId,
                    cancellationToken);
            }

            var dtos = matches
                .Skip(request.Skip)
                .Take(request.Take)
                .Select(m => new ReconciliationMatchDto
                {
                    MatchId = m.Id,
                    BankTransactionId = m.BankTransactionId,
                    JournalEntryId = m.JournalEntryId,
                    Status = m.Status.ToString(),
                    ConfidencePercentage = m.MatchScore.ConfidencePercentage,
                    MatchType = m.MatchScore.MatchType.ToString(),
                    VarianceAmount = m.VarianceAmount.Amount.Amount,
                    DaysDifference = m.TimelineVariance.DaysDifference,
                    Notes = m.Notes,
                    MatchedAt = m.MatchedAt
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} reconciliation matches for tenant {TenantId}", 
                dtos.Count, _tenantContext.TenantId);

            return Result<List<ReconciliationMatchDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation matches");
            return Result<List<ReconciliationMatchDto>>.Fail($"Failed to retrieve matches: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to get unmatched bank transactions.
/// </summary>
public record GetUnmatchedBankTransactionsQuery : IRequest<Result<List<UnmatchedBankTransactionDto>>>
{
    public int MinDaysOld { get; init; } = 0;
}

/// <summary>
/// Query handler for GetUnmatchedBankTransactionsQuery.
/// </summary>
public class GetUnmatchedBankTransactionsQueryHandler : IRequestHandler<GetUnmatchedBankTransactionsQuery, Result<List<UnmatchedBankTransactionDto>>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetUnmatchedBankTransactionsQueryHandler> _logger;

    public GetUnmatchedBankTransactionsQueryHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<GetUnmatchedBankTransactionsQueryHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<UnmatchedBankTransactionDto>>> Handle(GetUnmatchedBankTransactionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var unmatched = await _reconciliationService.GetUnmatchedBankTransactionsAsync(
                _tenantContext.TenantId,
                request.MinDaysOld,
                cancellationToken);

            var dtos = unmatched
                .Select(u => new UnmatchedBankTransactionDto
                {
                    TransactionId = u.BankTransactionId,
                    TransactionReference = u.TransactionReference,
                    Amount = u.Amount.Amount,
                    TransactionDate = u.TransactionDate,
                    Description = u.Description,
                    DaysUnmatched = u.DaysUnmatched
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} unmatched bank transactions for tenant {TenantId}", 
                dtos.Count, _tenantContext.TenantId);

            return Result<List<UnmatchedBankTransactionDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unmatched bank transactions");
            return Result<List<UnmatchedBankTransactionDto>>.Fail($"Failed to retrieve unmatched transactions: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to get unmatched journal entries.
/// </summary>
public record GetUnmatchedJournalEntriesQuery : IRequest<Result<List<UnmatchedJournalEntryDto>>>
{
    public int MinDaysOld { get; init; } = 0;
}

/// <summary>
/// Query handler for GetUnmatchedJournalEntriesQuery.
/// </summary>
public class GetUnmatchedJournalEntriesQueryHandler : IRequestHandler<GetUnmatchedJournalEntriesQuery, Result<List<UnmatchedJournalEntryDto>>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetUnmatchedJournalEntriesQueryHandler> _logger;

    public GetUnmatchedJournalEntriesQueryHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<GetUnmatchedJournalEntriesQueryHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<List<UnmatchedJournalEntryDto>>> Handle(GetUnmatchedJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var unmatched = await _reconciliationService.GetUnmatchedJournalEntriesAsync(
                _tenantContext.TenantId,
                request.MinDaysOld,
                cancellationToken);

            var dtos = unmatched
                .Select(u => new UnmatchedJournalEntryDto
                {
                    EntryId = u.JournalEntryId,
                    ReferenceNumber = u.ReferenceNumber,
                    Amount = u.Amount.Amount,
                    EntryDate = u.EntryDate,
                    Description = u.Description,
                    DaysUnmatched = u.DaysUnmatched
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} unmatched journal entries for tenant {TenantId}", 
                dtos.Count, _tenantContext.TenantId);

            return Result<List<UnmatchedJournalEntryDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unmatched journal entries");
            return Result<List<UnmatchedJournalEntryDto>>.Fail($"Failed to retrieve unmatched entries: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to get reconciliation statistics.
/// </summary>
public record GetReconciliationStatsQuery : IRequest<Result<ReconciliationStatsDto>>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

/// <summary>
/// Query handler for GetReconciliationStatsQuery.
/// </summary>
public class GetReconciliationStatsQueryHandler : IRequestHandler<GetReconciliationStatsQuery, Result<ReconciliationStatsDto>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetReconciliationStatsQueryHandler> _logger;

    public GetReconciliationStatsQueryHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<GetReconciliationStatsQueryHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<ReconciliationStatsDto>> Handle(GetReconciliationStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _reconciliationService.GetReconciliationStatsAsync(
                _tenantContext.TenantId,
                request.FromDate,
                request.ToDate,
                cancellationToken);

            var dto = new ReconciliationStatsDto
            {
                TotalMatches = stats.TotalMatches,
                ConfirmedMatches = stats.ConfirmedMatches,
                PendingMatches = stats.PendingMatches,
                RejectedMatches = stats.RejectedMatches,
                MatchRate = stats.MatchRate,
                AverageMatchConfidence = stats.AverageMatchConfidence,
                UnmatchedBankTransactions = stats.UnmatchedBankTransactions,
                UnmatchedJournalEntries = stats.UnmatchedJournalEntries
            };

            _logger.LogInformation("Retrieved reconciliation stats for tenant {TenantId}", _tenantContext.TenantId);
            return Result<ReconciliationStatsDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation stats");
            return Result<ReconciliationStatsDto>.Fail($"Failed to retrieve stats: {ex.Message}");
        }
    }
}

/// <summary>
/// Query to get reconciliation health report.
/// </summary>
public record GetReconciliationHealthQuery : IRequest<Result<ReconciliationHealthReportDto>>;

/// <summary>
/// DTO for reconciliation health report response.
/// </summary>
public sealed record ReconciliationHealthReportDto
{
    public string HealthStatus { get; init; } = null!;
    public int AgedUnmatchedCount { get; init; }
    public int LowConfidenceMatchCount { get; init; }
    public decimal HighVariancePercentage { get; init; }
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// Query handler for GetReconciliationHealthQuery.
/// </summary>
public class GetReconciliationHealthQueryHandler : IRequestHandler<GetReconciliationHealthQuery, Result<ReconciliationHealthReportDto>>
{
    private readonly IReconciliationService _reconciliationService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<GetReconciliationHealthQueryHandler> _logger;

    public GetReconciliationHealthQueryHandler(
        IReconciliationService reconciliationService,
        ITenantContext tenantContext,
        ILogger<GetReconciliationHealthQueryHandler> logger)
    {
        _reconciliationService = reconciliationService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<ReconciliationHealthReportDto>> Handle(GetReconciliationHealthQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var health = await _reconciliationService.GetReconciliationHealthAsync(
                _tenantContext.TenantId,
                cancellationToken);

            var dto = new ReconciliationHealthReportDto
            {
                HealthStatus = health.HealthStatus,
                AgedUnmatchedCount = health.AgedUnmatchedCount,
                LowConfidenceMatchCount = health.LowConfidenceMatchCount,
                HighVariancePercentage = health.HighVariancePercentage,
                Recommendations = health.Recommendations
            };

            _logger.LogInformation("Retrieved reconciliation health for tenant {TenantId}: {Status}", 
                _tenantContext.TenantId, health.HealthStatus);

            return Result<ReconciliationHealthReportDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reconciliation health");
            return Result<ReconciliationHealthReportDto>.Fail($"Failed to retrieve health report: {ex.Message}");
        }
    }
}
