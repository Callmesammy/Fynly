namespace AiCFO.Domain.Entities;

/// <summary>
/// ChartOfAccounts - Aggregate root for managing company's accounts.
/// Maintains the complete structure of accounts used for recording transactions.
/// </summary>
public class ChartOfAccounts : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string CompanyName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<ChartAccountEntry> _accounts = new();
    public IReadOnlyList<ChartAccountEntry> Accounts => _accounts.AsReadOnly();

    // EF Core constructor
    protected ChartOfAccounts() { }

    public ChartOfAccounts(
        Guid id,
        Guid tenantId,
        string companyName,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name cannot be empty", nameof(companyName));

        Id = id;
        TenantId = tenantId;
        CompanyName = companyName;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsArchived = false;
    }

    /// <summary>
    /// Add an account to the chart of accounts.
    /// </summary>
    public void AddAccount(
        AccountCode code,
        string name,
        AccountType type,
        AccountSubType subType,
        string? description,
        Guid addedBy)
    {
        if (_accounts.Any(a => a.Code.Code == code.Code))
            throw new InvalidOperationException($"Account with code {code} already exists");

        var entry = new ChartAccountEntry(
            id: Guid.NewGuid(),
            chartOfAccountsId: Id,
            code: code,
            name: name,
            type: type,
            subType: subType,
            description: description,
            createdBy: addedBy);

        _accounts.Add(entry);
    }

    /// <summary>
    /// Update an existing account.
    /// </summary>
    public void UpdateAccount(
        AccountCode code,
        string newName,
        string? description,
        Guid updatedBy)
    {
        var account = _accounts.FirstOrDefault(a => a.Code.Code == code.Code);
        if (account is null)
            throw new InvalidOperationException($"Account with code {code} not found");

        account.Update(newName, description, updatedBy);
    }

    /// <summary>
    /// Archive (disable) an account.
    /// </summary>
    public void ArchiveAccount(AccountCode code, Guid archivedBy)
    {
        var account = _accounts.FirstOrDefault(a => a.Code.Code == code.Code);
        if (account is null)
            throw new InvalidOperationException($"Account with code {code} not found");

        account.Archive(archivedBy);
    }

    /// <summary>
    /// Get account by code.
    /// </summary>
    public ChartAccountEntry? GetAccount(AccountCode code) =>
        _accounts.FirstOrDefault(a => a.Code.Code == code.Code);

    /// <summary>
    /// Get all active accounts of a specific type.
    /// </summary>
    public IReadOnlyList<ChartAccountEntry> GetAccountsByType(AccountType type) =>
        _accounts.Where(a => a.Type == type && !a.IsArchived).ToList().AsReadOnly();
}

/// <summary>
/// ChartAccountEntry - Represents a single account in the Chart of Accounts.
/// </summary>
public class ChartAccountEntry : Entity
{
    public Guid ChartOfAccountsId { get; private set; }
    public AccountCode Code { get; private set; }
    public string Name { get; private set; }
    public AccountType Type { get; private set; }
    public AccountSubType SubType { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public bool IsArchived { get; private set; }

    // Navigation properties
    public ChartOfAccounts? ChartOfAccounts { get; private set; }

    // EF Core constructor
    protected ChartAccountEntry() { }

    public ChartAccountEntry(
        Guid id,
        Guid chartOfAccountsId,
        AccountCode code,
        string name,
        AccountType type,
        AccountSubType subType,
        string? description,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name cannot be empty", nameof(name));

        Id = id;
        ChartOfAccountsId = chartOfAccountsId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name;
        Type = type;
        SubType = subType;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsArchived = false;
    }

    public void Update(string newName, string? description, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Account name cannot be empty", nameof(newName));

        Name = newName;
        Description = description;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive(Guid archivedBy)
    {
        IsArchived = true;
        UpdatedBy = archivedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unarchive(Guid unarchivedBy)
    {
        IsArchived = false;
        UpdatedBy = unarchivedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the normal balance direction for this account.
    /// </summary>
    public string NormalBalance => Type is AccountType.Asset or AccountType.Expense ? "Debit" : "Credit";
}
