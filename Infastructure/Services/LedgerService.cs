namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using AiCFO.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of ledger operations service.
/// Manages chart of accounts, journal entries, and reporting queries.
/// </summary>
public class LedgerService : ILedgerService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public LedgerService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Chart of Accounts Operations

    public async Task<ChartOfAccounts?> GetChartOfAccountsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.ChartsOfAccounts
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
    }

    public async Task<ChartOfAccounts> CreateChartOfAccountsAsync(Guid tenantId, string companyName, Guid createdBy, CancellationToken cancellationToken)
    {
        // Check if chart already exists for tenant
        var existing = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Chart of Accounts already exists for tenant {tenantId}");

        var chartOfAccounts = new ChartOfAccounts(Guid.NewGuid(), tenantId, companyName, createdBy);
        _dbContext.ChartsOfAccounts.Add(chartOfAccounts);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return chartOfAccounts;
    }

    public async Task<bool> AddAccountAsync(Guid tenantId, AccountCode code, string name, AccountType type, AccountSubType subType, string? description, Guid addedBy, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            throw new InvalidOperationException($"Chart of Accounts not found for tenant {tenantId}");

        try
        {
            chartOfAccounts.AddAccount(code, name, type, subType, description, addedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateAccountAsync(Guid tenantId, AccountCode code, string newName, string? description, Guid updatedBy, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            throw new InvalidOperationException($"Chart of Accounts not found for tenant {tenantId}");

        try
        {
            chartOfAccounts.UpdateAccount(code, newName, description, updatedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ArchiveAccountAsync(Guid tenantId, AccountCode code, Guid archivedBy, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            throw new InvalidOperationException($"Chart of Accounts not found for tenant {tenantId}");

        try
        {
            chartOfAccounts.ArchiveAccount(code, archivedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ChartAccountEntry?> GetAccountAsync(Guid tenantId, AccountCode code, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            return null;

        return chartOfAccounts.GetAccount(code);
    }

    public async Task<IReadOnlyList<ChartAccountEntry>> GetAccountsByTypeAsync(Guid tenantId, AccountType type, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            return new List<ChartAccountEntry>();

        return chartOfAccounts.GetAccountsByType(type);
    }

    #endregion

    #region Journal Entry Operations

    public async Task<JournalEntry> CreateJournalEntryAsync(Guid tenantId, string referenceNumber, DateTime entryDate, string description, Guid createdBy, CurrencyCode currencyCode, CancellationToken cancellationToken)
    {
        var entry = new JournalEntry(Guid.NewGuid(), tenantId, referenceNumber, entryDate, description, createdBy, currencyCode);
        _dbContext.JournalEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public async Task<JournalEntry?> GetJournalEntryAsync(Guid tenantId, Guid entryId, CancellationToken cancellationToken)
    {
        return await _dbContext.JournalEntries
            .FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == entryId, cancellationToken);
    }

    public async Task<bool> AddJournalLineAsync(Guid tenantId, Guid entryId, AccountCode accountCode, string description, Money amount, bool isDebit, Guid lineCreatedBy, CancellationToken cancellationToken)
    {
        var entry = await GetJournalEntryAsync(tenantId, entryId, cancellationToken);
        if (entry == null)
            return false;

        try
        {
            entry.AddLine(accountCode, description, amount, isDebit, lineCreatedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveJournalLineAsync(Guid tenantId, Guid entryId, Guid lineId, CancellationToken cancellationToken)
    {
        var entry = await GetJournalEntryAsync(tenantId, entryId, cancellationToken);
        if (entry == null)
            return false;

        try
        {
            entry.RemoveLine(lineId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PostJournalEntryAsync(Guid tenantId, Guid entryId, Guid postedBy, CancellationToken cancellationToken)
    {
        var entry = await GetJournalEntryAsync(tenantId, entryId, cancellationToken);
        if (entry == null)
            return false;

        if (!entry.IsValid)
            throw new InvalidOperationException("Journal entry does not balance (debits must equal credits)");

        try
        {
            entry.Post(postedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> VoidJournalEntryAsync(Guid tenantId, Guid entryId, Guid voidedBy, CancellationToken cancellationToken)
    {
        var entry = await GetJournalEntryAsync(tenantId, entryId, cancellationToken);
        if (entry == null)
            return false;

        try
        {
            entry.Void(voidedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        return await _dbContext.JournalEntries
            .Where(j => j.TenantId == tenantId && j.EntryDate >= startDate && j.EntryDate <= endDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Money> GetAccountBalanceAsync(Guid tenantId, AccountCode accountCode, CancellationToken cancellationToken)
    {
        var balance = await _dbContext.AccountBalances
            .FirstOrDefaultAsync(ab => ab.TenantId == tenantId && ab.AccountCode == accountCode, cancellationToken);

        return balance?.CurrentBalance ?? Money.Create(0, CurrencyCode.NGN);
    }

    #endregion

    #region Reporting

    public async Task<IReadOnlyList<(AccountCode Code, string Name, Money Balance)>> GetTrialBalanceAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var chartOfAccounts = await GetChartOfAccountsAsync(tenantId, cancellationToken);
        if (chartOfAccounts == null)
            return new List<(AccountCode, string, Money)>();

        var accounts = chartOfAccounts.Accounts;
        var result = new List<(AccountCode, string, Money)>();

        foreach (var account in accounts)
        {
            var balance = await GetAccountBalanceAsync(tenantId, account.Code, cancellationToken);
            result.Add((account.Code, account.Name, balance));
        }

        return result;
    }

    #endregion
}
