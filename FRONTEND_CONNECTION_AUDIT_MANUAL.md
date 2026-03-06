# 🔍 Frontend to API Connection - Manual Audit Procedure

## Environment Variables Setup

Your frontend is configured with:
```env
NEXT_PUBLIC_API_URL=http://localhost:5115
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

> **Note**: Your backend is running on **port 5115** (not 8080 in the docker-compose config)

---

## ✅ Step 1: Verify Backend is Running

### 1.1 Check if API is accessible
```bash
curl -i http://localhost:5115/health
```

**Expected Response:**
```
HTTP/1.1 200 OK
Content-Type: application/json
{
  "status": "Healthy",
  "checks": {...}
}
```

### 1.2 Check API Documentation
```bash
# Open in browser:
# - Swagger UI: http://localhost:5115/swagger/index.html
# - Scalar Docs: http://localhost:5115/scalar/v1
# - OpenAPI JSON: http://localhost:5115/swagger/v1/swagger.json
```

---

## ✅ Step 2: Verify CORS Configuration

### 2.1 Test CORS Headers (Preflight Request)
```bash
curl -i -X OPTIONS http://localhost:5115/api/auth/login \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type, Authorization"
```

**Expected Response Headers:**
```
HTTP/1.1 200 OK
Access-Control-Allow-Origin: http://localhost:3000
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS, PATCH
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Allow-Credentials: true
```

### 2.2 Test CORS with Actual Request
```bash
curl -i -X POST http://localhost:5115/api/auth/login \
  -H "Origin: http://localhost:3000" \
  -H "Content-Type: application/json" \
  -H "Cookie: " \
  -d '{
    "email": "test@example.com",
    "password": "Test@1234",
    "tenantId": "tenant-1"
  }'
```

**Should include in response headers:**
```
Access-Control-Allow-Origin: http://localhost:3000
Access-Control-Allow-Credentials: true
```

---

## ✅ Step 3: Test Authentication Flow

### 3.1 Register a New User
```bash
curl -X POST http://localhost:5115/api/auth/register \
  -H "Origin: http://localhost:3000" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "audituser@example.com",
    "password": "AuditTest@1234",
    "name": "Audit User",
    "tenantId": "tenant-audit-001"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-value...",
    "user": {
      "id": "user-id-123",
      "email": "audituser@example.com",
      "name": "Audit User",
      "tenantId": "tenant-audit-001"
    }
  },
  "message": "User registered successfully"
}
```

### 3.2 Login with Credentials
```bash
curl -i -X POST http://localhost:5115/api/auth/login \
  -H "Origin: http://localhost:3000" \
  -H "Content-Type: application/json" \
  -H "Cookie: " \
  -d '{
    "email": "audituser@example.com",
    "password": "AuditTest@1234",
    "tenantId": "tenant-audit-001"
  }'
```

**Save the `accessToken` for next steps:**
```bash
export ACCESS_TOKEN="your-token-here"
```

---

## ✅ Step 4: Test Protected Endpoints

### 4.1 Get AI Dashboard (Protected)
```bash
curl -i http://localhost:5115/api/ai/dashboard \
  -H "Origin: http://localhost:3000" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

**Expected:** 200 OK with dashboard data

### 4.2 Get Health Report (Protected)
```bash
curl -i http://localhost:5115/api/health-report \
  -H "Origin: http://localhost:3000" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

### 4.3 Get Alerts (Protected)
```bash
curl -i http://localhost:5115/api/alerts \
  -H "Origin: http://localhost:3000" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"
```

---

## ✅ Step 5: Frontend Configuration Checklist

### 5.1 React `.env.local` Setup
```bash
# .env.local in your React/Next.js project
NEXT_PUBLIC_API_URL=http://localhost:5115
NEXT_PUBLIC_APP_URL=http://localhost:3000
```

### 5.2 API Client Configuration

**Option A: Using Axios**
```javascript
// src/services/api.js
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: `${process.env.NEXT_PUBLIC_API_URL}/api`,
  withCredentials: true,  // ✅ IMPORTANT: Include credentials
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add JWT token to requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
```

**Option B: Using Fetch**
```javascript
// src/services/api.js
const API_URL = process.env.NEXT_PUBLIC_API_URL;

export async function fetchApi(endpoint, options = {}) {
  const token = localStorage.getItem('accessToken');
  const headers = {
    'Content-Type': 'application/json',
    ...(token && { Authorization: `Bearer ${token}` }),
    ...options.headers,
  };

  const response = await fetch(`${API_URL}/api${endpoint}`, {
    ...options,
    headers,
    credentials: 'include', // ✅ IMPORTANT: Include credentials
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.status}`);
  }

  return response.json();
}
```

### 5.3 Login Component Example
```javascript
// src/components/Login.jsx
import { useState } from 'react';
import apiClient from '../services/api';

export function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await apiClient.post('/auth/login', {
        email,
        password,
        tenantId: process.env.NEXT_PUBLIC_TENANT_ID || 'default',
      });

      const { accessToken, refreshToken } = response.data.data;
      
      // Store tokens
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);

      // Redirect to dashboard
      window.location.href = '/dashboard';
    } catch (err) {
      setError(err.response?.data?.message || 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="email"
        placeholder="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        required
      />
      <input
        type="password"
        placeholder="Password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        required
      />
      <button type="submit" disabled={loading}>
        {loading ? 'Logging in...' : 'Login'}
      </button>
      {error && <p style={{ color: 'red' }}>{error}</p>}
    </form>
  );
}
```

---

## ✅ Step 6: Browser DevTools Verification

### 6.1 Check Network Tab
1. Open **Chrome DevTools** (F12)
2. Go to **Network** tab
3. Make a login request from frontend
4. Check response headers for:
   - `Access-Control-Allow-Origin: http://localhost:3000` ✅
   - `Access-Control-Allow-Credentials: true` ✅
   - `Access-Control-Allow-Methods: POST, GET, ...` ✅

### 6.2 Check Console for CORS Errors
```javascript
// If you see this error:
// "Access to XMLHttpRequest at 'http://localhost:5115/api/auth/login' 
//  from origin 'http://localhost:3000' has been blocked by CORS policy"

// ❌ Check:
// 1. Frontend origin matches CORS whitelist in Program.cs
// 2. withCredentials: true is set in API client
// 3. Backend is returning correct CORS headers
```

### 6.3 Check Application Tab
1. Go to **Application** tab → **Cookies**
2. Verify cookies are being set (if applicable)
3. Check **Local Storage** for tokens:
   - `accessToken` should exist after login
   - `refreshToken` should exist after login

---

## ✅ Step 7: Complete Integration Test

### 7.1 Full User Flow Test

```bash
#!/bin/bash
API_URL="http://localhost:5115"
ORIGIN="http://localhost:3000"

echo "🔍 Testing Frontend Integration with AI CFO Backend\n"

# 1. Register
echo "1️⃣  Registering user..."
REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/register" \
  -H "Origin: $ORIGIN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "integration-test@example.com",
    "password": "IntegrationTest@1234",
    "name": "Integration Test User",
    "tenantId": "tenant-integration-001"
  }')

ACCESS_TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.data.accessToken')
echo "✅ Access Token: ${ACCESS_TOKEN:0:20}...\n"

# 2. Verify Protected Endpoint
echo "2️⃣  Testing protected endpoint (AI Dashboard)..."
curl -i -X GET "$API_URL/api/ai/dashboard" \
  -H "Origin: $ORIGIN" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json"

echo "\n✅ Frontend Integration Test Complete!"
```

---

## ❌ Troubleshooting CORS Issues

### Issue: "CORS Policy: No 'Access-Control-Allow-Origin' header"

**Cause:** Backend not returning CORS headers

**Solution:**
```csharp
// In Program.cs, verify:
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")  // ✅ Must match frontend
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();                    // ✅ Required for tokens
    });
});

app.UseCors("AllowFrontend");  // ✅ Must be called
```

### Issue: "Credentials mode is 'include' but Access-Control-Allow-Credentials is missing"

**Cause:** `withCredentials: true` but backend not allowing credentials

**Solution:**
```javascript
// Frontend - Use withCredentials: true
const apiClient = axios.create({
  withCredentials: true,  // ✅ This MUST be set
});

// Backend - MUST have .AllowCredentials()
policy.AllowCredentials();
```

### Issue: "Request header field Authorization is not allowed"

**Cause:** Backend not allowing Authorization header

**Solution:**
```csharp
// In Program.cs
policy.AllowAnyHeader();  // ✅ Or explicitly: .WithHeaders("Authorization", "Content-Type")
```

---

## 📊 Quick Audit Checklist

- [ ] Backend running on `http://localhost:5115`
- [ ] Health endpoint returns 200: `GET /health`
- [ ] CORS preflight returns correct headers
- [ ] User registration works
- [ ] User login returns `accessToken`
- [ ] Protected endpoints return data with valid token
- [ ] Frontend `.env.local` has correct `NEXT_PUBLIC_API_URL`
- [ ] Frontend API client uses `withCredentials: true`
- [ ] Frontend stores tokens in localStorage
- [ ] Frontend includes `Authorization: Bearer $TOKEN` header
- [ ] Browser DevTools shows no CORS errors
- [ ] Tokens are visible in localStorage after login

---

## 🔗 Related Documentation

- [CORS Configuration Details](./CORS_AND_FRONTEND_GUIDE.md)
- [Deployment Guide](./DEPLOYMENT_AND_CORS_SUMMARY.md)
- [API Testing Examples](./API_TESTING_EXAMPLES.md)
- [Backend Program.cs](./Fynly/Program.cs)

---

## 🆘 Support

If you encounter issues:

1. Check backend logs: `docker-compose logs -f api`
2. Verify backend CORS config in `Program.cs`
3. Check frontend network tab in DevTools
4. Verify environment variables in frontend `.env.local`
5. Ensure `withCredentials: true` in API client
