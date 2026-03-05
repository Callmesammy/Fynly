namespace AiCFO.Application.Features.Reconciliation.Dtos;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// Request DTO for auto-matching reconciliation.
/// </summary>
public sealed record AutoMatchingRequest
{
    public decimal VarianceThreshold { get; init; } = 2m;  // 2% default
    public int DateTolerance { get; init; } = 7;            // 7 days default
}

/// <summary>
/// Response DTO for matching results.
/// </summary>
public sealed record MatchingResult
{
    public int ExactMatchesCreated { get; init; }
    public int PartialMatchesCreated { get; init; }
    public int DateRangeMatchesCreated { get; init; }
    public int TotalMatchesCreated => ExactMatchesCreated + PartialMatchesCreated + DateRangeMatchesCreated;
    public int UnmatchedBankTransactions { get; init; }
    public int UnmatchedJournalEntries { get; init; }
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for confirming a reconciliation match.
/// </summary>
public sealed record ConfirmMatchRequest
{
    public string? Notes { get; init; }
}

/// <summary>
/// Request DTO for rejecting a reconciliation match.
/// </summary>
public sealed record RejectMatchRequest
{
    public string Reason { get; init; } = null!;
}

/// <summary>
/// Request DTO for creating a reconciliation session.
/// </summary>
public sealed record CreateSessionRequest
{
    public string SessionName { get; init; } = null!;
    public CurrencyCode CurrencyCode { get; init; } = CurrencyCode.NGN;
}

/// <summary>
/// Response DTO for reconciliation match.
/// </summary>
public sealed record ReconciliationMatchDto
{
    public Guid MatchId { get; init; }
    public Guid BankTransactionId { get; init; }
    public Guid JournalEntryId { get; init; }
    public string Status { get; init; } = null!;
    public decimal ConfidencePercentage { get; init; }
    public string MatchType { get; init; } = null!;
    public decimal VarianceAmount { get; init; }
    public int DaysDifference { get; init; }
    public string? Notes { get; init; }
    public DateTime MatchedAt { get; init; }
}

/// <summary>
/// Response DTO for reconciliation statistics.
/// </summary>
public sealed record ReconciliationStatsDto
{
    public int TotalMatches { get; init; }
    public int ConfirmedMatches { get; init; }
    public int PendingMatches { get; init; }
    public int RejectedMatches { get; init; }
    public decimal MatchRate { get; init; }
    public decimal AverageMatchConfidence { get; init; }
    public int UnmatchedBankTransactions { get; init; }
    public int UnmatchedJournalEntries { get; init; }
}

/// <summary>
/// Response DTO for unmatched bank transaction.
/// </summary>
public sealed record UnmatchedBankTransactionDto
{
    public Guid TransactionId { get; init; }
    public string TransactionReference { get; init; } = null!;
    public decimal Amount { get; init; }
    public DateTime TransactionDate { get; init; }
    public string Description { get; init; } = null!;
    public int DaysUnmatched { get; init; }
}

/// <summary>
/// Response DTO for unmatched journal entry.
/// </summary>
public sealed record UnmatchedJournalEntryDto
{
    public Guid EntryId { get; init; }
    public string ReferenceNumber { get; init; } = null!;
    public decimal Amount { get; init; }
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = null!;
    public int DaysUnmatched { get; init; }
}
