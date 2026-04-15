# Rapid SCADA Modern - .NET 8 Rewrite

A complete modernization of Rapid SCADA using .NET 8, Clean Architecture, and Domain-Driven Design.

## 🏗️ Architecture

### Clean Architecture Layers

```
┌─────────────────────────────────────┐
│         Presentation Layer          │
│    (WebAPI, Minimal APIs, Carter)   │
├─────────────────────────────────────┤
│         Application Layer           │
│    (CQRS, MediatR, Use Cases)      │
├─────────────────────────────────────┤
│           Domain Layer              │
│  (Entities, Value Objects, Events)  │
├─────────────────────────────────────┤
│       Infrastructure Layer          │
│   (EF Core, Repositories, Drivers)  │
└─────────────────────────────────────┘
```

## 📦 Projects

### Core

- **RapidScada.Domain** - Domain entities, value objects, domain events
- **RapidScada.Application** - Use cases, CQRS commands/queries, DTOs

### Infrastructure

- **RapidScada.Infrastructure** - Cross-cutting concerns
- **RapidScada.Persistence** - EF Core, repositories, database

### Drivers

- **RapidScada.Drivers.Abstractions** - Driver interfaces and base classes
- **RapidScada.Drivers.Modbus** - Modbus RTU/TCP driver

### Services

- **RapidScada.Server** - Background worker for device polling
- **RapidScada.Communicator** - Communication service

### Presentation

- **RapidScada.WebApi** - REST API with Swagger/OpenAPI

## 🚀 Key Features

### Modern C# 12 & .NET 8

- **Nullable reference types** - Compile-time null safety
- **Records** - Immutable DTOs and value objects
- **Pattern matching** - Cleaner conditional logic
- **Primary constructors** - Reduced boilerplate
- **Collection expressions** - Modern collection initialization

### Domain-Driven Design

- **Strongly-typed IDs** - DeviceId, TagId, etc.
- **Value Objects** - DeviceName, DeviceAddress with validation
- **Domain Events** - DeviceCreated, TagValueChanged
- **Aggregates** - Device with Tags, CommunicationLine with Devices

### CQRS with MediatR

- **Commands** - CreateDevice, UpdateTagValue
- **Queries** - GetDeviceById, GetCurrentTagValues
- **Handlers** - Decoupled business logic

### Clean Architecture Benefits

- ✅ **Testability** - Mock interfaces, test business logic
- ✅ **Maintainability** - Clear separation of concerns
- ✅ **Flexibility** - Swap infrastructure without touching domain
- ✅ **Scalability** - Independent scaling of services

## 🛠️ Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8.0 |
| Database | PostgreSQL with EF Core 8 |
| ORM | Entity Framework Core |
| API | ASP.NET Core Minimal APIs |
| CQRS | MediatR |
| Routing | Carter |
| Logging | Serilog |
| Testing | xUnit, FluentAssertions |

## 📋 Prerequisites

- .NET 8.0 SDK
- PostgreSQL 15+
- Docker (optional, for containerized deployment)

## 🏃 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourorg/rapidscada-modern.git
cd rapidscada-modern
```

### 2. Setup Database

```bash
# Using Docker
docker run --name rapidscada-postgres \
  -e POSTGRES_DB=rapidscada \
  -e POSTGRES_USER=scada \
  -e POSTGRES_PASSWORD=scada123 \
  -p 5432:5432 \
  -d postgres:15

# Migrations will run automatically on first start
```

### 3. Run the API

```bash
cd src/Presentation/RapidScada.WebApi
dotnet run
```

API will be available at `https://localhost:5001`
Swagger UI at `https://localhost:5001/swagger`

### 4. Run the Server Worker

```bash
cd src/Services/RapidScada.Server
dotnet run
```

## 🔌 Modbus Driver

### Supported Features

- ✅ Modbus RTU over Serial
- ✅ Modbus TCP over Ethernet
- ✅ Function codes: 01, 02, 03, 04, 05, 06, 0F, 10
- ✅ Data types: Bool, Int16, UInt16, Int32, UInt32, Float, Double, String
- ✅ Automatic CRC calculation (RTU)
- ✅ Transaction ID management (TCP)
- ✅ Configurable timeouts and retries
- ✅ Performance statistics

### Example: Create Modbus TCP Device

```csharp
var device = new CreateDeviceDto(
    Name: "Temperature Sensor",
    DeviceTypeId: 1, // Modbus TCP
    Address: 1, // Slave address
    CommunicationLineId: 1,
    CallSign: null,
    Description: "Warehouse temperature sensor"
);

POST /api/devices
```

## 📊 API Examples

### Get All Devices

```http
GET /api/devices
```

### Get Device with Tags

```http
GET /api/devices/1
```

### Get Current Tag Values

```http
GET /api/tags/current
```

### Get Alarm Tags

```http
GET /api/tags/alarms
```

### Update Tag Value

```http
PUT /api/tags/42/value
{
  "value": 23.5,
  "quality": 1.0
}
```

## 🏭 Production Deployment

### Docker Compose

```yaml
version: '3.8'
services:
  api:
    image: rapidscada-api:8.0
    ports:
      - "5000:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;...
    depends_on:
      - db
  
  server:
    image: rapidscada-server:8.0
    environment:
      - ConnectionStrings__DefaultConnection=Host=db;...
    depends_on:
      - db
  
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: rapidscada
      POSTGRES_USER: scada
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data

volumes:
  pgdata:
```

## 📈 Performance Optimizations

- **Bulk tag updates** - Single transaction for multiple values
- **Connection pooling** - Efficient database connections
- **Async/await** - Non-blocking I/O throughout
- **EF Core compiled queries** - Faster repeated queries
- **PostgreSQL JSONB** - Efficient polymorphic storage
- **Read-through caching** - Planned for future

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📝 Migrating from Legacy

### Key Differences

| Legacy | Modern |
|--------|--------|
| .NET Framework 4.0 | .NET 8.0 |
| DataTable/DataSet | EF Core Entities |
| BinaryFormatter | System.Text.Json |
| No async | Full async/await |
| Procedural | DDD/CQRS |
| Windows Forms | REST API |
| DAT files | PostgreSQL |
| No DI | Built-in DI |

### Migration Path

1. Export configuration from legacy DAT files
2. Import to PostgreSQL using migration tools (TBD)
3. Map device types to new driver system
4. Configure communication lines
5. Validate tag mappings
6. Run parallel for validation period

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Follow coding standards (see CONTRIBUTING.md)
4. Write tests
5. Submit pull request

## 📄 License

Apache License 2.0 - See LICENSE file

## 🔗 Resources

- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [EF Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Modbus Protocol](https://modbus.org/specs.php)

---

**Built with ❤️ using modern .NET technologies**
