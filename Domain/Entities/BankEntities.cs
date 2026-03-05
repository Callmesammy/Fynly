namespace AiCFO.Domain.Entities;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// BankConnection - Aggregate root representing a connection to a bank via OAuth2.
/// Manages credentials, sync status, and connection lifecycle.
/// </summary>
public class BankConnection : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public BankProvider Provider { get; private set; }
    public BankCode BankCode { get; private set; }
    public string BankName { get; private set; }
    public BankAccountStatus Status { get; private set; }
    public string? AccessToken { get; private set; }
    public DateTime? TokenExpiresAt { get; private set; }
    public DateTime? LastSyncAt { get; private set; }
    public string? SyncError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private readonly List<BankAccount> _accounts = new();
    public IReadOnlyList<BankAccount> Accounts => _accounts.AsReadOnly();

    // EF Core constructor
    protected BankConnection() { }

    public BankConnection(
        Guid id,
        Guid tenantId,
        BankProvider provider,
        BankCode bankCode,
        string bankName,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(bankName))
            throw new ArgumentException("Bank name cannot be empty", nameof(bankName));

        Id = id;
        TenantId = tenantId;
        Provider = provider;
        BankCode = bankCode;
        BankName = bankName;
        Status = BankAccountStatus.PendingConnection;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set OAuth2 credentials after successful authorization.
    /// </summary>
    public void SetOAuthCredentials(string accessToken, DateTime expiresAt, string? refreshToken = null, Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        AccessToken = accessToken;
        TokenExpiresAt = expiresAt;
        Status = BankAccountStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Record a successful sync operation.
    /// </summary>
    public void RecordSuccessfulSync(Guid syncedBy)
    {
        LastSyncAt = DateTime.UtcNow;
        SyncError = null;
        Status = BankAccountStatus.Active;
        UpdatedBy = syncedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Record a failed sync operation.
    /// </summary>
    public void RecordFailedSync(string errorMessage, Guid syncedBy)
    {
        SyncError = errorMessage;
        Status = BankAccountStatus.ConnectionFailed;
        UpdatedBy = syncedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a bank account to this connection.
    /// </summary>
    public void AddAccount(
        BankAccountId bankAccountId,
        string accountNumber,
        string accountName,
        Currency currency,
        Guid addedBy)
    {
        var account = new BankAccount(
            id: Guid.NewGuid(),
            bankConnectionId: Id,
            bankAccountId: bankAccountId,
            accountNumber: accountNumber,
            accountName: accountName,
            currency: currency,
            createdBy: addedBy);

        _accounts.Add(account);
    }

    /// <summary>
    /// Archive this connection (stop syncing).
    /// </summary>
    public void Archive(Guid archivedBy)
    {
        Status = BankAccountStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = archivedBy;
    }

    /// <summary>
    /// Check if OAuth token needs refresh.
    /// </summary>
    public bool NeedsTokenRefresh() => 
        TokenExpiresAt.HasValue && 
        TokenExpiresAt.Value <= DateTime.UtcNow.AddMinutes(5);
}

/// <summary>
/// BankAccount - Entity representing a single bank account linked to a bank connection.
/// </summary>
public class BankAccount : Entity
{
    public Guid BankConnectionId { get; private set; }
    public BankAccountId BankAccountId { get; private set; }
    public string AccountNumber { get; private set; }
    public string AccountName { get; private set; }
    public Currency Currency { get; private set; }
    public Money CurrentBalance { get; private set; }
    public DateTime? LastBalanceUpdate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<BankTransaction> _transactions = new();
    public IReadOnlyList<BankTransaction> Transactions => _transactions.AsReadOnly();

    // EF Core constructor
    protected BankAccount() { }

    public BankAccount(
        Guid id,
        Guid bankConnectionId,
        BankAccountId bankAccountId,
        string accountNumber,
        string accountName,
        Currency currency,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ArgumentException("Account number cannot be empty", nameof(accountNumber));

        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name cannot be empty", nameof(accountName));

        Id = id;
        BankConnectionId = bankConnectionId;
        BankAccountId = bankAccountId;
        AccountNumber = accountNumber;
        AccountName = accountName;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
        CurrentBalance = Money.Create(0m, currency.Code);
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update account balance from bank sync.
    /// </summary>
    public void UpdateBalance(Money balance, Guid updatedBy)
    {
        if (balance.Currency.Code != Currency.Code)
            throw new InvalidOperationException("Balance currency must match account currency");

        CurrentBalance = balance;
        LastBalanceUpdate = DateTime.UtcNow;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a transaction from bank sync.
    /// </summary>
    public void AddTransaction(BankTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    /// <summary>
    /// Archive this account.
    /// </summary>
    public void Archive(Guid archivedBy)
    {
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = archivedBy;
    }
}

/// <summary>
/// BankTransaction - Entity representing a transaction synced from the bank.
/// </summary>
public class BankTransaction : Entity
{
    public Guid BankAccountId { get; private set; }
    public string BankTransactionId { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public Money Amount { get; private set; }
    public BankTransactionType TransactionType { get; private set; }
    public string Description { get; private set; }
    public string? Reference { get; private set; }
    public string? CounterpartyName { get; private set; }
    public string? CounterpartyAccount { get; private set; }
    public Guid? LinkedJournalLineId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // EF Core constructor
    protected BankTransaction() { }

    public BankTransaction(
        Guid id,
        Guid bankAccountId,
        string bankTransactionId,
        DateTime transactionDate,
        Money amount,
        BankTransactionType transactionType,
        string description,
        string? reference = null,
        string? counterpartyName = null,
        string? counterpartyAccount = null)
    {
        if (string.IsNullOrWhiteSpace(bankTransactionId))
            throw new ArgumentException("Bank transaction ID cannot be empty", nameof(bankTransactionId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        Id = id;
        BankAccountId = bankAccountId;
        BankTransactionId = bankTransactionId;
        TransactionDate = transactionDate;
        Amount = amount;
        TransactionType = transactionType;
        Description = description;
        Reference = reference;
        CounterpartyName = counterpartyName;
        CounterpartyAccount = counterpartyAccount;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Link this transaction to a journal line for reconciliation.
    /// </summary>
    public void LinkToJournalLine(Guid journalLineId)
    {
        LinkedJournalLineId = journalLineId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlink from journal line.
    /// </summary>
    public void UnlinkFromJournalLine()
    {
        LinkedJournalLineId = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
