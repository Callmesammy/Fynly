# 🧪 API Testing Guide - Postman/cURL Examples

## Authentication Flow

### 1. Register New User
```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass@123",
    "name": "John Doe",
    "tenantId": "tenant-001"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "rT5Z8kL9pQ2wX...",
    "user": {
      "id": "user-uuid",
      "email": "user@example.com",
      "name": "John Doe",
      "createdAt": "2024-01-15T10:30:00Z"
    }
  }
}
```

### 2. Login
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass@123",
    "tenantId": "tenant-001"
  }'
```

### 3. Refresh Token
```bash
curl -X POST http://localhost:8080/api/auth/refresh \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_REFRESH_TOKEN" \
  -d '{
    "refreshToken": "rT5Z8kL9pQ2wX..."
  }'
```

---

## Ledger & Accounting APIs

### 1. Create Chart of Accounts
```bash
export TOKEN="your-access-token"

curl -X POST http://localhost:8080/api/ledger/chart-of-accounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "companyName": "Acme Corporation",
    "fiscalYearStart": "2024-01-01"
  }'
```

### 2. Add Account to Chart
```bash
curl -X POST http://localhost:8080/api/ledger/accounts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "chartOfAccountsId": "coa-uuid",
    "accountCode": "10010",
    "accountName": "Cash",
    "accountType": "Asset",
    "accountSubType": "Cash"
  }'
```

### 3. Record Journal Entry
```bash
curl -X POST http://localhost:8080/api/ledger/journal-entries \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "chartOfAccountsId": "coa-uuid",
    "description": "Initial cash deposit",
    "transactionDate": "2024-01-15",
    "referenceNumber": "INV-001"
  }'
```

### 4. Add Journal Line (Debit/Credit)
```bash
curl -X POST http://localhost:8080/api/ledger/journal-lines \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "journalEntryId": "entry-uuid",
    "accountCode": "10010",
    "debitAmount": 1000.00,
    "creditAmount": 0.00,
    "description": "Cash deposit"
  }'
```

### 5. Post Journal Entry
```bash
curl -X POST http://localhost:8080/api/ledger/journal-entries/{journalEntryId}/post \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{}'
```

### 6. Get Trial Balance
```bash
curl -X GET "http://localhost:8080/api/ledger/trial-balance?chartOfAccountsId=coa-uuid" \
  -H "Authorization: Bearer $TOKEN"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalDebits": 5000.00,
    "totalCredits": 5000.00,
    "accounts": [
      {
        "accountCode": "10010",
        "accountName": "Cash",
        "debitBalance": 1000.00,
        "creditBalance": 0.00
      }
    ]
  }
}
```

### 7. Get Account Balance
```bash
curl -X GET "http://localhost:8080/api/ledger/account-balance?accountCode=10010" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Bank Integration APIs

### 1. Initiate Bank Connection (OAuth)
```bash
curl -X POST http://localhost:8080/api/bank/connections/initiate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "bankProvider": "Flutterwave",
    "businessName": "Acme Corp"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "authorizationUrl": "https://api.flutterwave.com/oauth/authorize?client_id=...",
    "connectionId": "conn-uuid"
  }
}
```

### 2. Exchange OAuth Code
```bash
curl -X POST http://localhost:8080/api/bank/connections/exchange-code \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "code": "auth-code-from-bank",
    "connectionId": "conn-uuid"
  }'
```

### 3. Sync Bank Transactions
```bash
curl -X POST http://localhost:8080/api/bank/sync \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "bankConnectionId": "conn-uuid",
    "startDate": "2024-01-01",
    "endDate": "2024-01-31"
  }'
```

---

## Reconciliation APIs

### 1. Auto-Match Transactions
```bash
curl -X POST http://localhost:8080/api/reconciliation/auto-match \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "variancePercentage": 2.0,
    "dateToleranceDays": 7
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalMatches": 45,
    "exactMatches": 38,
    "partialMatches": 7,
    "confidenceAverage": 98.5
  }
}
```

### 2. Get Reconciliation Matches
```bash
curl -X GET "http://localhost:8080/api/reconciliation/matches?status=Confirmed&limit=10" \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Confirm Match
```bash
curl -X POST http://localhost:8080/api/reconciliation/matches/{matchId}/confirm \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "notes": "Verified with bank statement"
  }'
```

### 4. Get Reconciliation Stats
```bash
curl -X GET http://localhost:8080/api/reconciliation/stats \
  -H "Authorization: Bearer $TOKEN"
```

---

## AI & Analytics APIs

### 1. Trigger Anomaly Analysis
```bash
curl -X POST http://localhost:8080/api/ai/analyze/anomalies \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "lookbackDays": 90,
    "severityFilter": "High"
  }'
```

**Response:**
```json
{
  "success": true,
  "data": {
    "totalAnomalies": 5,
    "criticalAnomalies": 1,
    "averageConfidence": 87.3,
    "analyzedAt": "2024-01-15T10:30:00Z"
  }
}
```

### 2. Run Health Assessment
```bash
curl -X POST http://localhost:8080/api/ai/health \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d {}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "overallScore": 78,
    "rating": "Good",
    "healthDetails": {
      "liquidity": 85,
      "profitability": 72,
      "solvency": 65,
      "efficiency": 88,
      "growth": 75
    },
    "assessedAt": "2024-01-15T10:30:00Z"
  }
}
```

### 3. Generate Predictions
```bash
curl -X POST http://localhost:8080/api/ai/predictions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "forecastMonths": 6
  }'
```

### 4. Get Recommendations
```bash
curl -X GET "http://localhost:8080/api/ai/recommendations?topCount=5&priorityFilter=High" \
  -H "Authorization: Bearer $TOKEN"
```

### 5. Get AI Dashboard (All Insights)
```bash
curl -X GET "http://localhost:8080/api/ai/dashboard?topAnomalies=5&topRecommendations=5" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Alert Management APIs

### 1. Create Alert Threshold
```bash
curl -X POST http://localhost:8080/api/alerts \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "name": "Revenue Drop Alert",
    "description": "Alert when daily revenue drops 20% below average",
    "thresholdType": "Revenue",
    "operator": "LessThan",
    "value": 8000.00,
    "severity": "High"
  }'
```

### 2. Get Recent Alerts
```bash
curl -X GET "http://localhost:8080/api/alerts?days=7&severityFilter=Critical" \
  -H "Authorization: Bearer $TOKEN"
```

---

## Health Report APIs

### 1. Generate Health Report
```bash
curl -X POST http://localhost:8080/api/reports/health \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "reportType": "Overall",
    "includePredictions": true
  }'
```

### 2. Schedule Report
```bash
curl -X POST http://localhost:8080/api/reports/health/{reportId}/schedule \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "frequency": "Weekly",
    "dayOfWeek": "Monday",
    "time": "09:00"
  }'
```

### 3. Get Report Statistics
```bash
curl -X GET http://localhost:8080/api/reports/health/statistics \
  -H "Authorization: Bearer $TOKEN"
```

---

## Health & System APIs

### 1. Health Check
```bash
curl http://localhost:8080/health
```

**Response:**
```json
{
  "status": "Healthy"
}
```

### 2. API Documentation
```
Open in browser: http://localhost:8080/scalar
```

### 3. Hangfire Dashboard
```
Open in browser: http://localhost:8080/hangfire
```

---

## Postman Collection Template

```json
{
  "info": {
    "name": "AI CFO API",
    "description": "Complete API testing collection"
  },
  "auth": {
    "type": "bearer",
    "bearer": [
      {
        "key": "token",
        "value": "{{accessToken}}",
        "type": "string"
      }
    ]
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "http://localhost:8080"
    },
    {
      "key": "accessToken",
      "value": ""
    }
  ]
}
```

---

## Common Response Formats

### Success Response
```json
{
  "success": true,
  "data": { ... },
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error Response
```json
{
  "success": false,
  "error": "Detailed error message",
  "code": "ERROR_CODE",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Validation Error
```json
{
  "success": false,
  "error": "Validation failed",
  "errors": {
    "email": ["Email is required", "Email format invalid"]
  }
}
```

---

## Testing Workflow

1. **Register & Login** - Get access token
2. **Create Chart of Accounts** - Set up accounting structure
3. **Add Accounts** - Create individual accounts
4. **Record Transactions** - Add journal entries
5. **Get Trial Balance** - Verify double-entry
6. **Sync Bank Data** - Connect bank account
7. **Reconcile** - Match transactions
8. **Analyze** - Run AI analytics
9. **Monitor** - Check health scores
10. **Alert** - Set up thresholds

---

## Tips

- Always include `Authorization: Bearer TOKEN` header
- Use `Content-Type: application/json`
- Test with http://localhost:8080 (local)
- Check CORS by testing from frontend origin
- Use `?limit=10` for pagination
- Review API docs at /scalar for full schema
