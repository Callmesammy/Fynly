namespace AiCFO.Domain.ValueObjects;

/// <summary>
/// Bank provider enumeration - supported banking platforms.
/// </summary>
public enum BankProvider
{
    /// <summary>Flutterwave - Pan-African payment platform</summary>
    Flutterwave = 1,

    /// <summary>Stripe - Global payment processing</summary>
    Stripe = 2,

    /// <summary>Paystack - African payments</summary>
    Paystack = 3,

    /// <summary>Interswitch - Pan-African payments</summary>
    Interswitch = 4,

    /// <summary>Open Banking - Generic banking API</summary>
    OpenBanking = 5
}

/// <summary>
/// Bank transaction type enumeration.
/// </summary>
public enum BankTransactionType
{
    /// <summary>Money received</summary>
    Credit = 1,

    /// <summary>Money sent</summary>
    Debit = 2,

    /// <summary>Transfer between own accounts</summary>
    Transfer = 3,

    /// <summary>Fee or charge</summary>
    Fee = 4,

    /// <summary>Interest earned</summary>
    Interest = 5
}

/// <summary>
/// Bank account status enumeration.
/// </summary>
public enum BankAccountStatus
{
    /// <summary>Active and syncing</summary>
    Active = 1,

    /// <summary>Temporarily disabled</summary>
    Inactive = 2,

    /// <summary>Awaiting OAuth2 connection</summary>
    PendingConnection = 3,

    /// <summary>Connection failed or expired</summary>
    ConnectionFailed = 4,

    /// <summary>Archived (no longer syncing)</summary>
    Archived = 5
}

/// <summary>
/// Bank account identifier - unique per bank.
/// </summary>
public sealed record BankAccountId
{
    public string Value { get; }

    public BankAccountId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Bank account ID cannot be empty", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Bank identifier - unique per financial institution.
/// </summary>
public sealed record BankCode
{
    public string Value { get; }

    public BankCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Bank code cannot be empty", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// OAuth2 credentials for bank connection.
/// Stores encrypted access/refresh tokens.
/// </summary>
public sealed record BankOAuthCredentials
{
    public string AccessToken { get; }
    public string? RefreshToken { get; }
    public DateTime ExpiresAt { get; }
    public BankProvider Provider { get; }

    public BankOAuthCredentials(string accessToken, string? refreshToken, DateTime expiresAt, BankProvider provider)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentException("Token expiry must be in the future", nameof(expiresAt));

        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        Provider = provider;
    }

    /// <summary>
    /// Check if token is expired or about to expire (within 5 minutes).
    /// </summary>
    public bool IsExpiredOrExpiring() => ExpiresAt <= DateTime.UtcNow.AddMinutes(5);

    /// <summary>
    /// Check if token is still valid.
    /// </summary>
    public bool IsValid() => !IsExpiredOrExpiring();
}
