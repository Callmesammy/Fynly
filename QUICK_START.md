# 🚀 Quick Start - Docker Deployment

## 30-Second Setup

### 1. Clone & Navigate
```bash
git clone https://github.com/Callmesammy/Fynly.git
cd Fynly
```

### 2. Start Services
```bash
# Copy example env
cp .env.example .env

# Start all containers
docker-compose up -d

# Wait 10 seconds for database to initialize...
sleep 10

# Check status
docker-compose ps
```

### 3. Verify Everything Works
```bash
# Check API health
curl http://localhost:8080/health

# View logs
docker-compose logs -f api
```

---

## 🎯 Access Points

| Service | URL | Purpose |
|---------|-----|---------|
| **API** | http://localhost:8080 | Main API endpoint |
| **API Docs** | http://localhost:8080/scalar | Interactive API documentation |
| **Hangfire Dashboard** | http://localhost:8080/hangfire | Background job monitoring |
| **Health Check** | http://localhost:8080/health | System health |
| **Database** | localhost:5432 | PostgreSQL (postgres/postgres) |
| **Redis** | localhost:6379 | Cache layer |

---

## 🔐 CORS for Frontend

Your frontend is already configured to connect from:
- `http://localhost:3000` (React default)
- `http://localhost:3001` (Alternative port)

### Frontend Integration
```javascript
// React/Vue/Angular
const API_URL = 'http://localhost:8080/api';

// Login
const response = await fetch(`${API_URL}/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include', // Important!
  body: JSON.stringify({ email: 'test@example.com', password: 'password' }),
});
```

---

## 📝 First Test: Create User & Login

### 1. Register User
```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@1234",
    "name": "Test User",
    "tenantId": "tenant-1"
  }'
```

### 2. Login
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@1234",
    "tenantId": "tenant-1"
  }'
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "xyz...",
    "user": { "id": "...", "email": "test@example.com" }
  }
}
```

### 3. Use Token in Requests
```bash
export TOKEN="your-access-token-here"

curl http://localhost:8080/api/ai/dashboard \
  -H "Authorization: Bearer $TOKEN"
```

---

## 🔄 Docker Commands Reference

```bash
# View containers
docker-compose ps

# View logs
docker-compose logs -f api         # API logs
docker-compose logs -f postgres    # Database logs
docker-compose logs -f redis       # Redis logs

# Stop services
docker-compose down

# Stop and remove data
docker-compose down -v

# Rebuild images
docker-compose up --build

# Execute command in container
docker-compose exec api dotnet ef migrations list
docker-compose exec postgres psql -U postgres -d aicfo

# View resource usage
docker stats

# Clean up
docker system prune -a
```

---

## 🆘 Common Issues

### API won't start
```bash
# Check logs
docker-compose logs api

# Likely causes:
# 1. Database not ready: wait 5 seconds and try again
# 2. Port 8080 in use: docker-compose down && docker-compose up
# 3. Missing environment variables: check .env file
```

### Database won't connect
```bash
# Check PostgreSQL
docker-compose logs postgres

# Reset database
docker-compose down -v
docker-compose up -d
```

### Can't access Hangfire Dashboard
```bash
# Check if API is running
curl http://localhost:8080/health

# Dashboard should be at:
# http://localhost:8080/hangfire
```

---

## 📊 System Requirements

| Resource | Minimum | Recommended |
|----------|---------|------------|
| CPU | 2 cores | 4 cores |
| RAM | 4 GB | 8 GB |
| Disk | 10 GB | 20 GB |
| Docker | 20.10+ | Latest |

---

## 🎁 What's Deployed

✅ **API Backend** (.NET 10 / ASP.NET Core 8)
- 50+ REST endpoints
- JWT authentication
- Multi-tenancy support
- CORS configured

✅ **Database** (PostgreSQL 16)
- Automatic schema initialization
- Backup volume
- Health checks

✅ **Cache** (Redis 7)
- Session caching
- Job queue
- Rate limiting (Phase 5)

✅ **Background Jobs** (Hangfire)
- Recurring anomaly detection
- Health report scheduling
- Threshold evaluation
- Dashboard monitoring

✅ **AI/Analytics Features**
- Anomaly detection
- Financial predictions
- Health scoring
- Recommendations
- Alert management

---

## 🔄 Next Steps

### Option 1: Connect Frontend
See `CORS_AND_FRONTEND_GUIDE.md` for React/Vue/Angular setup

### Option 2: Deploy to Cloud
See `DEPLOYMENT_GUIDE.md` for AWS/Azure/Docker Hub

### Option 3: Phase 5 Features
- WebSocket real-time notifications (SignalR)
- PDF/Excel report export
- Redis caching optimization
- API rate limiting
- OpenAPI/Swagger docs

---

## 📖 Documentation

- **Setup**: This file (QUICK_START.md)
- **Deployment**: `DEPLOYMENT_GUIDE.md`
- **Frontend Integration**: `CORS_AND_FRONTEND_GUIDE.md`
- **Full Project**: `PROGRESS.md`
- **API Examples**: `API_USAGE_EXAMPLES.md`

---

## 🆘 Support

```bash
# Check logs
docker-compose logs -f

# Check health
curl http://localhost:8080/health

# Verify CORS
curl -H "Origin: http://localhost:3000" http://localhost:8080/api/auth/login

# Database check
docker-compose exec postgres psql -U postgres -c "SELECT 1"
```

---

## ✅ Deployment Checklist

- [ ] Docker Desktop installed and running
- [ ] Repository cloned
- [ ] .env file created and configured
- [ ] `docker-compose up -d` executed
- [ ] All containers healthy (`docker-compose ps`)
- [ ] API responds to health check
- [ ] Can login with test credentials
- [ ] Frontend can reach API (CORS working)
- [ ] Hangfire dashboard accessible
- [ ] Logs show no errors

**Congratulations! Your AI CFO backend is ready for use! 🎉**
