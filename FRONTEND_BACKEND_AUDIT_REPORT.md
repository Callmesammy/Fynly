# 🔍 Frontend ↔ Backend API Audit Report

**Date:** 2026-03-05  
**Status:** ⚠️ **ISSUES FOUND** — Critical and Medium Priority

---

## Executive Summary

| Category | Status | Issues |
|----------|--------|--------|
| Authentication | ⚠️ CRITICAL | JWT token format incompatible with frontend expectations |
| Headers | ⚠️ MEDIUM | `X-Tenant-Id` header handling inconsistent |
| Response DTO | ✅ OK | Mostly compliant, minor naming differences |
| Endpoints | ⚠️ MEDIUM | Missing endpoints, mismatched response shapes |
| Error Handling | ⚠️ MEDIUM | Error response envelope not standardized |

---

## 🔴 CRITICAL ISSUES

### Issue #1: JWT Token Format is Not Valid JWT

**Severity:** CRITICAL  
**File:** `Infastructure/Services/JwtTokenService.cs`  
**Lines:** 22-30

**Problem:**
```csharp
// Current implementation - NOT a real JWT!
var tokenData = $"{userId}|{tenantId}|{email}|{expiresAt:O}";
return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));
```

The backend is generating **base64-encoded strings, NOT proper JWT tokens**. This breaks:

1. ❌ **Frontend token validation** — Cannot decode or validate JWT signature
2. ❌ **JWT claims** — No claims can be extracted by frontend
3. ❌ **Security** — No cryptographic verification
4. ❌ **Browser compatibility** — Frontend libraries expect `header.payload.signature` format

**Expected Format (JWT):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
```

**Audit Result:** ❌ **FAIL**

---

### Issue #2: TenantContext Cannot Extract Claims from Non-JWT Tokens

**Severity:** CRITICAL  
**File:** `Infastructure/Services/TenantContext.cs`  
**Lines:** 20-32

**Problem:**
```csharp
public Guid TenantId
{
    get
    {
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id");
        if (tenantIdClaim is null)
            throw new InvalidOperationException("Tenant ID claim not found in token");
        // ...
    }
}
```

With base64 tokens, **claims extraction will fail** because:
1. Base64 strings have no internal claim structure
2. ASP.NET Core's JWT middleware cannot parse claims from custom base64 format
3. `httpContext.User.FindFirst()` will return null

**Current Flow (❌ Broken):**
```
Frontend sends base64 token
    ↓
ASP.NET cannot parse claims (not valid JWT)
    ↓
TenantContext.TenantId throws InvalidOperationException
    ↓
All protected endpoints return 500 or 403
```

**Audit Result:** ❌ **FAIL**

---

## 🟡 MEDIUM ISSUES

### Issue #3: Register Endpoint Request/Response Mismatch

**Severity:** MEDIUM  
**File:** `Fynly/Controllers/AuthController.cs` (Line 24-46)

**Frontend Expects:**
```json
{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "password": "string",
  "passwordConfirm": "string"
}
```

**Frontend Expects Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "string",
    "refreshToken": "string",
    "user": {
      "id": "string",
      "email": "string",
      "name": "string",              // ❌ MISSING - backend sends firstName + lastName
      "tenantId": "string"
    }
  }
}
```

**Backend Currently Returns:**
```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "user": {
    "id": "...",
    "firstName": "...",              // ❌ Should be combined into "name"
    "lastName": "...",               // ❌ Should be combined into "name"
    "tenantId": "...",
    "email": "..."
  }
}
```

**Audit Result:** ⚠️ **PARTIAL FAIL** — Frontend will have `firstName`/`lastName` instead of `name`

---

### Issue #4: Response Envelope Inconsistency

**Severity:** MEDIUM  
**File:** `Application/Common/ApiResponse.cs`

**Frontend Expects:**
```json
{
  "success": true | false,
  "data": { ... },
  "error": { "message": "..." }
}
```

**Backend Needs to Return:**
```csharp
public record ApiResponse<T>(
    bool Success,
    T? Data,
    ErrorDetail? Error
);

public record ErrorDetail(
    string Message,
    string? Code = null,
    Dictionary<string, string[]>? ValidationErrors = null
);
```

**Current Implementation:** Need to verify this is consistent across all endpoints.

**Audit Result:** ⚠️ **NEEDS VERIFICATION**

---

### Issue #5: X-Tenant-Id Header vs JWT Claim Conflict

**Severity:** MEDIUM  
**Files:** `TenantMiddleware.cs`, `AuthController.cs`

**Current Behavior:**
- Auth endpoints **require** `X-Tenant-Id` header
- Protected endpoints **extract** `tenant_id` from JWT claim
- This creates two different tenant ID sources

**Frontend Perspective:**
- Sends `X-Tenant-Id` header at registration
- Token payload should contain tenant_id for subsequent requests
- If JWT doesn't have claims, TenantContext fails

**Audit Result:** ⚠️ **FAIL** — Cannot work with base64 tokens

---

## 🟠 Response Shape Mismatches

### Missing Endpoints

| Endpoint | Status | Notes |
|----------|--------|-------|
| `GET /api/overview/summary` | ❌ MISSING | KPI dashboard |
| `GET /api/ai/briefing/today` | ❌ MISSING | AI briefing |
| `GET /api/ai/forecast/cashflow?days=30` | ❌ MISSING | Cash flow forecast |
| `POST /api/ai/forecast/scenario` | ❌ MISSING | Scenario planning |
| `GET /api/alerts` | ⚠️ EXISTS | Need to verify response shape |
| `GET /api/transactions` | ⚠️ EXISTS | Need to verify pagination |
| `GET /api/invoices` | ⚠️ EXISTS | Need to verify response |
| `GET /api/reports/profit-loss` | ⚠️ EXISTS | Need to verify |
| `GET /api/reports/balance-sheet` | ⚠️ EXISTS | Need to verify |
| `GET /api/reports/cash-flow` | ⚠️ EXISTS | Need to verify |
| `GET /api/analytics/profitability` | ⚠️ EXISTS | Need to verify |

---

## 🔧 REQUIRED FIXES

### Fix #1: Implement Proper JWT Token Generation

**Priority:** CRITICAL  
**Effort:** HIGH  
**File:** `Infastructure/Services/JwtTokenService.cs`

```csharp
// BEFORE (base64 hack)
public string GenerateAccessToken(Guid userId, Guid tenantId, string email)
{
    var tokenData = $"{userId}|{tenantId}|{email}|{expiresAt:O}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenData));
}

// AFTER (proper JWT)
public string GenerateAccessToken(Guid userId, Guid tenantId, string email, TimeSpan? expiresIn = null)
{
    var expirationTime = expiresIn ?? TimeSpan.FromMinutes(15);
    var expiresAt = DateTime.UtcNow.Add(expirationTime);

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key");

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim("sub", userId.ToString()),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("email", email),
            new Claim("user_id", userId.ToString()),  // For TenantContext.UserId
        }),
        Expires = expiresAt,
        Issuer = _configuration["Jwt:Issuer"] ?? "ai-cfo",
        Audience = _configuration["Jwt:Audience"] ?? "ai-cfo-api",
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}
```

---

### Fix #2: Standardize Response Envelope

**Priority:** HIGH  
**File:** Create/Update `Application/Common/ApiResponse.cs`

```csharp
public record ApiResponse<T>(
    bool Success,
    T? Data = null,
    ErrorDetail? Error = null)
{
    public static ApiResponse<T> Ok(T data) => 
        new(Success: true, Data: data, Error: null);
    
    public static ApiResponse<T> Failure(string message, string? code = null) => 
        new(Success: false, Data: default, Error: new ErrorDetail(message, code));
}

public record ErrorDetail(
    string Message,
    string? Code = null);
```

**Then ensure ALL endpoints wrap responses:**

```csharp
// BAD - frontend won't recognize this
return Ok(new AuthResponse(...));

// GOOD - matches frontend contract
return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse(...)));
```

---

### Fix #3: Normalize User DTO in Auth Response

**Priority:** MEDIUM  
**File:** `Application/Dtos/AuthDto.cs`

```csharp
// Add this new DTO
public record AuthUserDto(
    string Id,
    string Email,
    string Name,           // ✅ Combined firstName + lastName
    string TenantId);

// Update AuthResponse
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,         // ✅ Change to seconds, not DateTime
    AuthUserDto User);     // ✅ Use new DTO

// In RegisterCommandHandler and LoginCommandHandler:
var response = new AuthResponse(
    AccessToken: accessToken,
    RefreshToken: refreshToken,
    ExpiresIn: 900,        // 15 minutes in seconds
    User: new AuthUserDto(
        Id: user.Id.ToString(),
        Email: user.Email,
        Name: $"{user.FirstName} {user.LastName}",  // ✅ Combined
        TenantId: user.TenantId.ToString()));
```

---

### Fix #4: Remove X-Tenant-Id Requirement from Auth Endpoints

**Priority:** MEDIUM  
**File:** `Fynly/Controllers/AuthController.cs`

```csharp
// BEFORE - requires X-Tenant-Id header
[HttpPost("register")]
public async Task<IActionResult> Register(
    [FromBody] RegisterRequest request,
    [FromHeader(Name = "X-Tenant-Id")] string? tenantIdHeader,  // ❌ Required
    CancellationToken cancellationToken)
{
    if (!Guid.TryParse(tenantIdHeader, out var tenantId))
        return BadRequest(...);
}

// AFTER - tenantId comes from request body
[HttpPost("register")]
public async Task<IActionResult> Register(
    [FromBody] RegisterRequestWithTenant request,
    CancellationToken cancellationToken)
{
    var tenantId = request.TenantId ?? Guid.NewGuid();  // Generate if not provided
    // ...
}

// New DTO
public record RegisterRequestWithTenant(
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string PasswordConfirm,
    Guid? TenantId = null);  // Optional, generated if missing
```

---

### Fix #5: Add Missing Endpoints

**Priority:** HIGH  
**Create new controllers for:**

1. `OverviewController.cs` → `/api/overview`
   - `GET /summary` → Dashboard KPIs

2. `ForecastController.cs` → `/api/ai` (or expand AIAnalyticsController)
   - `GET /forecast/cashflow?days=30` → Cash flow forecast
   - `POST /forecast/scenario` → Scenario planning

3. Update `AIAnalyticsController.cs`
   - `GET /briefing/today` → Daily briefing

---

## ✅ Implementation Checklist

- [ ] **CRITICAL:** Implement proper JWT token generation
- [ ] **CRITICAL:** Update TenantContext to handle JWT claims correctly
- [ ] **HIGH:** Standardize ApiResponse envelope across all endpoints
- [ ] **HIGH:** Create missing endpoints (Overview, Forecast, Briefing)
- [ ] **MEDIUM:** Normalize AuthResponse DTO (combine firstName + lastName into "name")
- [ ] **MEDIUM:** Remove X-Tenant-Id requirement from auth endpoints
- [ ] **MEDIUM:** Add request validation (email format, password strength)
- [ ] **LOW:** Add comprehensive error codes to ErrorDetail
- [ ] **LOW:** Implement proper logging for audit trail

---

## 🧪 Testing Strategy

### Phase 1: JWT Token Validation
```bash
# 1. Register user
curl -X POST http://localhost:5115/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{ "email": "test@example.com", "password": "Test@1234", ... }'

# 2. Decode token on jwt.io - should show:
# - "sub": user ID
# - "tenant_id": tenant ID  
# - "email": user email
# - Proper signature
```

### Phase 2: TenantContext Claims Extraction
```bash
# Token should be parseable and claims extractable
curl -X GET http://localhost:5115/api/overview/summary \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json"
# Should return 200 (not 500/403)
```

### Phase 3: Response Envelope
```bash
# All responses should match:
{
  "success": true,
  "data": { ... },
  "error": null
}
```

---

## 📋 Questions for Backend Team

1. Should tenantId be required or auto-generated on registration?
2. Should JWT tokens contain additional user metadata?
3. Are there refresh token rotation requirements?
4. Should token expiration times be configurable per tenant?
5. Do we need token revocation/blacklisting?

---

## 🎯 Next Steps

1. **Immediate (Today):** Implement proper JWT token generation
2. **Short-term (This sprint):** Fix TenantContext, standardize responses
3. **Medium-term:** Create missing endpoints
4. **Long-term:** Add comprehensive error handling and logging

---

**Report Generated:** 2026-03-05  
**Reviewed By:** Frontend Audit Specification  
**Status:** Awaiting Backend Team Implementation
