namespace AiCFO.Application.Common;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Service abstraction for reconciliation operations.
/// Handles matching bank transactions with journal entries, confidence scoring, and reconciliation tracking.
/// Implementation in Infrastructure layer.
/// </summary>
public interface IReconciliationService
{
    // Reconciliation Match operations
    Task<ReconciliationMatch?> GetReconciliationMatchAsync(Guid tenantId, Guid matchId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReconciliationMatch>> GetReconciliationMatchesByStatusAsync(Guid tenantId, ReconciliationStatus status, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReconciliationMatch>> GetUnconfirmedMatchesAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<ReconciliationMatch> CreateMatchAsync(
        Guid tenantId,
        Guid bankTransactionId,
        Guid journalEntryId,
        MatchScore matchScore,
        VarianceAmount varianceAmount,
        TimelineVariance timelineVariance,
        Guid createdBy,
        CancellationToken cancellationToken);

    // Match confirmation/rejection
    Task<bool> ConfirmMatchAsync(Guid tenantId, Guid matchId, Guid confirmedBy, string? notes, CancellationToken cancellationToken);
    Task<bool> RejectMatchAsync(Guid tenantId, Guid matchId, Guid rejectedBy, string reason, CancellationToken cancellationToken);
    Task<bool> AddNotesAsync(Guid tenantId, Guid matchId, string notes, Guid addedBy, CancellationToken cancellationToken);

    // Auto-matching algorithms
    Task<List<ReconciliationMatch>> FindExactMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<List<ReconciliationMatch>> FindPartialMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        decimal varianceThreshold,
        Guid createdBy,
        CancellationToken cancellationToken);

    Task<List<ReconciliationMatch>> FindDateRangeMatchesAsync(
        Guid tenantId,
        IReadOnlyList<BankTransaction> bankTransactions,
        IReadOnlyList<JournalEntry> journalEntries,
        int dayTolerance,
        Guid createdBy,
        CancellationToken cancellationToken);

    // Unmatched items tracking
    Task<IReadOnlyList<UnmatchedBankTransaction>> GetUnmatchedBankTransactionsAsync(Guid tenantId, int minDaysOld = 0, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UnmatchedJournalEntry>> GetUnmatchedJournalEntriesAsync(Guid tenantId, int minDaysOld = 0, CancellationToken cancellationToken = default);
    Task<bool> UpdateUnmatchedItemsAsync(Guid tenantId, CancellationToken cancellationToken);

    // Reconciliation Sessions
    Task<ReconciliationSession> CreateSessionAsync(Guid tenantId, string sessionName, Guid createdBy, CurrencyCode currencyCode, CancellationToken cancellationToken);
    Task<ReconciliationSession?> GetSessionAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReconciliationSession>> GetRecentSessionsAsync(Guid tenantId, int count = 10, CancellationToken cancellationToken = default);
    Task<bool> CompleteSessionAsync(Guid tenantId, Guid sessionId, Guid completedBy, CancellationToken cancellationToken);

    // Reconciliation Statistics
    Task<ReconciliationStats> GetReconciliationStatsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<ReconciliationHealthReport> GetReconciliationHealthAsync(Guid tenantId, CancellationToken cancellationToken);
}

/// <summary>
/// Reconciliation statistics DTO.
/// </summary>
public sealed record ReconciliationStats
{
    public int TotalMatches { get; init; }
    public int ConfirmedMatches { get; init; }
    public int PendingMatches { get; init; }
    public int RejectedMatches { get; init; }
    public decimal MatchRate { get; init; }  // Percentage of transactions matched
    public Money TotalMatchedAmount { get; init; }
    public Money TotalUnmatchedAmount { get; init; }
    public decimal AverageMatchConfidence { get; init; }
    public int UnmatchedBankTransactions { get; init; }
    public int UnmatchedJournalEntries { get; init; }
}

/// <summary>
/// Reconciliation health report DTO - identifies potential issues.
/// </summary>
public sealed record ReconciliationHealthReport
{
    public DateTime GeneratedAt { get; init; }
    public int AgedUnmatchedCount { get; init; }  // Unmatched items older than 30 days
    public Money AgedUnmatchedAmount { get; init; }
    public int LowConfidenceMatchCount { get; init; }  // Matches with < 60% confidence
    public decimal HighVariancePercentage { get; init; }  // % of matches with > 1% variance
    public string HealthStatus { get; init; }  // "Excellent", "Good", "Fair", "Poor"
    public List<string> Recommendations { get; init; }
}
