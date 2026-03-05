# Phase 3.2 - API Usage Examples

## Complete OAuth2 Integration Example

### 1. Frontend Flow - Initiate Bank Connection

```javascript
// Step 1: Call API to initiate connection
const response = await fetch('/api/bank/connections/initiate', {
    method: 'POST',
    headers: {
        'Authorization': `Bearer ${accessToken}`,
        'Content-Type': 'application/json'
    },
    body: JSON.stringify({
        provider: 0,  // BankProvider.Flutterwave
        bankCode: 'FLW',
        bankName: 'Flutterwave'
    })
});

const result = await response.json();
console.log('Connection ID:', result.data.connectionId);
console.log('Authorization URL:', result.data.authorizationUrl);

// Step 2: Redirect user to bank login
window.location.href = result.data.authorizationUrl;
```

### 2. Backend - OAuth Callback Handler

```csharp
// The bank redirects back to:
// GET /api/bank/connections/oauth-callback?code=AUTH_CODE&state=STATE

// Handler automatically:
// 1. Validates state parameter (CSRF protection)
// 2. Exchanges authorization code for tokens
// 3. Stores access token and refresh token
// 4. Returns success response

// Frontend receives:
// {
//   "statusCode": 200,
//   "data": {
//     "message": "Bank connection authorized successfully"
//   }
// }
```

### 3. Sync Bank Transactions

```csharp
// After authorization, sync transactions
var command = new SyncBankTransactionsCommand(
    connectionId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    startDate: DateTime.Parse("2024-01-01"),
    endDate: DateTime.Parse("2024-01-31"));

var result = await mediator.Send(command);

if (result is Result<string>.Success success)
{
    Console.WriteLine($"Synced transactions: {success.Data}");
}
```

---

## cURL Examples

### Initiate Bank Connection
```bash
curl -X POST http://localhost:5000/api/bank/connections/initiate \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{
    "provider": 0,
    "bankCode": "FLW",
    "bankName": "Flutterwave"
  }'
```

**Response:**
```json
{
  "statusCode": 200,
  "data": {
    "connectionId": "550e8400-e29b-41d4-a716-446655440000",
    "authorizationUrl": "https://auth.flutterwave.co/oauth/authorize?client_id=pk_test_..."
  }
}
```

### Get Bank Connections
```bash
curl -X GET http://localhost:5000/api/bank/connections \
  -H "Authorization: Bearer eyJhbGc..."
```

**Response:**
```json
{
  "statusCode": 200,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "provider": "Flutterwave",
      "bankCode": "FLW",
      "bankName": "Flutterwave",
      "status": "Active",
      "authorizedAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

### Sync Transactions
```bash
curl -X POST http://localhost:5000/api/bank/sync \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{
    "connectionId": "550e8400-e29b-41d4-a716-446655440000",
    "startDate": "2024-01-01",
    "endDate": "2024-01-31"
  }'
```

**Response:**
```json
{
  "statusCode": 200,
  "data": {
    "message": "Successfully synced 42 transactions"
  }
}
```

### Get Unreconciled Transactions
```bash
curl -X GET http://localhost:5000/api/bank/transactions/unreconciled \
  -H "Authorization: Bearer eyJhbGc..."
```

**Response:**
```json
{
  "statusCode": 200,
  "data": [
    {
      "id": "txn-001",
      "amount": 1500.00,
      "currency": "NGN",
      "transactionDate": "2024-01-15T10:30:00Z",
      "transactionType": "Credit",
      "description": "Payment from Customer ABC",
      "status": "Unreconciled"
    }
  ]
}
```

---

## Postman Collection Examples

### Environment Variables
```json
{
  "base_url": "http://localhost:5000",
  "access_token": "eyJhbGc...",
  "bank_connection_id": "550e8400-e29b-41d4-a716-446655440000"
}
```

### Request 1: Initiate Bank Connection
```
POST {{base_url}}/api/bank/connections/initiate
Authorization: Bearer {{access_token}}

{
  "provider": 0,
  "bankCode": "FLW",
  "bankName": "Flutterwave"
}

// Save connectionId from response for next requests
```

### Request 2: List Bank Connections
```
GET {{base_url}}/api/bank/connections
Authorization: Bearer {{access_token}}
```

### Request 3: Sync Transactions
```
POST {{base_url}}/api/bank/sync
Authorization: Bearer {{access_token}}

{
  "connectionId": "{{bank_connection_id}}",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31"
}
```

### Request 4: Get Unreconciled Transactions
```
GET {{base_url}}/api/bank/transactions/unreconciled
Authorization: Bearer {{access_token}}
```

---

## C# Client Integration Examples

### Initialize Bank Connection
```csharp
using AiCFO.Application.Features.Bank.Commands;

// Inject IMediator
var initiateCommand = new InitiateBankConnectionCommand(
    provider: BankProvider.Flutterwave,
    bankCode: "FLW",
    bankName: "Flutterwave",
    callbackUrl: "https://yourdomain.com/api/bank/connections/oauth-callback"
);

var result = await mediator.Send(initiateCommand);

if (result is Result<BankConnectionInitiationResponse>.Success success)
{
    var redirectUrl = success.Data.AuthorizationUrl;
    var connectionId = success.Data.ConnectionId;
    
    // Store connectionId in session
    // Redirect user to redirectUrl
}
else if (result is Result<BankConnectionInitiationResponse>.Failure failure)
{
    Console.WriteLine($"Error: {failure.Message}");
}
```

### Handle OAuth Callback
```csharp
using AiCFO.Application.Features.Bank.Commands;

// This is called when bank redirects back
[AllowAnonymous]
[HttpGet("connections/oauth-callback")]
public async Task<IActionResult> HandleOAuthCallback(
    string code,
    string state,
    string? error,
    string? error_description,
    CancellationToken cancellationToken)
{
    if (!string.IsNullOrEmpty(error))
    {
        return BadRequest($"Authorization failed: {error_description}");
    }

    // Parse state to get connectionId
    var stateParts = state.Split(':');
    if (stateParts.Length != 3 || !Guid.TryParse(stateParts[2], out var connectionId))
    {
        return BadRequest("Invalid state parameter");
    }

    // Exchange code for tokens
    var command = new ExchangeOAuthCodeCommand(
        connectionId: connectionId,
        authorizationCode: code,
        callbackUrl: $"{Request.Scheme}://{Request.Host}/api/bank/connections/oauth-callback"
    );

    var result = await mediator.Send(command, cancellationToken);

    if (result is Result<bool>.Success)
    {
        return Ok("Bank connection authorized successfully");
    }
    
    return BadRequest("Failed to authorize bank connection");
}
```

### Query Bank Connections
```csharp
using AiCFO.Application.Features.Bank.Queries;

var query = new GetBankConnectionsQuery();
var result = await mediator.Send(query);

if (result is Result<List<BankConnectionDto>>.Success success)
{
    foreach (var connection in success.Data)
    {
        Console.WriteLine($"Connection: {connection.BankName} ({connection.Status})");
    }
}
```

### Sync Transactions
```csharp
using AiCFO.Application.Features.Bank.Commands;

var command = new SyncBankTransactionsCommand(
    connectionId: Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    startDate: DateTime.Now.AddMonths(-1),
    endDate: DateTime.Now
);

var result = await mediator.Send(command);

if (result is Result<string>.Success success)
{
    Console.WriteLine($"Sync result: {success.Data}");
}
```

---

## Error Handling Examples

### Missing Authorization Code
```json
{
  "statusCode": 400,
  "data": {
    "message": "Missing authorization code or state"
  }
}
```

### Invalid State Parameter
```json
{
  "statusCode": 400,
  "data": {
    "message": "Invalid state parameter"
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

### Connection Not Found
```json
{
  "statusCode": 400,
  "data": {
    "message": "Bank connection not found"
  }
}
```

---

## Configuration for Different Banks

### Flutterwave (Currently Supported)
```json
{
  "BankProviders": {
    "Flutterwave": {
      "ClientId": "pk_test_...",
      "ClientSecret": "sk_test_..."
    }
  }
}
```

### Paystack (Future)
```json
{
  "BankProviders": {
    "Paystack": {
      "ClientId": "pk_live_...",
      "ClientSecret": "sk_live_..."
    }
  }
}
```

### Stripe (Future)
```json
{
  "BankProviders": {
    "Stripe": {
      "ClientId": "ca_...",
      "ClientSecret": "sk_live_..."
    }
  }
}
```

---

## Debugging Tips

### Enable Detailed Logging
```csharp
// In Program.cs
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
```

### Check OAuth State Parameter
```csharp
// In ExchangeOAuthCodeCommand handler
var stateParts = state.Split(':');
Console.WriteLine($"TenantId: {stateParts[0]}");
Console.WriteLine($"UserId: {stateParts[1]}");
Console.WriteLine($"ConnectionId: {stateParts[2]}");
```

### Verify Bank Credentials
```csharp
// In FlutterwaveProvider
private string GetClientId()
{
    var clientId = _configuration["BankProviders:Flutterwave:ClientId"];
    if (string.IsNullOrEmpty(clientId))
        throw new InvalidOperationException("Missing Flutterwave ClientId in configuration");
    return clientId;
}
```

### Test OAuth Flow Offline
```csharp
// Create mock response
var mockTokenResult = new OAuth2TokenResult
{
    Success = true,
    AccessToken = "test_token_123",
    RefreshToken = "test_refresh_456",
    ExpiresAt = DateTime.UtcNow.AddHours(1)
};
```

---

## Testing with Flutterwave Sandbox

1. **Sign Up**: https://sandbox.flutterwave.com
2. **Get Credentials**: Dashboard → Settings → OAuth
3. **Test Bank Accounts**: Use Flutterwave test credentials
4. **Webhook Testing**: Enable webhooks in sandbox
5. **Monitor**: Check logs in Flutterwave dashboard

---

## Production Checklist

- [ ] Production Flutterwave credentials in appsettings.Production.json
- [ ] HTTPS enabled for all OAuth callbacks
- [ ] Redirect URI registered with production Flutterwave
- [ ] Error logging configured to Application Insights
- [ ] Rate limiting enabled on OAuth endpoints
- [ ] CSRF token validation enabled
- [ ] Secrets encrypted in database
- [ ] Token refresh automated
- [ ] Monitoring and alerts configured
- [ ] Load testing completed

---

## Support & References

- **Flutterwave Docs**: https://developer.flutterwave.com
- **OAuth2 RFC**: https://tools.ietf.org/html/rfc6749
- **Bank Integration Guide**: See OAUTH2_REFERENCE.md
- **Checkpoint Details**: See PHASE_3_2_CHECKPOINT.md
