namespace AiCFO.Infrastructure.BankIntegration;

using System.Net.Http.Json;
using System.Text.Json;
using AiCFO.Domain.ValueObjects;

/// <summary>
/// Flutterwave bank provider integration.
/// Implements OAuth2 flow for Flutterwave connections.
/// </summary>
public class FlutterwaveProvider : IBankProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FlutterwaveProvider> _logger;

    private const string BaseUrl = "https://api.flutterwave.com";
    private const string AuthorizationEndpoint = "https://auth.flutterwave.co";

    public BankProvider ProviderType => BankProvider.Flutterwave;

    public FlutterwaveProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FlutterwaveProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<OAuth2AuthorizationResult> GetAuthorizationUrlAsync(
        Guid tenantId,
        string callbackUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = _configuration["BankProviders:Flutterwave:ClientId"];
            var state = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tenantId.ToString()));

            var authUrl = $"{AuthorizationEndpoint}/oauth/authorize?" +
                $"client_id={clientId}" +
                $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                $"&state={state}" +
                $"&response_type=code" +
                $"&scope=full";

            _logger.LogInformation("Generated Flutterwave authorization URL for tenant {TenantId}", tenantId);

            return Task.FromResult(new OAuth2AuthorizationResult
            {
                Success = true,
                AuthorizationUrl = authUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Flutterwave authorization URL for tenant {TenantId}", tenantId);
            return Task.FromResult(new OAuth2AuthorizationResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    public async Task<OAuth2TokenResult> ExchangeCodeForTokenAsync(
        Guid tenantId,
        string authorizationCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = _configuration["BankProviders:Flutterwave:ClientId"];
            var clientSecret = _configuration["BankProviders:Flutterwave:ClientSecret"];

            var request = new
            {
                code = authorizationCode,
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "authorization_code"
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{AuthorizationEndpoint}/oauth/token",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Flutterwave token exchange failed: {Error}", errorContent);
                return new OAuth2TokenResult
                {
                    Success = false,
                    Error = "Failed to exchange authorization code"
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<FlutterwaveTokenResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize Flutterwave token response");

            _logger.LogInformation("Successfully exchanged authorization code for tenant {TenantId}", tenantId);

            return new OAuth2TokenResult
            {
                Success = true,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Flutterwave token exchange for tenant {TenantId}", tenantId);
            return new OAuth2TokenResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<OAuth2TokenResult> RefreshTokenAsync(
        Guid tenantId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var clientId = _configuration["BankProviders:Flutterwave:ClientId"];
            var clientSecret = _configuration["BankProviders:Flutterwave:ClientSecret"];

            var request = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "refresh_token",
                refresh_token = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{AuthorizationEndpoint}/oauth/token",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new OAuth2TokenResult
                {
                    Success = false,
                    Error = "Failed to refresh token"
                };
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<FlutterwaveTokenResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize Flutterwave token response");

            _logger.LogInformation("Successfully refreshed token for tenant {TenantId}", tenantId);

            return new OAuth2TokenResult
            {
                Success = true,
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token refresh for tenant {TenantId}", tenantId);
            return new OAuth2TokenResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<List<BankAccountData>> GetAccountsAsync(
        Guid tenantId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v3/accounts");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Flutterwave accounts: {StatusCode}", response.StatusCode);
                return new List<BankAccountData>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var accountsResponse = JsonSerializer.Deserialize<FlutterwaveAccountsResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize Flutterwave accounts response");

            var accounts = accountsResponse.Data
                .Select(acc => new BankAccountData
                {
                    AccountId = acc.Id,
                    AccountNumber = acc.AccountNumber,
                    AccountName = acc.AccountName,
                    Balance = acc.AvailableBalance,
                    Currency = acc.Currency
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} accounts for tenant {TenantId}", accounts.Count, tenantId);

            return accounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Flutterwave accounts for tenant {TenantId}", tenantId);
            return new List<BankAccountData>();
        }
    }

    public async Task<List<BankTransactionData>> GetTransactionsAsync(
        Guid tenantId,
        string accessToken,
        string accountId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{BaseUrl}/v3/accounts/{accountId}/transactions?start_date={startDateStr}&end_date={endDateStr}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Flutterwave transactions: {StatusCode}", response.StatusCode);
                return new List<BankTransactionData>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var transactionsResponse = JsonSerializer.Deserialize<FlutterwaveTransactionsResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize Flutterwave transactions response");

            var transactions = transactionsResponse.Data
                .Select(txn => new BankTransactionData
                {
                    TransactionId = txn.Id,
                    TransactionDate = txn.CreatedAt,
                    Amount = txn.Amount,
                    TransactionType = MapTransactionType(txn.Type),
                    Description = txn.Narration,
                    Reference = txn.Reference,
                    CounterpartyName = txn.CounterpartyName,
                    CounterpartyAccount = txn.CounterpartyAccount
                })
                .ToList();

            _logger.LogInformation("Retrieved {Count} transactions for tenant {TenantId}", transactions.Count, tenantId);

            return transactions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Flutterwave transactions for tenant {TenantId}", tenantId);
            return new List<BankTransactionData>();
        }
    }

    public async Task<(bool Success, decimal Balance, string Currency)> GetAccountBalanceAsync(
        Guid tenantId,
        string accessToken,
        string accountId,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v3/accounts/{accountId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Flutterwave account balance: {StatusCode}", response.StatusCode);
                return (false, 0, string.Empty);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var accountResponse = JsonSerializer.Deserialize<FlutterwaveAccountResponse>(content) ?? throw new InvalidOperationException("Failed to deserialize Flutterwave account response");

            _logger.LogInformation("Retrieved balance for tenant {TenantId}", tenantId);

            return (true, accountResponse.Data.AvailableBalance, accountResponse.Data.Currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Flutterwave account balance for tenant {TenantId}", tenantId);
            return (false, 0, string.Empty);
        }
    }

    private BankTransactionType MapTransactionType(string flutterwaveType)
    {
        return flutterwaveType.ToLower() switch
        {
            "debit" => BankTransactionType.Debit,
            "credit" => BankTransactionType.Credit,
            "transfer" => BankTransactionType.Transfer,
            "fee" => BankTransactionType.Fee,
            "interest" => BankTransactionType.Interest,
            _ => BankTransactionType.Debit
        };
    }

    #region Flutterwave API Response Types

    private class FlutterwaveTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private class FlutterwaveAccountsResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public List<FlutterwaveAccount> Data { get; set; } = new();
    }

    private class FlutterwaveAccountResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public FlutterwaveAccount Data { get; set; } = new();
    }

    private class FlutterwaveAccount
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("account_number")]
        public string AccountNumber { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("account_name")]
        public string AccountName { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("available_balance")]
        public decimal AvailableBalance { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("currency")]
        public string Currency { get; set; } = "NGN";
    }

    private class FlutterwaveTransactionsResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public List<FlutterwaveTransaction> Data { get; set; } = new();
    }

    private class FlutterwaveTransaction
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("narration")]
        public string Narration { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("counterparty_name")]
        public string? CounterpartyName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("counterparty_account")]
        public string? CounterpartyAccount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}
