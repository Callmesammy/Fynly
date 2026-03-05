namespace AiCFO.Domain.Entities;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// ReconciliationMatch - Aggregate root representing a reconciliation between a bank transaction and a journal entry.
/// Tracks the matching relationship, confidence scores, variances, and reconciliation status.
/// </summary>
public class ReconciliationMatch : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public Guid BankTransactionId { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public ReconciliationStatus Status { get; private set; }
    public MatchScore MatchScore { get; private set; }
    public VarianceAmount VarianceAmount { get; private set; }
    public TimelineVariance TimelineVariance { get; private set; }
    public string? Notes { get; private set; }
    public DateTime MatchedAt { get; private set; }
    public Guid MatchedBy { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public Guid? ConfirmedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private readonly List<ReconciliationAuditLog> _auditLogs = new();
    public IReadOnlyList<ReconciliationAuditLog> AuditLogs => _auditLogs.AsReadOnly();

    // EF Core constructor
    protected ReconciliationMatch() { }

    public ReconciliationMatch(
        Guid id,
        Guid tenantId,
        Guid bankTransactionId,
        Guid journalEntryId,
        MatchScore matchScore,
        VarianceAmount varianceAmount,
        TimelineVariance timelineVariance,
        Guid createdBy)
    {
        if (bankTransactionId == Guid.Empty)
            throw new ArgumentException("Bank transaction ID cannot be empty", nameof(bankTransactionId));

        if (journalEntryId == Guid.Empty)
            throw new ArgumentException("Journal entry ID cannot be empty", nameof(journalEntryId));

        Id = id;
        TenantId = tenantId;
        BankTransactionId = bankTransactionId;
        JournalEntryId = journalEntryId;
        Status = ReconciliationStatus.Proposed;
        MatchScore = matchScore ?? throw new ArgumentNullException(nameof(matchScore));
        VarianceAmount = varianceAmount ?? throw new ArgumentNullException(nameof(varianceAmount));
        TimelineVariance = timelineVariance ?? throw new ArgumentNullException(nameof(timelineVariance));
        CreatedBy = createdBy;
        MatchedBy = createdBy;
        MatchedAt = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;

        // Add initial audit log
        _auditLogs.Add(new ReconciliationAuditLog(
            id: Guid.NewGuid(),
            reconciliationMatchId: id,
            action: "CREATED",
            previousValue: null,
            newValue: Status.ToString(),
            createdBy: createdBy));
    }

    /// <summary>
    /// Confirm the reconciliation match (user approval).
    /// </summary>
    public void Confirm(Guid confirmedBy, string? notes = null)
    {
        if (Status == ReconciliationStatus.Rejected)
            throw new InvalidOperationException("Cannot confirm a rejected reconciliation");

        Status = ReconciliationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedBy = confirmedBy;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = confirmedBy;

        _auditLogs.Add(new ReconciliationAuditLog(
            id: Guid.NewGuid(),
            reconciliationMatchId: Id,
            action: "CONFIRMED",
            previousValue: "Proposed",
            newValue: "Confirmed",
            createdBy: confirmedBy));
    }

    /// <summary>
    /// Reject the reconciliation match.
    /// </summary>
    public void Reject(Guid rejectedBy, string reason)
    {
        if (Status == ReconciliationStatus.Rejected)
            throw new InvalidOperationException("Match is already rejected");

        if (Status == ReconciliationStatus.Confirmed)
            throw new InvalidOperationException("Cannot reject a confirmed reconciliation");

        Status = ReconciliationStatus.Rejected;
        Notes = reason;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = rejectedBy;

        _auditLogs.Add(new ReconciliationAuditLog(
            id: Guid.NewGuid(),
            reconciliationMatchId: Id,
            action: "REJECTED",
            previousValue: Status.ToString(),
            newValue: "Rejected",
            createdBy: rejectedBy));
    }

    /// <summary>
    /// Add notes to the reconciliation match.
    /// </summary>
    public void AddNotes(string notes, Guid addedBy)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("Notes cannot be empty", nameof(notes));

        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = addedBy;

        _auditLogs.Add(new ReconciliationAuditLog(
            id: Guid.NewGuid(),
            reconciliationMatchId: Id,
            action: "NOTES_ADDED",
            previousValue: Notes,
            newValue: notes,
            createdBy: addedBy));
    }
}

/// <summary>
/// ReconciliationAuditLog - Entity tracking audit trail for reconciliation matches.
/// </summary>
public class ReconciliationAuditLog : Entity
{
    public Guid ReconciliationMatchId { get; private set; }
    public string Action { get; private set; }          // CREATED, CONFIRMED, REJECTED, NOTES_ADDED
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    protected ReconciliationAuditLog() { }

    public ReconciliationAuditLog(
        Guid id,
        Guid reconciliationMatchId,
        string action,
        string? previousValue,
        string? newValue,
        Guid createdBy)
    {
        Id = id;
        ReconciliationMatchId = reconciliationMatchId;
        Action = action ?? throw new ArgumentNullException(nameof(action));
        PreviousValue = previousValue;
        NewValue = newValue;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// ReconciliationSession - Aggregate root representing a reconciliation session.
/// Tracks batch reconciliation operations with statistics and status.
/// </summary>
public class ReconciliationSession : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public DateTime SessionDate { get; private set; }
    public string SessionName { get; private set; }
    public int TotalTransactionsProcessed { get; private set; }
    public int MatchesFound { get; private set; }
    public int MatchesConfirmed { get; private set; }
    public int MatchesRejected { get; private set; }
    public Money TotalMatchedAmount { get; private set; }
    public Money TotalUnmatchedAmount { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private readonly List<Guid> _matchIds = new();
    public IReadOnlyList<Guid> MatchIds => _matchIds.AsReadOnly();

    // EF Core constructor
    protected ReconciliationSession() { }

    public ReconciliationSession(
        Guid id,
        Guid tenantId,
        string sessionName,
        Guid createdBy,
        CurrencyCode currencyCode)
    {
        if (string.IsNullOrWhiteSpace(sessionName))
            throw new ArgumentException("Session name cannot be empty", nameof(sessionName));

        Id = id;
        TenantId = tenantId;
        SessionName = sessionName;
        SessionDate = DateTime.UtcNow;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        TotalMatchedAmount = Money.Create(0, currencyCode);
        TotalUnmatchedAmount = Money.Create(0, currencyCode);
    }

    /// <summary>
    /// Add a match to the session.
    /// </summary>
    public void AddMatch(Guid matchId, Money matchedAmount, bool isConfirmed = false)
    {
        _matchIds.Add(matchId);
        MatchesFound++;

        if (isConfirmed)
        {
            MatchesConfirmed++;
            TotalMatchedAmount = TotalMatchedAmount.Add(matchedAmount);
        }
    }

    /// <summary>
    /// Complete the reconciliation session.
    /// </summary>
    public void Complete(Guid completedBy)
    {
        CompletedAt = DateTime.UtcNow;
        UpdatedBy = completedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update session statistics after confirmation.
    /// </summary>
    public void UpdateStatistics(int totalProcessed, Money unmatchedAmount)
    {
        TotalTransactionsProcessed = totalProcessed;
        TotalUnmatchedAmount = unmatchedAmount;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// UnmatchedBankTransaction - Entity representing a bank transaction that could not be matched.
/// </summary>
public class UnmatchedBankTransaction : Entity
{
    public Guid TenantId { get; private set; }
    public Guid BankTransactionId { get; private set; }
    public string TransactionReference { get; private set; }
    public Money Amount { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; }
    public int DaysUnmatched { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    protected UnmatchedBankTransaction() { }

    public UnmatchedBankTransaction(
        Guid id,
        Guid tenantId,
        Guid bankTransactionId,
        string transactionReference,
        Money amount,
        DateTime transactionDate,
        string description,
        int daysUnmatched)
    {
        Id = id;
        TenantId = tenantId;
        BankTransactionId = bankTransactionId;
        TransactionReference = transactionReference;
        Amount = amount;
        TransactionDate = transactionDate;
        Description = description;
        DaysUnmatched = daysUnmatched;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// UnmatchedJournalEntry - Entity representing a journal entry that could not be matched to a bank transaction.
/// </summary>
public class UnmatchedJournalEntry : Entity
{
    public Guid TenantId { get; private set; }
    public Guid JournalEntryId { get; private set; }
    public string ReferenceNumber { get; private set; }
    public Money Amount { get; private set; }
    public DateTime EntryDate { get; private set; }
    public string Description { get; private set; }
    public int DaysUnmatched { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    protected UnmatchedJournalEntry() { }

    public UnmatchedJournalEntry(
        Guid id,
        Guid tenantId,
        Guid journalEntryId,
        string referenceNumber,
        Money amount,
        DateTime entryDate,
        string description,
        int daysUnmatched)
    {
        Id = id;
        TenantId = tenantId;
        JournalEntryId = journalEntryId;
        ReferenceNumber = referenceNumber;
        Amount = amount;
        EntryDate = entryDate;
        Description = description;
        DaysUnmatched = daysUnmatched;
        CreatedAt = DateTime.UtcNow;
    }
}
