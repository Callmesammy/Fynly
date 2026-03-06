# Docker Build Troubleshooting & Complete Fix Guide

## ✅ Verification: What Was Fixed
- ✅ Runtime base image updated from `aspnet:8.0` to `aspnet:10.0` (matches SDK 10.0)
- ✅ `curl` added to apt-get install for healthcheck support
- ✅ Multi-stage build configuration is correct

## 🔧 Complete Docker Rebuild Process

### Step 1: Stop All Containers
```bash
# Stop running containers
docker-compose down

# Or force stop
docker-compose down -v
```

### Step 2: Clean Docker Build Cache
```bash
# Remove all dangling images and build cache
docker system prune -a --volumes

# Or be more selective
docker builder prune --all
```

### Step 3: Verify Docker Desktop Status
```bash
# Check if Docker is running
docker ps

# If fails, restart Docker Desktop:
# Windows/Mac: Quit Docker Desktop and restart it
# Linux: sudo systemctl restart docker
```

### Step 4: Rebuild Images with No Cache
```bash
# Full rebuild with no cache
docker-compose build --no-cache --progress=plain

# Watch the full build output to identify any errors
```

### Step 5: Run Containers
```bash
# Start services
docker-compose up -d

# Monitor logs in real-time
docker-compose logs -f fynly
```

### Step 6: Verify Everything is Running
```bash
# Check all services
docker-compose ps

# Should show all services as "healthy" or "up"
# Test the API
curl http://localhost:8080/health

# Expected response: HTTP 200 OK
```

## 🐛 Common Build Errors & Solutions

### Error: "No suitable constructor was found"
**Cause**: EF Core mapping issues  
**Status**: ✅ FIXED in codebase (see EF Core fixes from previous sessions)

### Error: "Failed to authenticate with server"
**Cause**: Docker registry authentication  
**Solution**:
```bash
# Clear credentials
docker logout

# Login again
docker login
```

### Error: "resource unavailable" or "cannot find image"
**Cause**: Network issue or base image not available  
**Solution**:
```bash
# Pull base images manually
docker pull mcr.microsoft.com/dotnet/sdk:10.0
docker pull mcr.microsoft.com/dotnet/aspnet:10.0
docker pull postgres:16-alpine
docker pull redis:7-alpine

# Then retry build
docker-compose build --no-cache
```

### Error: "OCI runtime error" or "container failed to start"
**Cause**: Application crash or missing configuration  
**Solution**:
```bash
# Check container logs
docker logs aicfo-api

# Check if PostgreSQL is ready
docker-compose logs postgres

# Verify connection string in docker-compose.yml
```

### Error: "port 8080 already in use"
**Cause**: Another process using port 8080  
**Solution (Windows)**:
```bash
netstat -ano | findstr :8080
taskkill /PID <PID> /F
```

**Solution (macOS/Linux)**:
```bash
lsof -i :8080
kill -9 <PID>
```

Or change port in `docker-compose.yml`:
```yaml
ports:
  - "8081:8080"  # Changed from 8080
```

## 📋 Dockerfile Health Check

Your healthcheck will call: `GET http://localhost:8080/health`

Ensure your API has this endpoint in `Program.cs`:
```csharp
app.MapHealthChecks("/health");
```

If not already added, you need to add it.

## 🔍 Docker Compose Configuration Check

Verify these in `docker-compose.yml`:

1. **PostgreSQL Service** - Should be healthy before API starts:
```yaml
depends_on:
  postgres:
    condition: service_healthy
```

2. **Connection Strings** - Must match service names:
```yaml
# Service name is "postgres", not "localhost"
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=aicfo;Username=postgres;Password=postgres"
```

3. **Environment Variables** - Must be set for .NET 10:
```yaml
ASPNETCORE_ENVIRONMENT: Production
ASPNETCORE_URLS: http://+:8080
```

## 💾 .env File Configuration

Create a `.env` file from `.env.example`:

```bash
cp .env.example .env
```

Update sensitive values:
```
POSTGRES_PASSWORD=your-secure-password
Jwt__Secret=your-min-32-char-secret-key-here
Redis__ConnectionString=redis:6379
```

Then in `docker-compose.yml`, reference it:
```yaml
env_file:
  - .env
```

## 📊 Build Output Interpretation

### Successful Build Output
```
Building fynly
...
 => exporting to image
 => => naming to docker.io/library/aicfo-api:latest
Successfully built <hash>
Successfully tagged aicfo-api:latest
```

### Failed Build Output - Look for
- ❌ `ERROR: failed to build`
- ❌ `dotnet restore failed`
- ❌ `dotnet build failed`
- ❌ `dotnet publish failed`

Run with more verbose output:
```bash
docker-compose build --no-cache --progress=plain 2>&1 | tee build.log
```

Then check `build.log` for specific errors.

## 🔐 Security Best Practices

Current Dockerfile includes:
- ✅ Non-root user (appuser:1000)
- ✅ Minimal runtime image (aspnet:10.0)
- ✅ Health checks
- ✅ Multi-stage build (smaller final image)

### Optional Enhancements
Add read-only filesystem:
```yaml
# In docker-compose.yml
fynly:
  read_only: true
  tmpfs:
    - /tmp
    - /app/Logs
```

## 🚀 Quick Start Commands (Copy & Paste)

```bash
# Complete fresh build
docker-compose down -v
docker system prune -a --volumes
docker-compose build --no-cache
docker-compose up -d

# Check status
docker-compose ps
docker-compose logs fynly

# Test API
curl http://localhost:8080/health
```

## 📞 Still Having Issues?

1. **Check Docker Desktop dashboard**: 
   - Windows/Mac: Open Docker Desktop GUI
   - Click on Container

2. **Collect debug information**:
```bash
# Build logs
docker-compose build --no-cache 2>&1 > build_debug.log

# Runtime logs
docker-compose logs fynly > runtime_debug.log

# Container info
docker inspect aicfo-api > container_debug.json
```

3. **Check system resources**:
```bash
# View Docker resource usage
docker stats

# Check free disk space
df -h

# Ensure at least 5GB free space
```

4. **Restart Docker completely**:
```bash
# Windows
docker-compose down -v
docker system prune -a --volumes

# Then stop Docker Desktop completely and restart it
# (Important: Use Quit, not just minimize)

# Restart services
docker-compose up -d
```

## ✅ Verification Checklist

Before considering Docker setup complete:

- [ ] `docker-compose ps` shows all services as "Up" or "healthy"
- [ ] `curl http://localhost:8080/health` returns HTTP 200
- [ ] `docker-compose logs fynly` shows no ERROR messages
- [ ] PostgreSQL is running and healthy
- [ ] Redis is running and responding to pings
- [ ] Database migrations have run successfully
- [ ] API can access database (check logs for connection errors)

## 📝 Next Steps

1. Run the complete rebuild process above
2. Monitor logs: `docker-compose logs -f`
3. Test API endpoint: `curl http://localhost:8080/health`
4. If still failing, collect logs and share error details

The Dockerfile is now correctly configured for .NET 10. Any remaining issues are likely environmental or configuration-related, not the base image mismatch.
