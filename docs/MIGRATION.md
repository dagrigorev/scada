# Migration Guide: Legacy Rapid SCADA to Modern .NET 8

## Overview

This guide helps you migrate from the legacy Rapid SCADA (.NET Framework 4.0) to the modern .NET 8 version.

## Key Architecture Changes

### 1. Data Storage

**Legacy:**
- Binary DAT files (cmdtype.dat, kp.dat, incnl.dat, etc.)
- DataTable/DataSet in memory
- Manual file I/O

**Modern:**
- PostgreSQL relational database
- EF Core entities
- LINQ queries, migrations

### 2. Communication Drivers

**Legacy:**
```csharp
public class KpModbusLogic : KPLogic
{
    public override void Session()
    {
        // Synchronous polling
        base.Session();
        // Manual buffer management
    }
}
```

**Modern:**
```csharp
public sealed class ModbusDriver : DeviceDriverBase
{
    protected override async Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        CancellationToken cancellationToken)
    {
        // Async/await pattern
        // Automatic resource management
        // Result<T> for errors
    }
}
```

### 3. Configuration

**Legacy:**
```xml
<!-- XML configuration files -->
<CommSettings>
  <Param name="ServerHost" value="localhost" />
  <Param name="ServerPort" value="10000" />
</CommSettings>
```

**Modern:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;..."
  }
}
```

## Migration Steps

### Phase 1: Export Legacy Data

1. **Export Configuration Database**

```sql
-- Run this query on your legacy ScadaBase
SELECT * FROM InCnl;
SELECT * FROM CtrlCnl;
SELECT * FROM KP;
SELECT * FROM CommLine;
```

2. **Export Current Data**

Save current snapshot and historical archives to CSV:
- Current.dat → current_data.csv
- Minute archives → minute_archive_YYYYMMDD.csv
- Hour archives → hour_archive_YYYYMMDD.csv

### Phase 2: Setup Modern Infrastructure

1. **Install Prerequisites**

```bash
# Install .NET 8 SDK
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Install PostgreSQL
docker run -d \
  --name rapidscada-db \
  -e POSTGRES_PASSWORD=scada123 \
  -p 5432:5432 \
  postgres:15
```

2. **Run Database Migrations**

```bash
cd src/Infrastructure/RapidScada.Persistence
dotnet ef database update
```

### Phase 3: Migrate Configuration

1. **Communication Lines**

Legacy CommLine.dat:
```
Number,Name,Active
1,Serial Line 1,1
2,TCP Line 1,1
```

Modern API call:
```http
POST /api/communication-lines
{
  "name": "Serial Line 1",
  "channelType": "SerialPort",
  "connectionSettings": {
    "type": "SerialPort",
    "portName": "COM1",
    "baudRate": 9600,
    "dataBits": 8,
    "parity": "None",
    "stopBits": "One",
    "timeoutMs": 1000
  }
}
```

2. **Devices (KP)**

Legacy KP.dat:
```
Number,Name,KPTypeID,Address,CallNum,CommLineNum
1,Temp Sensor,2,1,,1
```

Modern API call:
```http
POST /api/devices
{
  "name": "Temp Sensor",
  "deviceTypeId": 1,
  "address": 1,
  "communicationLineId": 1,
  "callSign": null,
  "description": "Temperature sensor"
}
```

3. **Input Channels (Tags)**

Legacy InCnl.dat:
```
CnlNum,Active,Name,CnlTypeID,ObjNum,KPNum,Signal,FormulaUsed,Formula
1,1,Temperature,1,1,1,1,0,
```

Modern API call:
```http
POST /api/tags
{
  "number": 1,
  "name": "Temperature",
  "deviceId": 1,
  "tagType": "Real",
  "units": "°C",
  "lowLimit": 0.0,
  "highLimit": 100.0
}
```

### Phase 4: Migrate Device Drivers

**Legacy KpModbus.dll:**
```csharp
namespace Scada.Comm.Devices
{
    public class KpModbusLogic : KPLogic
    {
        // Old synchronous code
        public override void Session()
        {
            // ...
        }
    }
}
```

**Modern Implementation:**

The new Modbus driver is already included. For custom drivers:

```csharp
namespace YourCompany.Drivers
{
    public sealed class YourDriver : DeviceDriverBase
    {
        protected override async Task<r> OnConnectAsync(
            CancellationToken cancellationToken)
        {
            // Modern async implementation
        }
    }
}
```

### Phase 5: Historical Data Migration

**Script to import historical data:**

```csharp
using var scope = serviceProvider.CreateScope();
var tagRepository = scope.ServiceProvider.GetRequiredService<ITagRepository>();

// Read legacy archive file
var archiveData = ReadLegacyArchive("minute_archive_20240115.csv");

foreach (var record in archiveData)
{
    var tag = await tagRepository.GetByNumberAsync(
        deviceId,
        record.TagNumber);

    if (tag is not null)
    {
        var value = TagValue.Create(
            record.Value,
            record.Timestamp,
            1.0);

        await tag.UpdateValue(value);
    }
}

await unitOfWork.SaveChangesAsync();
```

## Parallel Operation

Run both systems in parallel during migration:

1. **Week 1-2: Setup**
   - Install modern system
   - Import configuration
   - Verify all devices configured

2. **Week 3-4: Testing**
   - Run both systems
   - Compare tag values
   - Verify alarm conditions
   - Test API endpoints

3. **Week 5: Cutover**
   - Stop legacy system
   - Full production on modern system
   - Monitor closely

## Feature Mapping

| Legacy Feature | Modern Equivalent |
|----------------|-------------------|
| ScadaServerCtrl | RapidScada.Server (Worker Service) |
| ScadaCommCtrl | Built into Server Worker |
| ScadaWebShell | RapidScada.WebApi |
| ScadaAdmin | Swagger UI + API |
| Formulas | Tag.Formula property |
| Event log | Domain events → Serilog |
| Archives | Future: TimescaleDB extension |

## Breaking Changes

### Removed Features

- **Windows Forms UI** - Replaced with REST API
- **Binary serialization** - Now JSON
- **DAT files** - Now PostgreSQL
- **Synchronous code** - Now async/await

### Changed Concepts

- **KP (КП)** → Device
- **InCnl** → Tag
- **CtrlCnl** → Command (future)
- **Srez** → Snapshot (future)

## Troubleshooting

### Database Connection Issues

```bash
# Test PostgreSQL connection
psql -h localhost -U scada -d rapidscada
```

### EF Core Migrations

```bash
# List migrations
dotnet ef migrations list

# Rollback migration
dotnet ef database update PreviousMigration

# Create new migration
dotnet ef migrations add YourMigration
```

### Driver Not Found

Check driver factory registration:
```csharp
services.AddSingleton<IDeviceDriverFactory, DeviceDriverFactory>();
```

## Performance Tuning

### Database

```sql
-- Add indexes for common queries
CREATE INDEX idx_tags_device_id_number ON tags(device_id, number);
CREATE INDEX idx_devices_status ON devices(status);
```

### Bulk Operations

```csharp
// Use bulk updates for performance
await tagRepository.BulkUpdateValuesAsync(tagUpdates);
```

## Support

- GitHub Issues: https://github.com/yourorg/rapidscada-modern/issues
- Documentation: https://docs.rapidscada.modern
- Community: https://forum.rapidscada.modern

---

For questions about specific migration scenarios, open an issue on GitHub.
