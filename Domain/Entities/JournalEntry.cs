namespace AiCFO.Domain.Entities;

/// <summary>
/// JournalEntry - Aggregate root representing a journal entry (transaction record).
/// Double-entry accounting: Every entry must have equal debits and credits.
/// </summary>
public class JournalEntry : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string ReferenceNumber { get; private set; }
    public DateTime EntryDate { get; private set; }
    public string Description { get; private set; }
    public Money TotalDebits { get; private set; }
    public Money TotalCredits { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedBy { get; private set; }

    private readonly List<JournalLine> _lines = new();
    public IReadOnlyList<JournalLine> Lines => _lines.AsReadOnly();

    // EF Core constructor
    protected JournalEntry() { }

    public JournalEntry(
        Guid id,
        Guid tenantId,
        string referenceNumber,
        DateTime entryDate,
        string description,
        Guid createdBy,
        CurrencyCode currencyCode)
    {
        if (string.IsNullOrWhiteSpace(referenceNumber))
            throw new ArgumentException("Reference number cannot be empty", nameof(referenceNumber));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        Id = id;
        TenantId = tenantId;
        ReferenceNumber = referenceNumber;
        EntryDate = entryDate;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        Status = JournalEntryStatus.Draft;
        TotalDebits = Money.Create(0m, currencyCode);
        TotalCredits = Money.Create(0m, currencyCode);
    }

    /// <summary>
    /// Add a journal line (debit or credit).
    /// </summary>
    public void AddLine(
        AccountCode accountCode,
        string description,
        Money amount,
        bool isDebit,
        Guid lineCreatedBy)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a posted entry");

        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        var line = new JournalLine(
            id: Guid.NewGuid(),
            journalEntryId: Id,
            accountCode: accountCode,
            description: description,
            amount: amount,
            isDebit: isDebit,
            lineNumber: (short)(_lines.Count + 1),
            createdBy: lineCreatedBy);

        _lines.Add(line);
        UpdateTotals();
    }

    /// <summary>
    /// Remove a journal line.
    /// </summary>
    public void RemoveLine(Guid lineId)
    {
        if (Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException("Cannot remove lines from a posted entry");

        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null)
            throw new InvalidOperationException("Line not found");

        _lines.Remove(line);
        UpdateTotals();
    }

    /// <summary>
    /// Validate journal entry (debits = credits).
    /// </summary>
    public bool IsValid => Math.Abs(TotalDebits.Amount - TotalCredits.Amount) < 0.01m;

    /// <summary>
    /// Post the journal entry (make permanent).
    /// </summary>
    public void Post(Guid postedBy)
    {
        if (Status == JournalEntryStatus.Posted)
            throw new InvalidOperationException("Entry is already posted");

        if (!IsValid)
            throw new InvalidOperationException(
                $"Journal entry is not balanced. Debits: {TotalDebits.Amount}, Credits: {TotalCredits.Amount}");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot post an entry with no lines");

        Status = JournalEntryStatus.Posted;
        PostedAt = DateTime.UtcNow;
        PostedBy = postedBy;
    }

    /// <summary>
    /// Void a posted entry (creates reversal entry).
    /// </summary>
    public void Void(Guid voidedBy)
    {
        if (Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException("Only posted entries can be voided");

        Status = JournalEntryStatus.Voided;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = voidedBy;
    }

    private void UpdateTotals()
    {
        var currency = _lines.FirstOrDefault()?.Amount.Currency.Code ?? CurrencyCode.USD;
        var debits = _lines.Where(l => l.IsDebit).Sum(l => l.Amount.Amount);
        var credits = _lines.Where(l => !l.IsDebit).Sum(l => l.Amount.Amount);

        TotalDebits = Money.Create(debits, currency);
        TotalCredits = Money.Create(credits, currency);
    }
}

/// <summary>
/// Journal entry status enumeration.
/// </summary>
public enum JournalEntryStatus
{
    /// <summary>
    /// Entry is being prepared, not yet posted.
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Entry is posted and permanent.
    /// </summary>
    Posted = 2,

    /// <summary>
    /// Entry has been voided (reversed).
    /// </summary>
    Voided = 3,
}

/// <summary>
/// JournalLine - Represents a single debit or credit line in a journal entry.
/// </summary>
public class JournalLine : Entity
{
    public Guid JournalEntryId { get; private set; }
    public AccountCode AccountCode { get; private set; }
    public string Description { get; private set; }
    public Money Amount { get; private set; }
    public bool IsDebit { get; private set; }
    public short LineNumber { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Navigation properties
    public JournalEntry? JournalEntry { get; private set; }

    // EF Core constructor
    protected JournalLine() { }

    public JournalLine(
        Guid id,
        Guid journalEntryId,
        AccountCode accountCode,
        string description,
        Money amount,
        bool isDebit,
        short lineNumber,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Id = id;
        JournalEntryId = journalEntryId;
        AccountCode = accountCode ?? throw new ArgumentNullException(nameof(accountCode));
        Description = description;
        Amount = amount;
        IsDebit = isDebit;
        LineNumber = lineNumber;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the debit or credit amount.
    /// </summary>
    public Money DebitAmount => IsDebit ? Amount : Money.Create(0m, Amount.Currency.Code);
    public Money CreditAmount => !IsDebit ? Amount : Money.Create(0m, Amount.Currency.Code);
}
