namespace AiCFO.Infrastructure.Services;

using AiCFO.Application.Common;
using AiCFO.Domain.Entities;
using AiCFO.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of bank service.
/// Manages bank connections, accounts, and transactions.
/// </summary>
public class BankService : IBankService
{
    private readonly AppDbContext _dbContext;
    private readonly ITenantContext _tenantContext;

    public BankService(AppDbContext dbContext, ITenantContext tenantContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    #region Bank Connection Operations

    public async Task<BankConnection> CreateBankConnectionAsync(
        Guid tenantId,
        BankProvider provider,
        BankCode bankCode,
        string bankName,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var connection = new BankConnection(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            provider: provider,
            bankCode: bankCode,
            bankName: bankName,
            createdBy: createdBy);

        _dbContext.BankConnections.Add(connection);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return connection;
    }

    public async Task<BankConnection?> GetBankConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankConnections
            .FirstOrDefaultAsync(bc => bc.TenantId == tenantId && bc.Id == connectionId, cancellationToken);
    }

    public async Task<IReadOnlyList<BankConnection>> GetBankConnectionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankConnections
            .Where(bc => bc.TenantId == tenantId && bc.Status != BankAccountStatus.Archived)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SetOAuthCredentialsAsync(
        Guid tenantId,
        Guid connectionId,
        string accessToken,
        DateTime expiresAt,
        string? refreshToken,
        Guid updatedBy,
        CancellationToken cancellationToken)
    {
        var connection = await GetBankConnectionAsync(tenantId, connectionId, cancellationToken);
        if (connection == null)
            return false;

        try
        {
            connection.SetOAuthCredentials(accessToken, expiresAt, refreshToken, updatedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ArchiveBankConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        Guid archivedBy,
        CancellationToken cancellationToken)
    {
        var connection = await GetBankConnectionAsync(tenantId, connectionId, cancellationToken);
        if (connection == null)
            return false;

        try
        {
            connection.Archive(archivedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Bank Account Operations

    public async Task<BankAccount?> GetBankAccountAsync(
        Guid tenantId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankAccounts
            .FirstOrDefaultAsync(ba => ba.Id == accountId && !ba.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyList<BankAccount>> GetBankAccountsByConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankAccounts
            .Where(ba => ba.BankConnectionId == connectionId && !ba.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankAccount>> GetAllBankAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var connectionIds = await _dbContext.BankConnections
            .Where(bc => bc.TenantId == tenantId)
            .Select(bc => bc.Id)
            .ToListAsync(cancellationToken);

        return await _dbContext.BankAccounts
            .Where(ba => connectionIds.Contains(ba.BankConnectionId) && !ba.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ArchiveBankAccountAsync(
        Guid tenantId,
        Guid accountId,
        Guid archivedBy,
        CancellationToken cancellationToken)
    {
        var account = await GetBankAccountAsync(tenantId, accountId, cancellationToken);
        if (account == null)
            return false;

        try
        {
            account.Archive(archivedBy);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Bank Transaction Operations

    public async Task<BankTransaction?> GetBankTransactionAsync(
        Guid tenantId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankTransactions
            .FirstOrDefaultAsync(bt => bt.Id == transactionId, cancellationToken);
    }

    public async Task<IReadOnlyList<BankTransaction>> GetTransactionsByAccountAsync(
        Guid tenantId,
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await _dbContext.BankTransactions
            .Where(bt => bt.BankAccountId == accountId 
                && bt.TransactionDate >= startDate 
                && bt.TransactionDate <= endDate)
            .OrderByDescending(bt => bt.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankTransaction>> GetUnreconciledTransactionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var accountIds = await _dbContext.BankAccounts
            .Where(ba => !ba.IsArchived)
            .Select(ba => ba.Id)
            .ToListAsync(cancellationToken);

        return await _dbContext.BankTransactions
            .Where(bt => accountIds.Contains(bt.BankAccountId) && bt.LinkedJournalLineId == null)
            .OrderByDescending(bt => bt.TransactionDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> LinkTransactionToJournalLineAsync(
        Guid tenantId,
        Guid transactionId,
        Guid journalLineId,
        CancellationToken cancellationToken)
    {
        var transaction = await GetBankTransactionAsync(tenantId, transactionId, cancellationToken);
        if (transaction == null)
            return false;

        try
        {
            transaction.LinkToJournalLine(journalLineId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnlinkTransactionFromJournalLineAsync(
        Guid tenantId,
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var transaction = await GetBankTransactionAsync(tenantId, transactionId, cancellationToken);
        if (transaction == null)
            return false;

        try
        {
            transaction.UnlinkFromJournalLine();
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Sync Operations

    public async Task<(bool Success, string Message)> SyncBankTransactionsAsync(
        Guid tenantId,
        Guid connectionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var connection = await GetBankConnectionAsync(tenantId, connectionId, cancellationToken);
        if (connection == null)
            return (false, "Bank connection not found");

        if (connection.Status != BankAccountStatus.Active)
            return (false, "Bank connection is not active");

        try
        {
            // TODO: Implement actual bank API sync logic
            // This is a placeholder - actual implementation would call bank provider APIs
            connection.RecordSuccessfulSync(_tenantContext.UserId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return (true, "Bank transactions synced successfully");
        }
        catch (Exception ex)
        {
            connection.RecordFailedSync(ex.Message, _tenantContext.UserId);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return (false, $"Sync failed: {ex.Message}");
        }
    }

    public async Task<bool> RefreshBankTokenAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await GetBankConnectionAsync(tenantId, connectionId, cancellationToken);
        if (connection == null)
            return false;

        if (!connection.NeedsTokenRefresh())
            return true;

        try
        {
            // TODO: Implement actual OAuth2 token refresh logic
            // This is a placeholder - actual implementation would call bank provider OAuth endpoints
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
