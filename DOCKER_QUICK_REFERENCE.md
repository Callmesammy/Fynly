# 🚀 Docker Quick Reference Card

## Copy & Paste These Commands

### 1. Complete Fresh Start (Nuclear Option)
```bash
# Stop everything
docker-compose down -v

# Clean all Docker cache
docker system prune -a --volumes

# Rebuild with no cache
docker-compose build --no-cache

# Start services
docker-compose up -d

# Monitor (open separate terminal)
docker-compose logs -f
```

### 2. Just Rebuild API Service
```bash
docker-compose build fynly --no-cache
docker-compose up -d fynly
docker-compose logs fynly
```

### 3. Just Restart All Services
```bash
docker-compose restart
docker-compose logs -f
```

### 4. Check Health
```bash
# Show all services status
docker-compose ps

# Test API endpoint
curl http://localhost:8080/health

# Test database
docker exec aicfo-postgres psql -U postgres -d aicfo -c "SELECT version();"

# Test Redis
docker exec aicfo-redis redis-cli ping
```

### 5. View Logs
```bash
# Real-time logs (all services)
docker-compose logs -f

# API only
docker-compose logs -f fynly

# Database only
docker-compose logs -f postgres

# Redis only
docker-compose logs -f redis

# Last 100 lines of API logs
docker-compose logs --tail 100 fynly
```

### 6. Access Services
```bash
# PostgreSQL shell
docker exec -it aicfo-postgres psql -U postgres -d aicfo

# Hangfire dashboard
# Open browser: http://localhost:8080/hangfire

# API base URL
# http://localhost:8080

# API health check
# http://localhost:8080/health

# Redis CLI
docker exec -it aicfo-redis redis-cli
```

## Expected Output Examples

### Successful docker-compose ps
```
NAME             STATUS              PORTS
aicfo-postgres   Up (healthy)        0.0.0.0:5432->5432/tcp
aicfo-redis      Up (healthy)        0.0.0.0:6379->6379/tcp
aicfo-api        Up (healthy)        0.0.0.0:8080->8080/tcp
```

### Successful curl health check
```
HTTP/1.1 200 OK
Content-Type: application/json

{"status":"Healthy"}
```

### Successful build output
```
Successfully built <hash>
Successfully tagged aicfo-api:latest
```

## Troubleshooting One-Liners

```bash
# See all running containers
docker ps -a

# Get detailed info on a service
docker-compose stats fynly

# Show network info
docker network inspect aicfo-network

# Rebuild without any cache
docker builder prune --all

# Force remove stuck container
docker rm -f aicfo-api

# Check image sizes
docker images --format "table {{.Repository}}\t{{.Size}}"

# Full system diagnosis
docker system df
docker system events
```

## Port Reference
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432
- **Redis**: localhost:6379
- **Hangfire**: http://localhost:8080/hangfire

## File Locations
- **docker-compose.yml**: Root of project
- **Dockerfile**: Root of project
- **.dockerignore**: Root of project
- **.env**: Create from .env.example (add to git ignore)

## If It's Still Not Working

### Step 1: Get full error output
```bash
docker-compose build --no-cache 2>&1 | tee docker-build.log
```

### Step 2: Share the log
Look for ERROR lines in `docker-build.log`

### Step 3: Try nuclear option
```bash
# Stop everything
docker-compose down -v

# Clean system
docker system prune -a --volumes

# Restart Docker Desktop (completely close and reopen)

# Try again
docker-compose build --no-cache
docker-compose up -d
docker-compose logs -f
```

## Success Checklist ✅

Before declaring Docker working:
- [ ] `docker-compose ps` shows all services as UP
- [ ] PostgreSQL shows (healthy)
- [ ] Redis shows (healthy)  
- [ ] API shows (healthy)
- [ ] `curl http://localhost:8080/health` returns 200
- [ ] `docker-compose logs fynly` shows no ERROR lines
- [ ] All services stayed running for 30 seconds

If all checked: **Docker is working! 🎉**

---

**Need help?** Check `DOCKER_BUILD_TROUBLESHOOTING.md` for detailed debugging
