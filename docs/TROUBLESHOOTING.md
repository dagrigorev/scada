# RapidSCADA Troubleshooting Guide

Common issues and solutions for RapidSCADA deployment and operation.

## 🔴 Service Won't Start

### PostgreSQL Connection Errors

**Problem**: Services fail with "Could not connect to PostgreSQL"

**Solutions**:
```bash
# 1. Check PostgreSQL is running
docker-compose ps postgres

# 2. Verify PostgreSQL health
docker-compose exec postgres pg_isready -U scada

# 3. Check connection string
docker-compose logs webapi | grep "Connection"

# 4. Test manual connection
docker-compose exec postgres psql -U scada -d rapidscada

# 5. Restart PostgreSQL
docker-compose restart postgres
```

### Port Already in Use

**Problem**: "Error: bind: address already in use"

**Solutions**:
```bash
# Find process using the port
lsof -i :5001
netstat -tulpn | grep :5001

# Kill the process
kill -9 <PID>

# Or change port in docker-compose.yml
ports:
  - "5101:8080"
```

### Migration Failures

**Problem**: "Database migration failed"

**Solutions**:
```bash
# 1. Drop and recreate database (CAUTION: DATA LOSS)
docker-compose exec postgres psql -U scada -c "DROP DATABASE rapidscada;"
docker-compose exec postgres psql -U scada -c "CREATE DATABASE rapidscada;"

# 2. Run migrations manually
docker-compose exec webapi dotnet ef database update --verbose

# 3. Check migration status
docker-compose exec webapi dotnet ef migrations list
```

## 🟡 SignalR / WebSocket Issues

### WebSocket Connection Failed

**Problem**: Browser console shows "WebSocket connection to 'ws://localhost:3000/scadahub' failed"

**Root Causes & Fixes**:

**1. Vite Proxy Configuration Issue**
```typescript
// vite.config.ts - Ensure ws: true is set
'/scadahub': {
  target: 'wss://localhost:5005',
  changeOrigin: true,
  secure: false,
  ws: true,  // CRITICAL
},
```

**2. CORS Configuration Missing**
```csharp
// Program.cs in Realtime service
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();  // REQUIRED for SignalR
    });
});
```

**3. SignalR Connection State Error**
```typescript
// signalrService.ts - Add state check before start
async start() {
  if (this.connection.state === signalR.HubConnectionState.Disconnected) {
    try {
      await this.connection.start();
      console.log('SignalR Connected');
    } catch (err) {
      console.error('SignalR Connection Error:', err);
      setTimeout(() => this.start(), 5000);
    }
  }
}
```

**4. Nginx WebSocket Upgrade**
```nginx
location /scadahub {
    proxy_pass http://realtime:8080/scadahub;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 86400;  # 24 hours
}
```

### SignalR "Not in Disconnected state" Error

**Problem**: Console error "The connection is not in the 'Disconnected' state"

**Solution**:
```typescript
// Add state check before starting
if (this.connection.state === signalR.HubConnectionState.Disconnected) {
  await this.connection.start();
}

// Or stop before restarting
await this.connection.stop();
await this.connection.start();
```

## 🟢 API & Endpoint Issues

### 404 Not Found on API Endpoints

**Problem**: GET /api/devices returns 404

**Solutions**:
```bash
# 1. Check service is running
curl http://localhost:5001/health

# 2. Verify route registration
docker-compose logs webapi | grep "endpoint"

# 3. Check Carter module registration
# In Program.cs, ensure:
builder.Services.AddCarter();
app.MapCarter();

# 4. Test discovery endpoint
curl http://localhost:5001/api/discovery/services
```

### 401 Unauthorized Errors

**Problem**: API returns 401 even with valid token

**Solutions**:
```bash
# 1. Check JWT configuration matches across services
# Jwt:SecretKey must be identical
# Jwt:Issuer must match
# Jwt:Audience must match

# 2. Verify token is being sent
# Browser DevTools → Network → Request Headers
# Should see: Authorization: Bearer <token>

# 3. Check token expiration
# Decode JWT at jwt.io and check exp claim

# 4. Test with fresh login
curl -X POST http://localhost:5003/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

### 500 Internal Server Error

**Problem**: API endpoint returns 500

**Solutions**:
```bash
# 1. Check service logs
docker-compose logs webapi | tail -50

# 2. Look for exception details
docker-compose logs webapi | grep "Exception"

# 3. Common causes:
# - Missing handler registration
# - Database connection issue
# - Value Object validation failure
# - Result<T> error not handled

# 4. Enable detailed errors (dev only)
ASPNETCORE_ENVIRONMENT=Development
```

## 🔵 Database Issues

### Tag CurrentValue JSONB Error

**Problem**: "column Quality does not exist"

**Solution**:
```sql
-- This was a previous design issue, now fixed
-- If you still see this error, run:

-- 1. Check current schema
\d tags

-- 2. If separate columns exist, run migration
ALTER TABLE tags DROP COLUMN IF EXISTS quality;
ALTER TABLE tags DROP COLUMN IF EXISTS value;
ALTER TABLE tags DROP COLUMN IF EXISTS timestamp;
ALTER TABLE tags ADD COLUMN IF NOT EXISTS current_value jsonb;

-- 3. Verify structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'tags';
```

### TimescaleDB Extension Missing

**Problem**: "extension timescaledb does not exist"

**Solution**:
```bash
# Use timescale/timescaledb image
# docker-compose.yml:
postgres:
  image: timescale/timescaledb:latest-pg15

# Or manually enable
docker-compose exec postgres psql -U scada rapidscada -c "CREATE EXTENSION IF NOT EXISTS timescaledb;"
```

## 🟣 Docker & Container Issues

### Container Keeps Restarting

**Problem**: Service restarts in loop

**Solutions**:
```bash
# 1. Check why it's failing
docker-compose logs --tail=50 servicename

# 2. Remove restart policy temporarily
# docker-compose.yml:
# restart: "no"

# 3. Run interactively to debug
docker-compose run --rm servicename /bin/sh

# 4. Check health check
docker inspect rapidscada-servicename | grep -A 10 Health
```

### Out of Memory Errors

**Problem**: Container killed by OOM

**Solutions**:
```yaml
# Limit memory in docker-compose.yml
services:
  webapi:
    deploy:
      resources:
        limits:
          memory: 1G
        reservations:
          memory: 512M
```

### Build Fails - NuGet Restore

**Problem**: "Unable to load the service index for source"

**Solutions**:
```bash
# 1. Clear NuGet cache
docker-compose build --no-cache webapi

# 2. Check network connectivity
docker run --rm mcr.microsoft.com/dotnet/sdk:8.0 dotnet nuget list source

# 3. Add NuGet config
# NuGet.Config:
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

## 🟤 Frontend / React Issues

### Service Discovery Page Not Showing

**Problem**: /system/discovery route shows blank page

**Checklist**:
```bash
# 1. Verify route added to App.tsx
# Should have: <Route path="system/discovery" element={<ServiceDiscoveryPage />} />

# 2. Check import exists
# import ServiceDiscoveryPage from './pages/System/ServiceDiscoveryPage';

# 3. Verify translations loaded
# i18n.ts should have discovery section

# 4. Check hooks directory
ls src/hooks/useServiceDiscovery.ts

# 5. Browser console errors
# Open DevTools → Console
```

### Build Errors - React

**Problem**: npm run build fails

**Solutions**:
```bash
# 1. Clear node_modules
cd src/WebUI/rapidscada-web
rm -rf node_modules package-lock.json
npm install

# 2. Check TypeScript errors
npm run type-check

# 3. Update dependencies
npm update

# 4. Check Vite config
cat vite.config.ts
```

## 🧰 Useful Commands

### Complete System Reset

**WARNING: Destroys all data**
```bash
# Stop everything
docker-compose down -v

# Remove images
docker-compose down --rmi all

# Clean build
docker system prune -a

# Fresh start
docker-compose up -d --build
```

### Quick Service Restart

```bash
# Restart single service
docker-compose restart webapi

# Rebuild and restart
docker-compose up -d --no-deps --build webapi

# View real-time logs
docker-compose logs -f webapi
```

### Database Console

```bash
# PostgreSQL shell
docker-compose exec postgres psql -U scada rapidscada

# Common SQL queries
SELECT * FROM devices;
SELECT * FROM tags;
SELECT * FROM __EFMigrationsHistory;
```

## 📊 Diagnostic Scripts

Run these to diagnose issues:

```bash
# 1. Verify all services
./verify-all-services.sh

# 2. Test API endpoints
./test-api-endpoints.sh

# 3. Check service logs
docker-compose logs --tail=100

# 4. Monitor resource usage
docker stats
```

## 🆘 Still Having Issues?

1. **Check logs first**: `docker-compose logs servicename`
2. **Review documentation**: Read DEPLOYMENT_GUIDE.md
3. **Search issues**: GitHub Issues tab
4. **Create new issue**: Include logs and steps to reproduce
5. **Contact support**: support@rapidscada.com

## 📚 Additional Resources

- [Architecture Documentation](./ARCHITECTURE.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [API Reference](./API_REFERENCE.md)
- [Developer Quick Reference](./DEVELOPER_QUICK_REFERENCE.md)
