namespace AiCFO.Application.Common;

using AiCFO.Domain.ValueObjects;

/// <summary>
/// Bank transaction data from bank API.
/// </summary>
public record BankTransactionData
{
    public string TransactionId { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public decimal Amount { get; init; }
    public BankTransactionType TransactionType { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? Reference { get; init; }
    public string? CounterpartyName { get; init; }
    public string? CounterpartyAccount { get; init; }
}

/// <summary>
/// Bank account data from bank API.
/// </summary>
public record BankAccountData
{
    public string AccountId { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "NGN";
    public List<BankTransactionData> Transactions { get; init; } = new();
}

/// <summary>
/// OAuth2 authorization result.
/// </summary>
public record OAuth2AuthorizationResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? AuthorizationUrl { get; init; }
}

/// <summary>
/// OAuth2 token exchange result.
/// </summary>
public record OAuth2TokenResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? AccessToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? RefreshToken { get; init; }
}

/// <summary>
/// Bank provider abstraction for OAuth2 and API operations.
/// Implementations handle specific bank provider logic (Flutterwave, Paystack, Stripe, etc.)
/// </summary>
public interface IBankProvider
{
    /// <summary>
    /// Provider type identifier.
    /// </summary>
    BankProvider ProviderType { get; }

    /// <summary>
    /// Generate OAuth2 authorization URL for user to authorize.
    /// </summary>
    Task<OAuth2AuthorizationResult> GetAuthorizationUrlAsync(
        Guid tenantId,
        string callbackUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exchange authorization code for access token.
    /// </summary>
    Task<OAuth2TokenResult> ExchangeCodeForTokenAsync(
        Guid tenantId,
        string authorizationCode,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refresh expired access token.
    /// </summary>
    Task<OAuth2TokenResult> RefreshTokenAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get linked accounts for authenticated user.
    /// </summary>
    Task<List<BankAccountData>> GetAccountsAsync(
        Guid tenantId,
        string accessToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get transactions for account within date range.
    /// </summary>
    Task<List<BankTransactionData>> GetTransactionsAsync(
        Guid tenantId,
        string accessToken,
        string accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get account balance.
    /// </summary>
    Task<(bool Success, decimal Balance, string Currency)> GetAccountBalanceAsync(
        Guid tenantId,
        string accessToken,
        string accountId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Factory for creating bank provider instances.
/// </summary>
public interface IBankProviderFactory
{
    /// <summary>
    /// Create a bank provider instance.
    /// </summary>
    IBankProvider CreateProvider(BankProvider providerType);

    /// <summary>
    /// Check if provider is supported.
    /// </summary>
    bool IsProviderSupported(BankProvider providerType);
}
