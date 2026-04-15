# RapidScada Modern - Developer Quick Reference

## 🚀 Quick Start Commands

```bash
# Clone/Extract
cd /path/to/scada-master

# Build everything
dotnet build RapidScada.sln

# Start PostgreSQL
docker run -d --name rapidscada-db \
  -e POSTGRES_DB=rapidscada \
  -e POSTGRES_USER=scada \
  -e POSTGRES_PASSWORD=scada123 \
  -p 5432:5432 \
  timescale/timescaledb:latest-pg15

# Run all services (7 terminals)
cd src/Services/RapidScada.Identity && dotnet run        # Terminal 1 - Port 5003
cd src/Presentation/RapidScada.WebApi && dotnet run      # Terminal 2 - Port 5001
cd src/Services/RapidScada.Realtime && dotnet run        # Terminal 3 - Port 5005
cd src/Services/RapidScada.Communicator && dotnet run    # Terminal 4 - Background
cd src/Services/RapidScada.Archiver && dotnet run        # Terminal 5 - Background
cd src/Services/RapidScada.Notifications && dotnet run   # Terminal 6 - Background
cd src/Services/RapidScada.Alarms && dotnet run          # Terminal 7 - Background
```

---

## 📁 Project Structure

```
scada-master/
├── src/
│   ├── Core/
│   │   ├── RapidScada.Domain/          # Entities, Value Objects, Events
│   │   └── RapidScada.Application/     # CQRS Commands/Queries
│   │
│   ├── Infrastructure/
│   │   └── RapidScada.Persistence/     # EF Core, Repositories
│   │
│   ├── Presentation/
│   │   └── RapidScada.WebApi/          # REST API (Port 5001)
│   │
│   ├── Services/
│   │   ├── RapidScada.Identity/        # Auth (Port 5003)
│   │   ├── RapidScada.Realtime/        # SignalR (Port 5005)
│   │   ├── RapidScada.Communicator/    # Device polling
│   │   ├── RapidScada.Archiver/        # TimescaleDB
│   │   ├── RapidScada.Notifications/   # Email/SMS
│   │   └── RapidScada.Alarms/          # Alarm engine
│   │
│   └── Drivers/
│       ├── RapidScada.Drivers.Modbus/  # Modbus RTU/TCP
│       └── RapidScada.Drivers.Mqtt/    # MQTT IoT
│
├── tests/
│   ├── RapidScada.Domain.Tests/
│   └── RapidScada.Integration.Tests/
│
└── docs/                                # 10 documentation files
```

---

## 🔑 Key URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **WebAPI** | https://localhost:5001 | REST API |
| **Swagger** | https://localhost:5001/swagger | API Documentation |
| **Identity** | https://localhost:5003 | Authentication |
| **SignalR** | wss://localhost:5005/scadahub | Real-time Updates |

---

## 📝 Common Tasks

### Create a New Device

```bash
curl -X POST https://localhost:5001/api/devices \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sensor-001",
    "deviceTypeId": 1,
    "address": 1,
    "communicationLineId": 1
  }'
```

### Create a New Tag

```bash
curl -X POST https://localhost:5001/api/tags \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Temperature",
    "tagNumber": 1,
    "deviceId": 1,
    "dataType": "Double",
    "isWritable": false
  }'
```

### Create Alarm Rule

```sql
INSERT INTO alarm_rules (id, name, tag_id, enabled, severity, priority, condition_data)
VALUES (
  'temp-high',
  'High Temperature',
  1,
  true,
  3,
  10,
  '{"Type": "GreaterThan", "Threshold": 80.0}'::jsonb
);
```

### Query Historical Data

```sql
-- Last 24 hours
SELECT time, value, quality
FROM tag_history
WHERE tag_id = 1
  AND time > NOW() - INTERVAL '24 hours'
ORDER BY time DESC;

-- Hourly aggregates
SELECT time_bucket, avg_value, min_value, max_value
FROM tag_history_1hour
WHERE tag_id = 1
  AND time_bucket > NOW() - INTERVAL '7 days'
ORDER BY time_bucket DESC;
```

---

## 🔐 Authentication Flow

```bash
# 1. Register
curl -X POST https://localhost:5003/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin",
    "email": "admin@example.com",
    "password": "Admin123!"
  }'

# 2. Login (get JWT)
curl -X POST https://localhost:5003/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin",
    "password": "Admin123!"
  }'

# Response:
{
  "accessToken": "eyJhbGci...",
  "refreshToken": "xyz...",
  "userId": 1,
  "userName": "admin",
  "roles": ["User"]
}

# 3. Use token in requests
curl -X GET https://localhost:5001/api/devices \
  -H "Authorization: Bearer eyJhbGci..."
```

---

## 🌐 SignalR Client Example

### JavaScript/TypeScript

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:5005/scadahub", {
        accessTokenFactory: () => "YOUR_JWT_TOKEN"
    })
    .withAutomaticReconnect()
    .build();

// Subscribe to tags
await connection.start();
await connection.invoke("SubscribeToTags", [1, 2, 3]);

// Listen for updates
connection.on("TagValuesUpdated", (updates) => {
    console.log("Tag updates:", updates);
    // updates: [{tagId: 1, value: 25.5, timestamp: "..."}]
});
```

### C# Client

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://localhost:5005/scadahub", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(jwtToken);
    })
    .WithAutomaticReconnect()
    .Build();

await connection.StartAsync();
await connection.InvokeAsync("SubscribeToTags", new[] { 1, 2, 3 });

connection.On<List<TagValueUpdate>>("TagValuesUpdated", updates =>
{
    foreach (var update in updates)
    {
        Console.WriteLine($"Tag {update.TagId}: {update.Value}");
    }
});
```

---

## 🔧 Database Commands

```bash
# Connect to database
psql -h localhost -U scada -d rapidscada

# List tables
\dt

# Describe table
\d devices

# Run TimescaleDB setup
\i src/Services/RapidScada.Archiver/TimescaleDB_Setup.sql

# Check hypertables
SELECT * FROM timescaledb_information.hypertables;

# View compression status
SELECT * FROM timescaledb_information.chunks;
```

---

## 🧪 Testing Commands

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/RapidScada.Domain.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run with verbosity
dotnet test -v detailed
```

---

## 📊 Monitoring Queries

### Active Alarms

```sql
SELECT 
    a.id,
    r.name as rule_name,
    a.message,
    a.severity,
    a.state,
    a.triggered_at,
    a.acknowledged_at
FROM alarms a
JOIN alarm_rules r ON a.rule_id = r.id
WHERE a.state IN ('Active', 'Acknowledged')
ORDER BY a.severity DESC, a.triggered_at DESC;
```

### Tag Update Frequency

```sql
SELECT 
    tag_id,
    COUNT(*) as updates,
    MAX(time) as last_update,
    AVG(value) as avg_value
FROM tag_history
WHERE time > NOW() - INTERVAL '1 hour'
GROUP BY tag_id
ORDER BY updates DESC;
```

### Device Communication Status

```sql
SELECT 
    d.id,
    d.name,
    COUNT(th.tag_id) as tag_update_count,
    MAX(th.time) as last_communication
FROM devices d
LEFT JOIN tags t ON t.device_id = d.id
LEFT JOIN tag_history th ON th.tag_id = t.id
WHERE th.time > NOW() - INTERVAL '1 hour'
GROUP BY d.id, d.name
ORDER BY last_communication DESC;
```

---

## 🐛 Debug Tips

### Enable Verbose Logging

**appsettings.Development.json:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information"
      }
    }
  }
}
```

### Check Service Health

```bash
# Identity
curl https://localhost:5003/health

# WebAPI
curl https://localhost:5001/health

# Realtime
curl https://localhost:5005/health
```

### View Logs

```bash
# Real-time log tailing
tail -f src/Services/RapidScada.Identity/logs/identity-*.log
tail -f src/Presentation/RapidScada.WebApi/logs/scada-*.log
tail -f src/Services/RapidScada.Realtime/logs/realtime-*.log
```

---

## 🔐 Security Configuration

### Generate JWT Secret

```bash
openssl rand -base64 32
```

### Update in appsettings.json

**Both Identity and Realtime:**
```json
{
  "Jwt": {
    "SecretKey": "YOUR_GENERATED_SECRET_32_CHARS_MINIMUM",
    "Issuer": "RapidScada",
    "Audience": "RapidScadaClients",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

---

## 📚 Documentation Index

1. **README.md** - Overview
2. **ARCHITECTURE.md** - System architecture
3. **BUILD_VERIFICATION_GUIDE.md** - Build & verify
4. **CRITICAL_COMPONENTS_SETUP.md** - Archiver/Identity/Realtime
5. **ALARMS_SETUP_GUIDE.md** - Alarm configuration
6. **COMPLETE_COMPONENT_SUMMARY.md** - All components
7. **FINAL_SYSTEM_SUMMARY.md** - Complete overview
8. **COMPARISON.md** - Legacy vs Modern
9. **MIGRATION.md** - Migration guide
10. **MODERNIZATION_ROADMAP.md** - Future plans

---

## 🎯 Performance Tips

### Optimize Database

```sql
-- Create indexes
CREATE INDEX CONCURRENTLY idx_tags_device ON tags(device_id);
CREATE INDEX CONCURRENTLY idx_tag_history_time ON tag_history(time DESC);

-- Analyze tables
ANALYZE devices;
ANALYZE tags;
ANALYZE tag_history;

-- Vacuum
VACUUM ANALYZE;
```

### Enable Redis Caching

**appsettings.json:**
```json
{
  "Realtime": {
    "UseRedisBackplane": true,
    "RedisConnectionString": "localhost:6379"
  }
}
```

### Tune TimescaleDB

```sql
-- Adjust chunk interval
SELECT set_chunk_time_interval('tag_history', INTERVAL '1 day');

-- Enable compression
ALTER TABLE tag_history SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'tag_id'
);
```

---

## 🔄 Common Workflows

### Add New Driver

1. Create project in `src/Drivers/`
2. Inherit from `DeviceDriverBase`
3. Implement abstract methods
4. Register in `DeviceDriverFactory`
5. Add configuration model
6. Test with real device

### Add New Endpoint

1. Create command/query in Application layer
2. Create handler with MediatR
3. Add endpoint in WebApi using Carter
4. Update Swagger documentation
5. Add integration test
6. Test with Postman/curl

### Add New Notification Channel

1. Create interface in `INotificationService`
2. Implement service (e.g., `PushNotificationService`)
3. Add configuration in `appsettings.json`
4. Register in DI container
5. Create Hangfire job
6. Test delivery

---

**Quick Reference Version: 1.0**  
**Last Updated: 2024-04-15**  
**All Systems Operational ✅**
