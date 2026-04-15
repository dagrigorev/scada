# Rapid SCADA Modern - Architecture Documentation

## Table of Contents

1. [System Overview](#system-overview)
2. [Architecture Patterns](#architecture-patterns)
3. [Layer Details](#layer-details)
4. [Data Flow](#data-flow)
5. [Design Decisions](#design-decisions)
6. [Scalability](#scalability)
7. [Security](#security)

## System Overview

Rapid SCADA Modern is a complete rewrite of the legacy SCADA system using modern .NET 8 technologies and clean architecture principles.

### High-Level Architecture

```
┌──────────────────────────────────────────────────────────┐
│                     Client Layer                          │
│  (Web UI, Mobile Apps, Third-party Integrations)         │
└──────────────────┬───────────────────────────────────────┘
                   │ REST API / WebSocket
┌──────────────────▼───────────────────────────────────────┐
│              Presentation Layer                           │
│  ┌────────────────────────────────────────────────┐      │
│  │  RapidScada.WebApi (ASP.NET Core Minimal API)  │      │
│  │  - Carter Endpoints                             │      │
│  │  - Swagger/OpenAPI                             │      │
│  │  - Authentication/Authorization                 │      │
│  └────────────────────────────────────────────────┘      │
└──────────────────┬───────────────────────────────────────┘
                   │ MediatR Commands/Queries
┌──────────────────▼───────────────────────────────────────┐
│              Application Layer                            │
│  ┌────────────────────────────────────────────────┐      │
│  │  RapidScada.Application                        │      │
│  │  - Command Handlers                            │      │
│  │  - Query Handlers                              │      │
│  │  - DTOs                                        │      │
│  │  - Abstractions (IRepository, IDeviceDriver)   │      │
│  └────────────────────────────────────────────────┘      │
└──────────────────┬───────────────────────────────────────┘
                   │ Domain Events / Business Logic
┌──────────────────▼───────────────────────────────────────┐
│                Domain Layer                               │
│  ┌────────────────────────────────────────────────┐      │
│  │  RapidScada.Domain                             │      │
│  │  - Entities (Device, Tag, CommunicationLine)   │      │
│  │  - Value Objects (DeviceName, TagValue)        │      │
│  │  - Domain Events                               │      │
│  │  - Business Rules                              │      │
│  └────────────────────────────────────────────────┘      │
└──────────────────┬───────────────────────────────────────┘
                   │ Repository Interfaces
┌──────────────────▼───────────────────────────────────────┐
│            Infrastructure Layer                           │
│  ┌─────────────────────┐  ┌─────────────────────┐       │
│  │ RapidScada.         │  │ RapidScada.Drivers  │       │
│  │ Persistence         │  │ .Modbus             │       │
│  │ - EF Core           │  │ - Protocol Layer    │       │
│  │ - Repositories      │  │ - Transport Layer   │       │
│  │ - Migrations        │  │ - RTU/TCP Support   │       │
│  └─────────────────────┘  └─────────────────────┘       │
└──────────────────┬───────────────────────────────────────┘
                   │
┌──────────────────▼───────────────────────────────────────┐
│              External Systems                             │
│  - PostgreSQL Database                                    │
│  - Physical Devices (Modbus, OPC, etc.)                  │
│  - Event Bus / Message Queue (Future)                    │
└───────────────────────────────────────────────────────────┘
```

## Architecture Patterns

### 1. Clean Architecture

**Dependencies flow inward:**
- Presentation → Application → Domain
- Infrastructure → Application → Domain
- Domain has NO dependencies

**Benefits:**
- Framework independence
- Testability
- UI independence
- Database independence
- External agency independence

### 2. Domain-Driven Design (DDD)

**Core Concepts:**

```csharp
// Aggregates
Device (root) → Contains → Tags (entities)
CommunicationLine (root) → References → Devices

// Value Objects
DeviceName - Immutable, validated
DeviceAddress - Range validation
TagValue - With timestamp and quality

// Domain Events
DeviceCreatedEvent
TagValueChangedEvent
DeviceStatusChangedEvent
```

**Ubiquitous Language:**
- Device (not KP/КП)
- Tag (not InCnl)
- CommunicationLine (not CommLine)
- Snapshot (not Srez)

### 3. CQRS (Command Query Responsibility Segregation)

**Commands (Write Operations):**
```csharp
CreateDeviceCommand → CreateDeviceCommandHandler
UpdateTagValueCommand → UpdateTagValueCommandHandler
```

**Queries (Read Operations):**
```csharp
GetDeviceByIdQuery → GetDeviceByIdQueryHandler
GetCurrentTagValuesQuery → GetCurrentTagValuesQueryHandler
```

**Benefits:**
- Separation of read/write models
- Optimized queries
- Scalability (future: separate read/write databases)
- Clearer code organization

### 4. Repository Pattern

```csharp
public interface IDeviceRepository : IRepository<Device, DeviceId>
{
    Task<IReadOnlyList<Device>> GetByCommunicationLineAsync(...);
    Task<IReadOnlyList<Device>> GetByStatusAsync(...);
    Task<Device?> GetWithTagsAsync(...);
}
```

**Benefits:**
- Abstraction over data access
- Testable business logic
- Consistent API
- Centralized query logic

### 5. Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Error Error { get; }
}

// Usage
var result = Device.Create(id, name, ...);
if (result.IsSuccess)
{
    var device = result.Value;
}
else
{
    var error = result.Error;
}
```

**Benefits:**
- No exceptions for business failures
- Explicit error handling
- Railway-oriented programming
- Better performance

## Layer Details

### Domain Layer

**Entities:**
```csharp
Device
├── Properties: Id, Name, Address, Status
├── Behaviors: UpdateConfiguration(), AddTag()
└── Events: DeviceCreatedEvent, DeviceStatusChangedEvent

Tag
├── Properties: Number, Name, CurrentValue, Status
├── Behaviors: UpdateValue(), MarkAsInvalid()
└── Events: TagValueChangedEvent

CommunicationLine
├── Properties: Name, ChannelType, IsActive
├── Behaviors: Activate(), Deactivate(), AddDevice()
└── Events: CommunicationLineActivatedEvent
```

**Value Objects:**
```csharp
DeviceName
├── Validation: Not empty, max 100 chars
└── Immutable

DeviceAddress
├── Validation: Range 0-65535
└── Immutable

TagValue
├── Components: Value, Timestamp, Quality
└── Immutable
```

### Application Layer

**Use Cases:**
```
Create Device
├── Validate input
├── Create domain entity
├── Persist to repository
└── Return DTO

Poll Device
├── Get device from repository
├── Initialize driver
├── Read tags
├── Update tag values
└── Save changes
```

**DTOs:**
```csharp
CreateDeviceDto (Input)
DeviceDto (Output)
DeviceDetailsDto (Output with relations)
```

### Infrastructure Layer

**EF Core Configuration:**
```csharp
public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        // Table mapping
        builder.ToTable("devices");
        
        // Value object conversions
        builder.Property(d => d.Name)
            .HasConversion(
                name => name.Value,
                value => DeviceName.Create(value).Value);
        
        // Relationships
        builder.HasMany(d => d.Tags)
            .WithOne()
            .HasForeignKey(t => t.DeviceId);
    }
}
```

**Driver Architecture:**
```
IDeviceDriver (Interface)
    ↑
DeviceDriverBase (Abstract Base)
    ↑
ModbusDriver (Concrete Implementation)
    ├── Protocol Layer (PDU/ADU)
    └── Transport Layer (TCP/RTU)
```

### Presentation Layer

**Minimal APIs with Carter:**
```csharp
public class DeviceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices");
        
        group.MapGet("/", GetAllDevices);
        group.MapPost("/", CreateDevice);
        // ... more endpoints
    }
}
```

## Data Flow

### Read Operation (Query)

```
1. HTTP GET /api/devices/1
   ↓
2. DeviceEndpoints.GetDeviceById()
   ↓
3. MediatR sends GetDeviceByIdQuery
   ↓
4. GetDeviceByIdQueryHandler
   ↓
5. IDeviceRepository.GetByIdAsync()
   ↓
6. EF Core queries PostgreSQL
   ↓
7. Device entity returned
   ↓
8. Map to DeviceDto
   ↓
9. HTTP 200 OK with JSON
```

### Write Operation (Command)

```
1. HTTP POST /api/devices
   ↓
2. DeviceEndpoints.CreateDevice()
   ↓
3. MediatR sends CreateDeviceCommand
   ↓
4. CreateDeviceCommandHandler
   ↓
5. Device.Create() (Domain logic)
   ↓
6. Domain events raised
   ↓
7. IDeviceRepository.AddAsync()
   ↓
8. IUnitOfWork.SaveChangesAsync()
   ↓
9. EF Core INSERT to PostgreSQL
   ↓
10. Domain events dispatched
   ↓
11. HTTP 201 Created
```

### Device Polling Flow

```
Server Worker (Background Service)
   ↓
1. Get active communication lines
   ↓
2. For each line, get devices
   ↓
3. For each device:
   ├─ Create driver instance
   ├─ Initialize with settings
   ├─ Connect to device
   ├─ Read tags
   ├─ Parse response
   ├─ Update tag values (bulk)
   ├─ Update device status
   └─ Disconnect
   ↓
4. Save all changes
   ↓
5. Wait polling interval
   ↓
6. Repeat
```

## Design Decisions

### Why PostgreSQL?

✅ **Chosen:** PostgreSQL
❌ **Not:** SQL Server, MySQL, MongoDB

**Reasons:**
- JSONB for polymorphic data (ConnectionSettings)
- Arrays for collections (DeviceIds)
- Excellent performance
- Open source
- Cross-platform
- Strong ACID guarantees

### Why EF Core?

✅ **Chosen:** EF Core 8
❌ **Not:** Dapper, ADO.NET

**Reasons:**
- Type-safe queries
- Migrations
- Change tracking
- Navigation properties
- Code-first approach
- Built-in DI integration

### Why MediatR?

✅ **Chosen:** MediatR
❌ **Not:** Manual handlers, custom bus

**Reasons:**
- Decoupling
- Single responsibility
- Pipeline behaviors
- Request/response pattern
- Industry standard

### Why Carter?

✅ **Chosen:** Carter
❌ **Not:** Controllers, Plain Minimal APIs

**Reasons:**
- Organized minimal APIs
- Module-based routing
- Less boilerplate
- Modern approach
- Better performance than controllers

### Why Strongly-Typed IDs?

```csharp
// Instead of:
public class Device
{
    public int Id { get; set; }
    public int CommunicationLineId { get; set; }
}

// We use:
public class Device
{
    public DeviceId Id { get; set; }
    public CommunicationLineId CommunicationLineId { get; set; }
}
```

**Reasons:**
- Compile-time safety (can't mix up IDs)
- Self-documenting code
- Type-driven development
- Prevents primitive obsession

## Scalability

### Horizontal Scaling

```yaml
# Multiple API instances
api-1, api-2, api-3 → Load Balancer → Clients

# Multiple Server Workers
server-1: Lines 1-10
server-2: Lines 11-20
server-3: Lines 21-30
```

### Database Scaling

**Current (Single PostgreSQL):**
```
API → PostgreSQL ← Server Worker
```

**Future (Read Replicas):**
```
API (Reads) → PostgreSQL Read Replica
API (Writes) → PostgreSQL Primary ← Server Worker
```

**Future (CQRS with Separate Stores):**
```
Commands → PostgreSQL (Write DB)
            ↓ (Events)
Queries ← TimescaleDB (Read DB, Time-series)
```

### Caching Strategy

**Future Implementation:**
```csharp
public class CachedDeviceRepository : IDeviceRepository
{
    private readonly IDeviceRepository _inner;
    private readonly IDistributedCache _cache;
    
    public async Task<Device?> GetByIdAsync(DeviceId id, ...)
    {
        var cacheKey = $"device:{id}";
        var cached = await _cache.GetAsync(cacheKey);
        
        if (cached != null)
            return Deserialize(cached);
        
        var device = await _inner.GetByIdAsync(id);
        await _cache.SetAsync(cacheKey, Serialize(device));
        
        return device;
    }
}
```

## Security

### Authentication & Authorization

**Future Implementation:**
```csharp
// JWT Bearer tokens
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });

// Role-based authorization
[Authorize(Roles = "Administrator")]
public class DeviceEndpoints : ICarterModule { }
```

### API Security

```csharp
// Rate limiting
builder.Services.AddRateLimiter(options => 
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://scada.company.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Data Protection

```csharp
// Sensitive data encryption
public class EncryptedConnectionSettings : ConnectionSettings
{
    private readonly IDataProtector _protector;
    
    public string EncryptPassword(string password)
    {
        return _protector.Protect(password);
    }
}
```

### Audit Logging

```csharp
public class AuditBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(...)
    {
        // Log request
        _logger.LogInformation(
            "Executing {Command} by {User}",
            typeof(TRequest).Name,
            _currentUser.Email);
        
        var response = await next();
        
        // Log response
        return response;
    }
}
```

---

**This architecture provides a solid foundation for a modern, scalable, maintainable SCADA system.**
