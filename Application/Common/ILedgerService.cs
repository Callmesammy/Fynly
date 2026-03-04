namespace AiCFO.Application.Common;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Service abstraction for ledger operations (reading/writing to chart of accounts and journal).
/// Implementation in Infrastructure layer.
/// </summary>
public interface ILedgerService
{
    // Chart of Accounts operations
    Task<ChartOfAccounts?> GetChartOfAccountsAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<ChartOfAccounts> CreateChartOfAccountsAsync(Guid tenantId, string companyName, Guid createdBy, CancellationToken cancellationToken);
    Task<bool> AddAccountAsync(Guid tenantId, AccountCode code, string name, AccountType type, AccountSubType subType, string? description, Guid addedBy, CancellationToken cancellationToken);
    Task<bool> UpdateAccountAsync(Guid tenantId, AccountCode code, string newName, string? description, Guid updatedBy, CancellationToken cancellationToken);
    Task<bool> ArchiveAccountAsync(Guid tenantId, AccountCode code, Guid archivedBy, CancellationToken cancellationToken);
    Task<ChartAccountEntry?> GetAccountAsync(Guid tenantId, AccountCode code, CancellationToken cancellationToken);
    Task<IReadOnlyList<ChartAccountEntry>> GetAccountsByTypeAsync(Guid tenantId, AccountType type, CancellationToken cancellationToken);

    // Journal Entry operations
    Task<JournalEntry> CreateJournalEntryAsync(Guid tenantId, string referenceNumber, DateTime entryDate, string description, Guid createdBy, CurrencyCode currencyCode, CancellationToken cancellationToken);
    Task<JournalEntry?> GetJournalEntryAsync(Guid tenantId, Guid entryId, CancellationToken cancellationToken);
    Task<bool> AddJournalLineAsync(Guid tenantId, Guid entryId, AccountCode accountCode, string description, Money amount, bool isDebit, Guid lineCreatedBy, CancellationToken cancellationToken);
    Task<bool> RemoveJournalLineAsync(Guid tenantId, Guid entryId, Guid lineId, CancellationToken cancellationToken);
    Task<bool> PostJournalEntryAsync(Guid tenantId, Guid entryId, Guid postedBy, CancellationToken cancellationToken);
    Task<bool> VoidJournalEntryAsync(Guid tenantId, Guid entryId, Guid voidedBy, CancellationToken cancellationToken);
    Task<IReadOnlyList<JournalEntry>> GetJournalEntriesByDateRangeAsync(Guid tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken);
    Task<Money> GetAccountBalanceAsync(Guid tenantId, AccountCode accountCode, CancellationToken cancellationToken);

    // Trial Balance and Reporting
    Task<IReadOnlyList<(AccountCode Code, string Name, Money Balance)>> GetTrialBalanceAsync(Guid tenantId, CancellationToken cancellationToken);
}
