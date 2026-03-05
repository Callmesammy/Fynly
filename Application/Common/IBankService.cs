namespace AiCFO.Application.Common;

using AiCFO.Domain.Entities;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Bank service abstraction for managing bank connections and transactions.
/// </summary>
public interface IBankService
{
    // ==================== Bank Connection Operations ====================

    /// <summary>
    /// Create a new bank connection (OAuth2 pending).
    /// </summary>
    Task<BankConnection> CreateBankConnectionAsync(
        Guid tenantId,
        BankProvider provider,
        BankCode bankCode,
        string bankName,
        Guid createdBy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get bank connection by ID.
    /// </summary>
    Task<BankConnection?> GetBankConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get all active bank connections for tenant.
    /// </summary>
    Task<IReadOnlyList<BankConnection>> GetBankConnectionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Store OAuth2 credentials after user authorization.
    /// </summary>
    Task<bool> SetOAuthCredentialsAsync(
        Guid tenantId,
        Guid connectionId,
        string accessToken,
        DateTime expiresAt,
        string? refreshToken,
        Guid updatedBy,
        CancellationToken cancellationToken);

    /// <summary>
    /// Archive a bank connection (stop syncing).
    /// </summary>
    Task<bool> ArchiveBankConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        Guid archivedBy,
        CancellationToken cancellationToken);

    // ==================== Bank Account Operations ====================

    /// <summary>
    /// Get bank account by ID.
    /// </summary>
    Task<BankAccount?> GetBankAccountAsync(
        Guid tenantId,
        Guid accountId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get all bank accounts for a connection.
    /// </summary>
    Task<IReadOnlyList<BankAccount>> GetBankAccountsByConnectionAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get all bank accounts for tenant.
    /// </summary>
    Task<IReadOnlyList<BankAccount>> GetAllBankAccountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Archive a bank account.
    /// </summary>
    Task<bool> ArchiveBankAccountAsync(
        Guid tenantId,
        Guid accountId,
        Guid archivedBy,
        CancellationToken cancellationToken);

    // ==================== Bank Transaction Operations ====================

    /// <summary>
    /// Get bank transaction by ID.
    /// </summary>
    Task<BankTransaction?> GetBankTransactionAsync(
        Guid tenantId,
        Guid transactionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for an account within date range.
    /// </summary>
    Task<IReadOnlyList<BankTransaction>> GetTransactionsByAccountAsync(
        Guid tenantId,
        Guid accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get unreconciled transactions (not linked to journal lines).
    /// </summary>
    Task<IReadOnlyList<BankTransaction>> GetUnreconciledTransactionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Link a bank transaction to a journal line for reconciliation.
    /// </summary>
    Task<bool> LinkTransactionToJournalLineAsync(
        Guid tenantId,
        Guid transactionId,
        Guid journalLineId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unlink a transaction from its journal line.
    /// </summary>
    Task<bool> UnlinkTransactionFromJournalLineAsync(
        Guid tenantId,
        Guid transactionId,
        CancellationToken cancellationToken);

    // ==================== Sync Operations ====================

    /// <summary>
    /// Synchronize transactions from bank (background job).
    /// </summary>
    Task<(bool Success, string Message)> SyncBankTransactionsAsync(
        Guid tenantId,
        Guid connectionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refresh OAuth2 token if expired.
    /// </summary>
    Task<bool> RefreshBankTokenAsync(
        Guid tenantId,
        Guid connectionId,
        CancellationToken cancellationToken);
}
