# Phase 3.2 - End-to-End Integration Test Scenarios

**Status:** ✅ READY FOR TESTING  
**Build:** ✅ GREEN  
**Environment:** Local Development / Staging

---

## 🎯 Test Scenario 1: Complete OAuth2 Flow

### Preconditions
- ✅ Application running on http://localhost:5000
- ✅ Flutterwave sandbox credentials configured
- ✅ Database initialized with schema
- ✅ User authenticated with valid JWT token

### Test Steps

#### 1.1 - Initiate Bank Connection
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

**Expected Result:**
```json
{
  "statusCode": 200,
  "data": {
    "connectionId": "550e8400-e29b-41d4-a716-446655440000",
    "authorizationUrl": "https://auth.flutterwave.co/oauth/authorize?client_id=pk_test_...&redirect_uri=...&state=550e8400-e29b-41d4-a716-446655440000:123:789"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 200
- [ ] Response contains connectionId (UUID format)
- [ ] Response contains authorizationUrl
- [ ] AuthorizationUrl contains client_id from config
- [ ] AuthorizationUrl contains redirect_uri
- [ ] AuthorizationUrl contains state parameter
- [ ] State parameter format: {tenantId}:{userId}:{connectionId}
- [ ] Database record created with ConnectionPending status

#### 1.2 - User Authorizes at Bank
```
Open authorizationUrl in browser
User logs in with Flutterwave sandbox credentials
User grants permission to application
Bank redirects to: http://localhost:5000/api/bank/connections/oauth-callback?code=AUTH_CODE&state=STATE
```

**Verification Checklist:**
- [ ] Browser redirected to authorization URL
- [ ] Flutterwave login page loads
- [ ] Permission dialog appears
- [ ] User can authorize
- [ ] Redirect happens to callback endpoint

#### 1.3 - OAuth Callback Handler
```
Automatic (happens in background when bank redirects)
GET /api/bank/connections/oauth-callback?code=AUTH_CODE&state=STATE
```

**Verification Checklist:**
- [ ] Callback endpoint receives auth code
- [ ] Callback endpoint receives state parameter
- [ ] State parameter is validated
- [ ] Connection ID extracted from state
- [ ] ExchangeOAuthCodeCommand created
- [ ] Tokens exchanged with Flutterwave
- [ ] Credentials stored in database
- [ ] HTTP redirect to success page (or JSON response if API)

#### 1.4 - Verify Connection Created
```bash
curl -X GET http://localhost:5000/api/bank/connections \
  -H "Authorization: Bearer eyJhbGc..."
```

**Expected Result:**
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

**Verification Checklist:**
- [ ] Connection appears in list
- [ ] Status is "Active"
- [ ] Provider is "Flutterwave"
- [ ] authorizedAt is recent timestamp
- [ ] Only current tenant's connections returned (multi-tenancy check)

---

## 🎯 Test Scenario 2: Bank Account Retrieval

### Preconditions
- ✅ OAuth2 flow complete (Scenario 1 passed)
- ✅ Bank connection has valid OAuth credentials

### Test Steps

#### 2.1 - Sync Bank Transactions
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

**Expected Result:**
```json
{
  "statusCode": 200,
  "data": {
    "message": "Successfully synced 15 transactions"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 200
- [ ] Sync message returns transaction count
- [ ] Database contains new BankTransaction records
- [ ] Each transaction has ConnectionId
- [ ] Each transaction has TenantId
- [ ] Transaction dates are within requested range
- [ ] Transaction amounts are positive decimals
- [ ] Transaction types are valid enum values

#### 2.2 - Get Unreconciled Transactions
```bash
curl -X GET http://localhost:5000/api/bank/transactions/unreconciled \
  -H "Authorization: Bearer eyJhbGc..."
```

**Expected Result:**
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

**Verification Checklist:**
- [ ] Status code is 200
- [ ] Response contains transaction list
- [ ] Transaction IDs are unique
- [ ] Amounts are positive or negative (debit/credit)
- [ ] Dates are valid ISO 8601 format
- [ ] Transaction types are valid values
- [ ] Status shows "Unreconciled"
- [ ] Only unreconciled transactions returned

---

## 🎯 Test Scenario 3: Multi-Tenancy Isolation

### Preconditions
- ✅ Two different tenants with separate JWT tokens
- ✅ Tenant A has OAuth connections
- ✅ Tenant B has different OAuth connections

### Test Steps

#### 3.1 - Tenant A Views Connections
```bash
# Using Tenant A's JWT token
curl -X GET http://localhost:5000/api/bank/connections \
  -H "Authorization: Bearer TENANT_A_TOKEN"
```

**Expected Result:**
```json
{
  "statusCode": 200,
  "data": [
    {
      "id": "connection-a1",
      "bankName": "Flutterwave (Tenant A)"
    }
  ]
}
```

**Verification Checklist:**
- [ ] Only Tenant A's connections returned
- [ ] Tenant B's connections NOT visible
- [ ] Connection belongs to Tenant A's ID

#### 3.2 - Tenant B Views Connections
```bash
# Using Tenant B's JWT token
curl -X GET http://localhost:5000/api/bank/connections \
  -H "Authorization: Bearer TENANT_B_TOKEN"
```

**Expected Result:**
```json
{
  "statusCode": 200,
  "data": [
    {
      "id": "connection-b1",
      "bankName": "Flutterwave (Tenant B)"
    }
  ]
}
```

**Verification Checklist:**
- [ ] Only Tenant B's connections returned
- [ ] Tenant A's connections NOT visible
- [ ] Connection belongs to Tenant B's ID

#### 3.3 - Verify Data Isolation in Database
```sql
-- Verify global query filters working
SELECT COUNT(*) FROM bank_connections WHERE tenant_id = 'tenant-a-id';
SELECT COUNT(*) FROM bank_connections WHERE tenant_id = 'tenant-b-id';
SELECT COUNT(*) FROM bank_oauth_credentials WHERE tenant_id = 'tenant-a-id';
```

**Verification Checklist:**
- [ ] Each tenant has separate connection records
- [ ] No cross-tenant data leakage
- [ ] Each credential record has correct TenantId
- [ ] Global query filters active and working

---

## 🎯 Test Scenario 4: Error Handling

### Preconditions
- ✅ Application running
- ✅ Invalid/expired credentials available

### Test Steps

#### 4.1 - Missing Authorization Code
```bash
# Manually navigate to: http://localhost:5000/api/bank/connections/oauth-callback?state=STATE
# Without code parameter
```

**Expected Result:**
```json
{
  "statusCode": 400,
  "data": {
    "message": "Missing authorization code or state"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 400
- [ ] Error message is clear
- [ ] No sensitive data exposed
- [ ] Log entry created for debugging

#### 4.2 - Invalid State Parameter
```bash
curl -X GET "http://localhost:5000/api/bank/connections/oauth-callback?code=ABC&state=invalid"
```

**Expected Result:**
```json
{
  "statusCode": 400,
  "data": {
    "message": "Invalid state parameter"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 400
- [ ] Error message indicates invalid state
- [ ] No exception thrown (graceful error handling)
- [ ] Log entry created

#### 4.3 - Provider Not Supported
```bash
curl -X POST http://localhost:5000/api/bank/connections/initiate \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{
    "provider": 1,
    "bankCode": "PAYSTACK",
    "bankName": "Paystack"
  }'
```

**Expected Result:**
```json
{
  "statusCode": 400,
  "data": {
    "message": "Failed to initiate bank connection: Paystack provider not yet implemented"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 400
- [ ] Error message indicates not implemented
- [ ] No unhandled exceptions
- [ ] Logging captures the error

#### 4.4 - Invalid JWT Token
```bash
curl -X GET http://localhost:5000/api/bank/connections \
  -H "Authorization: Bearer INVALID_TOKEN"
```

**Expected Result:**
```json
{
  "statusCode": 401,
  "data": {
    "message": "Unauthorized"
  }
}
```

**Verification Checklist:**
- [ ] Status code is 401
- [ ] Endpoint returns Unauthorized (not 500)
- [ ] No sensitive data exposed
- [ ] Request rejected by middleware

---

## 🎯 Test Scenario 5: Security Validation

### Preconditions
- ✅ Application running
- ✅ HTTPS enabled (staging/production)
- ✅ Security headers configured

### Test Steps

#### 5.1 - Bearer Token in Authorization Header
```bash
# Verify token in Authorization header (not in URL)
curl -X POST http://localhost:5000/api/bank/sync \
  -H "Authorization: Bearer eyJhbGc..." \
  -H "Content-Type: application/json" \
  -d '{"connectionId": "..."}' -v
```

**Verification Checklist:**
- [ ] Token in Authorization header (not query string)
- [ ] Token not logged to stdout
- [ ] Token not exposed in response
- [ ] Token sent via HTTPS only (production)

#### 5.2 - State Parameter CSRF Protection
```javascript
// Verify state parameter in OAuth URL
const authUrl = response.data.authorizationUrl;
const stateParam = new URL(authUrl).searchParams.get('state');
const parts = stateParam.split(':');

// Verify format: {tenantId}:{userId}:{connectionId}
console.assert(parts.length === 3, 'State parameter has 3 parts');
console.assert(isValidGuid(parts[0]), 'TenantId is GUID');
console.assert(isValidGuid(parts[1]), 'UserId is GUID');
console.assert(isValidGuid(parts[2]), 'ConnectionId is GUID');
```

**Verification Checklist:**
- [ ] State parameter included in authorization URL
- [ ] State parameter is random/unique per request
- [ ] State parameter includes tenantId (multi-tenant)
- [ ] State parameter includes userId (user tracking)
- [ ] State parameter includes connectionId (request correlation)

#### 5.3 - Credential Storage
```sql
-- Verify credentials stored securely
SELECT id, access_token, refresh_token, expires_at 
FROM bank_oauth_credentials 
WHERE connection_id = '...';
```

**Verification Checklist:**
- [ ] Tokens stored in database (not in memory)
- [ ] Tokens encrypted (if encryption configured)
- [ ] ExpiresAt is populated (token management)
- [ ] CreatedBy audit field set (user tracking)
- [ ] CreatedAt audit field set (timestamp)
- [ ] UpdatedAt audit field updated on refresh

#### 5.4 - No Sensitive Data in Logs
```bash
# Check application logs
grep -i "token\|secret\|password" app.log | wc -l
# Should return 0 results (no sensitive data)
```

**Verification Checklist:**
- [ ] No access tokens in logs
- [ ] No refresh tokens in logs
- [ ] No secrets in logs
- [ ] No passwords in logs
- [ ] Generic error messages in responses
- [ ] Detailed logs only in debug console

---

## 🎯 Test Scenario 6: Performance & Load

### Preconditions
- ✅ Application running
- ✅ Database with realistic data
- ✅ Load testing tools installed (e.g., Apache JMeter, k6)

### Test Steps

#### 6.1 - Concurrent OAuth Initiations
```bash
# Simulate 10 concurrent requests
ab -n 10 -c 10 -H "Authorization: Bearer eyJhbGc..." \
  -p payload.json \
  http://localhost:5000/api/bank/connections/initiate
```

**Verification Checklist:**
- [ ] All requests complete successfully
- [ ] Response time < 500ms per request
- [ ] No connection timeouts
- [ ] Database handles concurrent inserts
- [ ] Each request gets unique state parameter

#### 6.2 - Transaction Sync Performance
```bash
# Measure sync performance with 1000 transactions
time curl -X POST http://localhost:5000/api/bank/sync \
  -H "Authorization: Bearer eyJhbGc..." \
  -d '{"connectionId": "...", "startDate": "2024-01-01", "endDate": "2024-12-31"}'
```

**Verification Checklist:**
- [ ] Sync completes in < 5 seconds
- [ ] Database indexes used (query plan reviewed)
- [ ] Memory usage stable
- [ ] No N+1 query patterns
- [ ] Batch processing used if available

---

## 📋 Test Execution Checklist

### Pre-Test Setup
- [ ] Build successful (`dotnet build`)
- [ ] Application starts without errors
- [ ] Database migrations applied
- [ ] Flutterwave sandbox credentials configured
- [ ] JWT token generated for test user
- [ ] Second JWT token generated for multi-tenancy test

### Scenario Execution
- [ ] Scenario 1: OAuth2 Flow (7 steps)
- [ ] Scenario 2: Account Retrieval (2 steps)
- [ ] Scenario 3: Multi-Tenancy (3 steps)
- [ ] Scenario 4: Error Handling (4 steps)
- [ ] Scenario 5: Security (4 steps)
- [ ] Scenario 6: Performance (2 steps)

### Post-Test Validation
- [ ] All scenarios passed
- [ ] Build still green
- [ ] No memory leaks
- [ ] Logs reviewed for errors
- [ ] Database integrity verified

---

## 🎯 Success Criteria

All of the following must be true:

✅ **Build**: GREEN (0 errors, 0 warnings)  
✅ **OAuth2 Flow**: Complete from initiation to token storage  
✅ **API Endpoints**: All responding with correct status codes  
✅ **Multi-Tenancy**: Data properly isolated by TenantId  
✅ **Security**: No sensitive data exposure, CSRF protection active  
✅ **Error Handling**: All error scenarios handled gracefully  
✅ **Performance**: Response times < 500ms (excluding bank I/O)  
✅ **Logging**: Detailed logs available, no sensitive data logged  
✅ **Database**: Transactions properly stored and isolated  

---

## 📊 Test Results Template

```
PHASE 3.2 - INTEGRATION TEST RESULTS
====================================

Test Date: ___________
Tester: ___________
Environment: ___________

Scenario 1 - OAuth2 Flow: _____ PASS / FAIL
  - Initiate: [ ] Pass
  - Authorize: [ ] Pass
  - Callback: [ ] Pass
  - Verify: [ ] Pass

Scenario 2 - Account Retrieval: _____ PASS / FAIL
  - Sync: [ ] Pass
  - Retrieve: [ ] Pass

Scenario 3 - Multi-Tenancy: _____ PASS / FAIL
  - Tenant A: [ ] Pass
  - Tenant B: [ ] Pass
  - Isolation: [ ] Pass

Scenario 4 - Error Handling: _____ PASS / FAIL
  - Missing Code: [ ] Pass
  - Invalid State: [ ] Pass
  - Unsupported Provider: [ ] Pass
  - Invalid Token: [ ] Pass

Scenario 5 - Security: _____ PASS / FAIL
  - Bearer Token: [ ] Pass
  - State CSRF: [ ] Pass
  - Storage: [ ] Pass
  - Logs: [ ] Pass

Scenario 6 - Performance: _____ PASS / FAIL
  - Concurrent: [ ] Pass
  - Sync: [ ] Pass

OVERALL RESULT: _____ PASS / FAIL

Notes:
_________________________________________________________________
_________________________________________________________________

Issues Found:
_________________________________________________________________
_________________________________________________________________

Recommendations:
_________________________________________________________________
_________________________________________________________________
```

---

## 🎓 Notes for Testers

- Use Flutterwave **sandbox** credentials (never production)
- Test with at least 2 different tenant IDs
- Verify database records after each step
- Check application logs for any warnings/errors
- Monitor memory usage during concurrent tests
- Document any performance bottlenecks
- Report security concerns immediately

---

**Status: ✅ READY FOR TESTING**

*All test scenarios are ready to execute. Begin with Scenario 1 (OAuth2 Flow) as it's the foundational test.*
