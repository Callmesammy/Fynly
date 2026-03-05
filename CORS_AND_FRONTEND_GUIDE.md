# 🔗 CORS & Frontend Integration Guide

## Current CORS Configuration ✅

Your backend is configured to allow requests from:

| Origin | Purpose |
|--------|---------|
| `http://localhost:3000` | Local React dev (default) |
| `http://localhost:3001` | Alternative local dev |
| `https://localhost:3000` | HTTPS testing |
| `https://localhost:3001` | HTTPS testing |

---

## Frontend Setup (React/Vue/Angular)

### 1. React with Axios

```typescript
// src/services/api.ts
import axios, { AxiosInstance, AxiosError } from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:8080';

export const apiClient: AxiosInstance = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // ✅ Required for CORS with credentials
});

// Interceptor: Add JWT token to all requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Interceptor: Handle 401 responses
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Token expired, try to refresh
      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        try {
          const response = await axios.post(
            `${API_BASE_URL}/api/auth/refresh`,
            { refreshToken },
            { withCredentials: true }
          );
          const { accessToken } = response.data.data;
          localStorage.setItem('accessToken', accessToken);
          // Retry original request
          return apiClient(error.config!);
        } catch {
          localStorage.removeItem('accessToken');
          localStorage.removeItem('refreshToken');
          window.location.href = '/login';
        }
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### 2. React Context for Authentication

```typescript
// src/contexts/AuthContext.tsx
import { createContext, useContext, useState, useCallback } from 'react';
import apiClient from '../services/api';

interface AuthContextType {
  isAuthenticated: boolean;
  user: any;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  register: (email: string, password: string, name: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null);

  const login = useCallback(async (email: string, password: string) => {
    try {
      const response = await apiClient.post('/auth/login', {
        email,
        password,
      });
      const { accessToken, refreshToken, user: userData } = response.data.data;
      
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);
      setUser(userData);
      setIsAuthenticated(true);
    } catch (error) {
      console.error('Login failed:', error);
      throw error;
    }
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    setUser(null);
    setIsAuthenticated(false);
  }, []);

  const register = useCallback(async (email: string, password: string, name: string) => {
    try {
      const response = await apiClient.post('/auth/register', {
        email,
        password,
        name,
      });
      const { accessToken, refreshToken, user: userData } = response.data.data;
      
      localStorage.setItem('accessToken', accessToken);
      localStorage.setItem('refreshToken', refreshToken);
      setUser(userData);
      setIsAuthenticated(true);
    } catch (error) {
      console.error('Registration failed:', error);
      throw error;
    }
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, login, logout, register }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
```

### 3. Example API Call Component

```typescript
// src/components/Dashboard.tsx
import { useEffect, useState } from 'react';
import apiClient from '../services/api';
import { useAuth } from '../contexts/AuthContext';

export const Dashboard = () => {
  const { user } = useAuth();
  const [health, setHealth] = useState<string | null>(null);
  const [dashboard, setDashboard] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        // Get AI Dashboard
        const response = await apiClient.get('/ai/dashboard', {
          params: { topAnomalies: 5, topRecommendations: 5 },
        });
        setDashboard(response.data.data);
      } catch (err: any) {
        setError(err.response?.data?.message || 'Failed to fetch dashboard');
      } finally {
        setLoading(false);
      }
    };

    fetchDashboard();
  }, []);

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h1>Welcome, {user?.email}</h1>
      <div>{JSON.stringify(dashboard, null, 2)}</div>
    </div>
  );
};
```

### 4. React Environment Variables

```env
# .env
REACT_APP_API_URL=http://localhost:8080
REACT_APP_API_TIMEOUT=30000

# .env.production
REACT_APP_API_URL=https://api.your-domain.com
REACT_APP_API_TIMEOUT=30000
```

---

## 2. Vue 3 with Axios

```typescript
// src/api/client.ts
import axios from 'axios';

export const apiClient = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
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

```typescript
// src/stores/authStore.ts
import { defineStore } from 'pinia';
import apiClient from '../api/client';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: null,
    isAuthenticated: false,
    loading: false,
  }),

  actions: {
    async login(email: string, password: string) {
      this.loading = true;
      try {
        const response = await apiClient.post('/auth/login', {
          email,
          password,
        });
        const { accessToken, user } = response.data.data;
        localStorage.setItem('accessToken', accessToken);
        this.user = user;
        this.isAuthenticated = true;
      } finally {
        this.loading = false;
      }
    },

    logout() {
      localStorage.removeItem('accessToken');
      this.user = null;
      this.isAuthenticated = false;
    },
  },
});
```

```env
# .env
VITE_API_URL=http://localhost:8080

# .env.production
VITE_API_URL=https://api.your-domain.com
```

---

## 3. Angular Integration

```typescript
// src/services/api.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ApiService {
  private apiUrl = 'http://localhost:8080/api';

  constructor(private http: HttpClient) {}

  getHeaders(): HttpHeaders {
    const token = localStorage.getItem('accessToken');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    });
  }

  login(email: string, password: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/login`, { email, password });
  }

  getAIDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/ai/dashboard`, {
      headers: this.getHeaders(),
    });
  }
}
```

```typescript
// src/app/interceptors/auth.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    const token = localStorage.getItem('accessToken');
    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          localStorage.removeItem('accessToken');
          // Redirect to login
        }
        return throwError(() => error);
      })
    );
  }
}
```

---

## Environment Configuration

### Local Development (.env)
```env
REACT_APP_API_URL=http://localhost:8080
REACT_APP_ENV=development
```

### Production (.env.production)
```env
REACT_APP_API_URL=https://api.your-domain.com
REACT_APP_ENV=production
```

---

## Testing CORS

### Using Postman
1. **Headers Tab**: Add `Origin: http://localhost:3000`
2. **Send Request**: POST to `http://localhost:8080/api/auth/login`
3. **Check Response Headers**: Look for:
   - `Access-Control-Allow-Origin: http://localhost:3000`
   - `Access-Control-Allow-Credentials: true`

### Using curl
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -H "Origin: http://localhost:3000" \
  -d '{"email":"test@example.com","password":"password"}'
```

### Using JavaScript (Browser Console)
```javascript
fetch('http://localhost:8080/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include', // Important for CORS
  body: JSON.stringify({ email: 'test@example.com', password: 'password' }),
})
  .then((res) => res.json())
  .then((data) => console.log(data));
```

---

## Adding Production Domain to CORS

To add your production domain, update `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? new[]
            {
                "http://localhost:3000",
                "http://localhost:3001",
                "https://localhost:3000",
                "https://localhost:3001",
            };

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

Then in `appsettings.json`:
```json
{
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:3001",
    "https://your-domain.com",
    "https://app.your-domain.com"
  ]
}
```

---

## Common Issues & Solutions

### CORS Error: "Access to XMLHttpRequest blocked"
**Cause**: Frontend origin not in allowed list
**Solution**: Add frontend URL to CORS policy in Program.cs

### "Credential mode not allowed"
**Cause**: `withCredentials: true` without `AllowCredentials()` on backend
**Solution**: Ensure both frontend and backend have credentials enabled

### Preflight Request Failing
**Cause**: OPTIONS request not allowed
**Solution**: Ensure `.AllowAnyMethod()` includes OPTIONS

### Token Not Sent
**Cause**: `withCredentials: false` or missing Authorization header
**Solution**: Use `withCredentials: true` and add token to headers

---

## Production Deployment Checklist

- [ ] Update CORS origins to production domain
- [ ] Use HTTPS in production URLs
- [ ] Secure JWT token storage (consider httpOnly cookies)
- [ ] Implement token refresh logic
- [ ] Add rate limiting to login endpoint
- [ ] Configure CSRF protection if needed
- [ ] Enable HTTPS redirect
- [ ] Set up monitoring/logging
- [ ] Test CORS with production domain
