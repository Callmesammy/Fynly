# 🐳 AI CFO Docker Deployment Guide

## Quick Start (Local Development)

### Prerequisites
- Docker Desktop installed
- Docker Compose installed
- Git

### Step 1: Clone Repository
```bash
git clone https://github.com/Callmesammy/Fynly.git
cd Fynly
```

### Step 2: Setup Environment Variables
```bash
# Copy example to .env
cp .env.example .env

# Edit .env with your values
# Important: Update JWT_SECRET, FLUTTERWAVE credentials, etc.
nano .env  # or use your editor
```

### Step 3: Build & Run Containers
```bash
# Build images and start all services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f api
```

### Step 4: Verify Deployment
```bash
# Check API health
curl http://localhost:8080/health

# Access API Documentation
open http://localhost:8080/scalar

# Access Hangfire Dashboard
open http://localhost:8080/hangfire
```

---

## 🌐 CORS Configuration for Frontend

Your CORS is configured to allow:
- `http://localhost:3000` (React dev server)
- `http://localhost:3001` (Alternative)
- `https://localhost:3000` (HTTPS dev)
- `https://localhost:3001` (HTTPS alternative)

### Frontend Implementation (React Example)

```javascript
// src/api/client.ts
import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:8080';

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Important: allows cookies/auth headers
});

// Add JWT token to requests
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Example: Login
export const login = async (email: string, password: string) => {
  const response = await apiClient.post('/auth/login', { email, password });
  const { accessToken, refreshToken } = response.data.data;
  localStorage.setItem('accessToken', accessToken);
  localStorage.setItem('refreshToken', refreshToken);
  return response.data;
};
```

### .env for React Frontend
```env
REACT_APP_API_URL=http://localhost:8080
REACT_APP_OAUTH_REDIRECT_URI=http://localhost:3000/oauth/callback
```

---

## 📊 Database Initialization

The PostgreSQL container automatically initializes with:
- Database: `aicfo`
- User: `postgres`
- Password: `postgres`

### Manual Database Access
```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U postgres -d aicfo

# List tables
\dt

# Exit
\q
```

---

## 🔧 Production Deployment

### Update Configuration for Production

#### 1. Update CORS for Production Domain
In `Program.cs`, update CORS policy:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "https://your-domain.com",
                "https://app.your-domain.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

#### 2. Create Production `.env` File
```bash
cp .env.example .env.prod

# Update with production values:
# - Strong JWT_SECRET (32+ characters, unique, random)
# - Real Flutterwave credentials
# - Production database credentials
# - Production frontend URL
```

#### 3. Docker Compose for Production
```bash
# Build image
docker build -t aicfo-api:latest .

# Push to registry (e.g., Docker Hub)
docker tag aicfo-api:latest your-registry/aicfo-api:latest
docker push your-registry/aicfo-api:latest

# Deploy with environment variables
docker-compose -f docker-compose.prod.yml --env-file .env.prod up -d
```

---

## 🚀 Deployment Platforms

### Option 1: Azure Container Instances
```bash
# Create resource group
az group create --name aicfo-rg --location eastus

# Deploy container
az container create \
  --resource-group aicfo-rg \
  --name aicfo-api \
  --image aicfo-api:latest \
  --cpu 2 --memory 4 \
  --ports 8080 \
  --environment-variables \
    ConnectionStrings__DefaultConnection="Host=your-db.postgres.database.azure.com;..." \
    ASPNETCORE_ENVIRONMENT=Production
```

### Option 2: AWS ECS
```bash
# Create ECR repository
aws ecr create-repository --repository-name aicfo-api

# Push image to ECR
docker tag aicfo-api:latest your-account.dkr.ecr.region.amazonaws.com/aicfo-api:latest
docker push your-account.dkr.ecr.region.amazonaws.com/aicfo-api:latest

# Create ECS task definition and service
# (Use AWS Console or CloudFormation)
```

### Option 3: Docker Swarm
```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.yml aicfo
```

### Option 4: Kubernetes (Helm)
```bash
# Create namespace
kubectl create namespace aicfo

# Install with Helm (requires helm chart)
helm install aicfo ./helm-chart -n aicfo -f .env.prod
```

---

## 📦 Docker Cleanup

### Remove Containers
```bash
# Stop all containers
docker-compose down

# Remove volumes (caution: deletes data!)
docker-compose down -v

# Remove images
docker-compose down --rmi all
```

### Prune System
```bash
# Remove unused images
docker image prune -a

# Remove unused networks
docker network prune

# Remove unused volumes
docker volume prune

# Full system cleanup
docker system prune -a --volumes
```

---

## 🔐 Security Best Practices

### Production Checklist
- [ ] Change JWT_SECRET to strong random value
- [ ] Use production database credentials
- [ ] Enable HTTPS (configure reverse proxy/load balancer)
- [ ] Update Hangfire Dashboard authentication
- [ ] Restrict API access with rate limiting (Phase 5.4)
- [ ] Use environment variables for all secrets
- [ ] Enable database backups
- [ ] Configure logging and monitoring
- [ ] Update CORS to only allow production domain
- [ ] Use secrets management (AWS Secrets Manager, Azure Key Vault, etc.)

### Hangfire Security
```csharp
// Add Hangfire authorization in production
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Create authorization filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
```

---

## 📊 Monitoring & Logging

### Docker Logs
```bash
# View logs
docker-compose logs -f

# View specific service
docker-compose logs -f api

# Last 100 lines
docker-compose logs --tail=100 api
```

### Health Checks
```bash
# API health
curl http://localhost:8080/health

# Database connection
docker-compose exec api dotnet run --project Domain EF migrations list

# Redis connection
docker-compose exec redis redis-cli ping
```

---

## 🆘 Troubleshooting

### API won't start
```bash
# Check logs
docker-compose logs api

# Common causes:
# - Database connection string incorrect
# - Port 8080 already in use
# - Missing environment variables
```

### Database connection fails
```bash
# Check PostgreSQL is running
docker-compose ps postgres

# Test connection
docker-compose exec postgres psql -U postgres -d aicfo

# Reset database
docker-compose exec postgres psql -U postgres -c "DROP DATABASE aicfo;"
```

### Redis connection fails
```bash
# Check Redis is running
docker-compose ps redis

# Test connection
docker-compose exec redis redis-cli ping

# View keys
docker-compose exec redis redis-cli KEYS "*"
```

---

## 📞 Support

For issues:
1. Check logs: `docker-compose logs -f`
2. Verify environment variables: `docker-compose config`
3. Check Docker resources: `docker stats`
4. Review PROGRESS.md for latest updates

---

## ✅ Deployment Verification Checklist

- [ ] Docker Compose builds successfully
- [ ] All containers start and are healthy
- [ ] API responds to health check
- [ ] Database migrations run successfully
- [ ] Hangfire Dashboard is accessible
- [ ] Scalar API docs are accessible
- [ ] CORS allows frontend requests
- [ ] JWT authentication works
- [ ] Bank OAuth flow is configured
- [ ] Logs show no errors
