# OAuth2 Bank Integration - Quick Reference

## Complete OAuth2 Flow

### Step 1: Frontend Initiates Connection
```http
POST /api/bank/connections/initiate
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "provider": 0,  // BankProvider.Flutterwave = 0
  "bankCode": "FLW",
  "bankName": "Flutterwave"
}
```

### Step 2: API Returns Authorization URL
```json
{
  "statusCode": 200,
  "data": {
    "connectionId": "550e8400-e29b-41d4-a716-446655440000",
    "authorizationUrl": "https://auth.flutterwave.co/oauth/authorize?client_id=...&redirect_uri=...&state=..."
  }
}
```

### Step 3: Frontend Redirects User to Bank
```javascript
// Frontend redirects user
window.location.href = response.data.authorizationUrl;
```

### Step 4: User Authorizes at Bank
- User logs in to their bank
- Approves application access
- Bank redirects back to: `/api/bank/connections/oauth-callback?code=AUTH_CODE&state=STATE`

### Step 5: Backend Exchanges Code for Tokens
```csharp
// Automatic (happens in OAuth callback endpoint)
GET /api/bank/connections/oauth-callback?code=abc123&state=123:456:789
```

### Step 6: Credentials Stored
- Access token → stored in BankOAuthCredentials.AccessToken
- Refresh token → stored in BankOAuthCredentials.RefreshToken
- Expiry → tracked in BankOAuthCredentials.ExpiresAt
- Connection status → marked as authorized

### Step 7: Use Connection
```http
POST /api/bank/sync
Authorization: Bearer {access_token}
Content-Type: application/json

{
  "connectionId": "550e8400-e29b-41d4-a716-446655440000",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31"
}
```

---

## Configuration

### appsettings.json
```json
{
  "BankProviders": {
    "Flutterwave": {
      "ClientId": "your-flutterwave-client-id",
      "ClientSecret": "your-flutterwave-client-secret"
    }
  }
}
```

### Flutterwave OAuth Setup
1. Go to https://dashboard.flutterwave.com
2. Navigate to Settings → OAuth Applications
3. Create new OAuth application:
   - **Application Name**: AI CFO
   - **Redirect URI**: `https://yourdomain.com/api/bank/connections/oauth-callback`
   - **Scopes**: accounts, transactions
4. Copy **Client ID** and **Client Secret**
5. Add to appsettings.json

---

## API Endpoints

### Initiate Bank Connection
```
POST /api/bank/connections/initiate
Authorization: Bearer {token}
Body: {provider, bankCode, bankName}
Response: {connectionId, authorizationUrl}
```

### OAuth2 Callback (from Bank)
```
GET /api/bank/connections/oauth-callback?code=...&state=...
(No authentication required - bank redirects here)
Response: {message, success}
```

### Direct Credential Storage (Testing)
```
POST /api/bank/connections/{connectionId}/oauth-credentials
Authorization: Bearer {token}
Body: {accessToken, expiresAt, refreshToken}
Response: {message, success}
```

### List Connections
```
GET /api/bank/connections
Authorization: Bearer {token}
Response: [{id, provider, status, authorizedAt, ...}]
```

### Sync Transactions
```
POST /api/bank/sync
Authorization: Bearer {token}
Body: {connectionId, startDate, endDate}
Response: {message, transactionCount}
```

### Get Unreconciled Transactions
```
GET /api/bank/transactions/unreconciled
Authorization: Bearer {token}
Response: [{id, amount, date, description, ...}]
```

---

## Data Models

### BankConnectionInitiationResponse
```csharp
public class BankConnectionInitiationResponse
{
    public Guid ConnectionId { get; set; }
    public string AuthorizationUrl { get; set; }
}
```

### OAuth2TokenResult
```csharp
public record OAuth2TokenResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? AccessToken { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? RefreshToken { get; init; }
}
```

### BankAccountData
```csharp
public record BankAccountData
{
    public string AccountId { get; init; }
    public string AccountNumber { get; init; }
    public string AccountName { get; init; }
    public decimal Balance { get; init; }
    public string Currency { get; init; }
    public List<BankTransactionData> Transactions { get; init; }
}
```

---

## Provider Factory Usage

### Creating a Provider
```csharp
var provider = _bankProviderFactory.CreateProvider(BankProvider.Flutterwave);

// Returns: FlutterwaveProvider instance from DI container
```

### Supported Providers
- ✅ Flutterwave
- ⏳ Paystack (NotImplementedException)
- ⏳ Stripe (NotImplementedException)
- ⏳ Interswitch (NotImplementedException)
- ⏳ OpenBanking (NotImplementedException)

---

## DI Registration

### Program.cs Setup
```csharp
// HttpClient for Flutterwave
builder.Services.AddHttpClient<FlutterwaveProvider>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.flutterwave.com");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

// Factory for provider instantiation
builder.Services.AddScoped<IBankProviderFactory, BankProviderFactory>();
```

---

## State Parameter Format

### Purpose
- CSRF protection
- Multi-tenant isolation
- Connection tracking

### Format
```
{tenantId}:{userId}:{connectionId}
Example: 123e4567-e89b-12d3-a456-426614174000:456:789
```

### Parsing
```csharp
var stateParts = state.Split(':');
var tenantId = Guid.Parse(stateParts[0]);
var userId = Guid.Parse(stateParts[1]);
var connectionId = Guid.Parse(stateParts[2]);
```

---

## Error Handling

### Authorization Failed
```json
{
  "statusCode": 400,
  "data": {
    "message": "Authorization failed: user_denied"
  }
}
```

### Missing Code
```json
{
  "statusCode": 400,
  "data": {
    "message": "Missing authorization code or state"
  }
}
```

### Connection Not Found
```json
{
  "statusCode": 400,
  "data": {
    "message": "Bank connection not found"
  }
}
```

### Token Exchange Failed
```json
{
  "statusCode": 400,
  "data": {
    "message": "Failed to exchange authorization code: invalid_grant"
  }
}
```

---

## Development Checklist

- [ ] Flutterwave OAuth credentials added to appsettings.json
- [ ] HttpClient timeout configured (30 seconds)
- [ ] Redirect URI registered with Flutterwave
- [ ] Test bank account created
- [ ] OAuth flow tested end-to-end
- [ ] State parameter validation working
- [ ] Token expiry tracking confirmed
- [ ] Error handling tested
- [ ] Multi-tenant isolation verified
- [ ] Logging verified in Application Insights

---

## Next Steps

### Checkpoint 3.3: Reconciliation Engine
- Transaction reconciliation rules
- Auto-matching algorithms
- Manual reconciliation endpoints
- Reconciliation audit trail

### Future Providers
- Paystack OAuth2
- Stripe integration
- Interswitch integration
- OpenBanking integration

---

## References

- Flutterwave OAuth2: https://developer.flutterwave.com/docs/integration/oauth2
- Flutterwave APIs: https://developer.flutterwave.com/reference
- OAuth2.0: https://tools.ietf.org/html/rfc6749
- Multi-Tenancy Security: https://docs.microsoft.com/en-us/azure/architecture/guide/multitenant/service
