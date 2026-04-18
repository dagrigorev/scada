# RapidSCADA Deployment Guide

Complete guide for deploying RapidSCADA in production using Docker Compose.

## 📋 Prerequisites

### Required Software
- Docker Engine 20.10+
- Docker Compose 2.0+
- Git

### System Requirements
- **Minimum**: 4 CPU cores, 8GB RAM, 20GB disk
- **Recommended**: 8 CPU cores, 16GB RAM, 50GB disk
- **Network**: Ports 3000, 5001, 5003, 5005, 5007, 5009, 5432, 6379 available

## 🚀 Quick Start (Development)

### 1. Clone Repository
```bash
git clone https://github.com/yourorg/rapidscada.git
cd rapidscada
```

### 2. Configure Environment
```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your settings
nano .env
```

### 3. Start All Services
```bash
# Build and start all containers
docker-compose up -d

# View logs
docker-compose logs -f

# Check service health
./verify-all-services.sh
```

### 4. Initialize Database
```bash
# Apply database migrations
docker-compose exec webapi dotnet ef database update

# Or run migrations script
./scripts/run-migrations.sh
```

### 5. Access Applications
- **Web UI**: http://localhost:3000
- **WebAPI (Swagger)**: http://localhost:5001/swagger
- **Service Discovery**: http://localhost:3000/system/discovery

## 🔧 Production Deployment

### 1. Environment Configuration

Create `.env` file with production settings:

```env
# Database
POSTGRES_DB=rapidscada
POSTGRES_USER=scada_prod
POSTGRES_PASSWORD=<STRONG_PASSWORD>

# JWT Configuration
JWT_SECRET_KEY=<GENERATE_STRONG_KEY_32_CHARS>
JWT_ISSUER=RapidScada.Production
JWT_AUDIENCE=RapidScada
JWT_EXPIRATION_MINUTES=60

# Redis
REDIS_ENABLED=true
REDIS_CONNECTION=redis:6379

# Service URLs (for production reverse proxy)
WEBAPI_URL=https://api.rapidscada.com
IDENTITY_URL=https://auth.rapidscada.com
REALTIME_URL=https://realtime.rapidscada.com
```

### 2. Generate Strong Secrets

```bash
# Generate JWT Secret Key (32+ characters)
openssl rand -base64 32

# Generate PostgreSQL password
openssl rand -base64 24
```

### 3. SSL/TLS Configuration

For production, use a reverse proxy (Nginx/Traefik) with Let's Encrypt:

```yaml
# docker-compose.override.yml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./nginx/ssl:/etc/nginx/ssl
      - certbot-etc:/etc/letsencrypt
    depends_on:
      - webui

  certbot:
    image: certbot/certbot
    volumes:
      - certbot-etc:/etc/letsencrypt
      - certbot-var:/var/lib/letsencrypt
    command: certonly --webroot --webroot-path=/var/www/html --email admin@rapidscada.com --agree-tos --no-eff-email -d rapidscada.com -d www.rapidscada.com

volumes:
  certbot-etc:
  certbot-var:
```

### 4. Build Production Images

```bash
# Build all images
docker-compose build --no-cache

# Tag for registry
docker tag rapidscada-webapi:latest registry.example.com/rapidscada/webapi:1.0.0
docker tag rapidscada-identity:latest registry.example.com/rapidscada/identity:1.0.0
docker tag rapidscada-realtime:latest registry.example.com/rapidscada/realtime:1.0.0
docker tag rapidscada-communicator:latest registry.example.com/rapidscada/communicator:1.0.0
docker tag rapidscada-archiver:latest registry.example.com/rapidscada/archiver:1.0.0
docker tag rapidscada-webui:latest registry.example.com/rapidscada/webui:1.0.0

# Push to registry
docker-compose push
```

### 5. Deploy to Production

```bash
# Pull latest images
docker-compose pull

# Start services
docker-compose up -d

# Verify deployment
./verify-all-services.sh
./test-api-endpoints.sh
```

## 📊 Monitoring & Maintenance

### Health Checks

All services have built-in health endpoints:
- **Identity**: http://localhost:5003/health
- **WebAPI**: http://localhost:5001/health
- **Realtime**: http://localhost:5005/health
- **Communicator**: http://localhost:5007/health
- **Archiver**: http://localhost:5009/health

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f webapi
docker-compose logs -f realtime

# Last 100 lines
docker-compose logs --tail=100 identity
```

### Database Backup

```bash
# Backup PostgreSQL database
docker-compose exec postgres pg_dump -U scada rapidscada > backup_$(date +%Y%m%d).sql

# Restore from backup
docker-compose exec -T postgres psql -U scada rapidscada < backup_20260418.sql
```

### Scaling Services

```bash
# Scale Realtime service to 3 instances
docker-compose up -d --scale realtime=3

# Scale Communicator to 2 instances
docker-compose up -d --scale communicator=2
```

## 🔐 Security Best Practices

### 1. JWT Configuration
- Use strong, randomly generated secret keys (32+ characters)
- Set appropriate token expiration times
- Rotate secrets periodically

### 2. Database Security
- Use strong passwords
- Limit network access to database
- Enable SSL/TLS for PostgreSQL connections
- Regular backups

### 3. Network Security
- Use firewall rules to restrict access
- Enable rate limiting
- Use HTTPS in production
- Implement CORS properly

### 4. Container Security
- Run containers as non-root users
- Scan images for vulnerabilities
- Keep base images updated
- Use minimal base images (alpine)

## 🐛 Troubleshooting

### Service Won't Start

```bash
# Check logs
docker-compose logs servicename

# Check container status
docker-compose ps

# Restart service
docker-compose restart servicename

# Rebuild service
docker-compose up -d --build servicename
```

### Database Connection Issues

```bash
# Check PostgreSQL is healthy
docker-compose exec postgres pg_isready -U scada

# Test connection string
docker-compose exec webapi dotnet ef database update --connection "Host=postgres;Port=5432;Database=rapidscada;Username=scada;Password=scada123"
```

### SignalR WebSocket Issues

1. Check nginx/proxy WebSocket upgrade headers
2. Verify CORS settings in Realtime service
3. Check browser console for connection errors
4. Verify firewall allows WebSocket connections

### Port Conflicts

```bash
# Check which process is using a port
lsof -i :5001
netstat -tulpn | grep :5001

# Change ports in docker-compose.yml
ports:
  - "5101:8080"  # Change external port
```

## 📈 Performance Tuning

### PostgreSQL Optimization

Edit `postgresql.conf`:
```conf
shared_buffers = 2GB
effective_cache_size = 6GB
maintenance_work_mem = 512MB
checkpoint_completion_target = 0.9
wal_buffers = 16MB
default_statistics_target = 100
random_page_cost = 1.1
effective_io_concurrency = 200
work_mem = 52428kB
min_wal_size = 1GB
max_wal_size = 4GB
```

### Redis Configuration

For SignalR scaling:
```conf
maxmemory 2gb
maxmemory-policy allkeys-lru
```

### Application Settings

Adjust in service environment variables:
```yaml
environment:
  - Communicator__MaxConcurrentDevices=100
  - Archiver__BatchSize=2000
  - Realtime__BroadcastIntervalMs=500
```

## 🔄 Updates & Migrations

### Rolling Update

```bash
# Update one service at a time
docker-compose up -d --no-deps --build webapi
docker-compose up -d --no-deps --build identity
docker-compose up -d --no-deps --build realtime
```

### Database Migrations

```bash
# Create new migration
docker-compose exec webapi dotnet ef migrations add MigrationName

# Apply migrations
docker-compose exec webapi dotnet ef database update

# Rollback migration
docker-compose exec webapi dotnet ef database update PreviousMigrationName
```

## 📞 Support

For issues and support:
- GitHub Issues: https://github.com/yourorg/rapidscada/issues
- Documentation: https://docs.rapidscada.com
- Email: support@rapidscada.com

## 📄 License

Copyright © 2026 RapidSCADA Project
