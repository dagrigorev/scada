# Rapid SCADA Modernization Roadmap

## ✅ Completed Components (What You Have)

### Core Foundation
- ✅ **RapidScada.Domain** - Domain entities, value objects, events
- ✅ **RapidScada.Application** - CQRS, commands, queries, DTOs
- ✅ **RapidScada.Infrastructure** - Cross-cutting concerns
- ✅ **RapidScada.Persistence** - EF Core 8 + PostgreSQL

### Drivers
- ✅ **RapidScada.Drivers.Abstractions** - Driver framework
- ✅ **RapidScada.Drivers.Modbus** - Modbus RTU/TCP implementation

### Services
- ✅ **RapidScada.Server** - Background worker
- ✅ **RapidScada.Communicator** - Device polling service

### API
- ✅ **RapidScada.WebApi** - REST API with Swagger

---

## 🎯 Priority 1: Core Extensions (Next Steps)

### 1. Additional Protocol Drivers

#### A. **RapidScada.Drivers.Snmp** (SNMP Protocol)
**Location:** `src/Drivers/RapidScada.Drivers.Snmp/`
**Legacy Source:** `ScadaComm/OpenKPs/KpSnmp/`
**Purpose:** Network device monitoring via SNMP
**Key Features:**
- SNMPv2c and SNMPv3 support
- OID browsing and management
- Trap receiving
- Bulk operations

**Implementation Priority:** HIGH
**Estimated Effort:** 2-3 days

#### B. **RapidScada.Drivers.OpcUa** (OPC UA Protocol)
**Location:** `src/Drivers/RapidScada.Drivers.OpcUa/`
**Legacy Source:** Would need OPC UA library
**Purpose:** Industrial automation standard protocol
**Key Features:**
- OPC UA client
- Subscription management
- Data type mapping
- Certificate management

**Implementation Priority:** HIGH
**Estimated Effort:** 5-7 days

#### C. **RapidScada.Drivers.Http** (HTTP/REST Driver)
**Location:** `src/Drivers/RapidScada.Drivers.Http/`
**Legacy Source:** `ScadaComm/OpenKPs/KpHttpNotif/`
**Purpose:** HTTP API polling and webhooks
**Key Features:**
- HTTP GET/POST/PUT/DELETE
- JSON/XML parsing
- OAuth2 authentication
- Webhook receiver

**Implementation Priority:** MEDIUM
**Estimated Effort:** 2-3 days

#### D. **RapidScada.Drivers.Mqtt** (MQTT Protocol)
**Location:** `src/Drivers/RapidScada.Drivers.Mqtt/`
**Legacy Source:** NEW (not in legacy)
**Purpose:** IoT device communication
**Key Features:**
- MQTT 3.1.1 and 5.0
- Topic subscription
- QoS levels
- TLS support

**Implementation Priority:** HIGH (Modern IoT requirement)
**Estimated Effort:** 3-4 days

---

### 2. Notification Services

#### A. **RapidScada.Notifications** (Unified Notifications)
**Location:** `src/Services/RapidScada.Notifications/`
**Legacy Sources:** 
- `ScadaComm/OpenKPs/KpEmail/`
- `ScadaComm/OpenKPs/KpSms/`
**Purpose:** Centralized notification service
**Key Features:**
- Email notifications (SMTP)
- SMS notifications (Twilio/AWS SNS)
- Push notifications
- Telegram/Slack webhooks
- Template engine
- Delivery tracking

**Implementation Priority:** HIGH
**Estimated Effort:** 4-5 days

---

### 3. Historical Data & Archiving

#### A. **RapidScada.Archiver** (Time-Series Data)
**Location:** `src/Services/RapidScada.Archiver/`
**Legacy Source:** `ScadaServer` (archive writing)
**Purpose:** High-performance time-series data storage
**Key Features:**
- TimescaleDB integration
- Data compression
- Retention policies
- Query optimization
- Downsampling

**Implementation Priority:** HIGH
**Estimated Effort:** 5-7 days

**Database:**
```sql
-- Use TimescaleDB extension for PostgreSQL
CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE tag_history (
    time TIMESTAMPTZ NOT NULL,
    tag_id INTEGER NOT NULL,
    value DOUBLE PRECISION,
    quality DOUBLE PRECISION,
    FOREIGN KEY (tag_id) REFERENCES tags(id)
);

SELECT create_hypertable('tag_history', 'time');
```

---

### 4. Real-Time Communication

#### A. **RapidScada.Realtime** (SignalR Hub)
**Location:** `src/Presentation/RapidScada.Realtime/`
**Legacy Source:** NEW (WebSocket replacement for polling)
**Purpose:** Real-time data push to clients
**Key Features:**
- SignalR hubs
- Tag subscription
- Alarm broadcasting
- Device status updates
- Connection management

**Implementation Priority:** HIGH
**Estimated Effort:** 3-4 days

**Usage:**
```csharp
// Client subscribes to tags
await hubConnection.InvokeAsync("SubscribeToTags", new[] { 1, 2, 3 });

// Server pushes updates
await Clients.All.SendAsync("TagValueUpdated", new TagValueUpdate
{
    TagId = 1,
    Value = 25.5,
    Timestamp = DateTime.UtcNow
});
```

---

## 🎯 Priority 2: User Management & Security

### 5. Authentication & Authorization

#### A. **RapidScada.Identity** (Identity Service)
**Location:** `src/Services/RapidScada.Identity/`
**Legacy Source:** `ScadaServices/ScadaUserService/`
**Purpose:** User authentication and authorization
**Key Features:**
- JWT token generation
- Refresh tokens
- Role-based access control (RBAC)
- Claims-based authorization
- Password policies
- Two-factor authentication (2FA)

**Implementation Priority:** HIGH
**Estimated Effort:** 5-6 days

**Domain Entities:**
```csharp
// User entity
public sealed class User : Entity<UserId>
{
    public UserName UserName { get; private set; }
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public IReadOnlyCollection<Role> Roles { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
}

// Role entity
public sealed class Role : Entity<RoleId>
{
    public RoleName Name { get; private set; }
    public IReadOnlyCollection<Permission> Permissions { get; private set; }
}
```

---

## 🎯 Priority 3: Reporting & Analytics

### 6. Reporting System

#### A. **RapidScada.Reporting** (Report Generation)
**Location:** `src/Services/RapidScada.Reporting/`
**Legacy Sources:**
- `Report/RepBuilder/`
- `Report/RepExec/`
**Purpose:** Generate reports from historical data
**Key Features:**
- Report templates (Handlebars/Razor)
- Excel export (EPPlus)
- PDF generation
- Scheduled reports
- Email delivery
- Chart generation

**Implementation Priority:** MEDIUM
**Estimated Effort:** 6-8 days

**Report Types:**
- Trend reports (time-series charts)
- Event logs
- Device uptime/downtime
- Alarm history
- Custom SQL queries

---

## 🎯 Priority 4: Web Frontend

### 7. Modern Web UI

#### A. **RapidScada.WebUI** (React/Blazor Frontend)
**Location:** `src/Presentation/RapidScada.WebUI/`
**Legacy Source:** `ScadaWeb/` (ASP.NET Web Forms)
**Purpose:** Modern responsive web interface
**Key Features:**
- Dashboard widgets
- Real-time charts (Chart.js/Recharts)
- Alarm panel
- Device configuration
- Historical data viewer
- Report viewer

**Technology Choice:**
- **Option 1:** React + TypeScript + Vite
- **Option 2:** Blazor Server/WASM
- **Option 3:** Angular

**Implementation Priority:** MEDIUM-HIGH
**Estimated Effort:** 15-20 days

**Components:**
```
- Dashboard (real-time tiles)
- Tag Browser (tree view with live values)
- Trend Viewer (historical charts)
- Alarm Manager (active/historical alarms)
- Device Manager (CRUD operations)
- Report Viewer (generate and view reports)
- User Settings
```

---

## 🎯 Priority 5: Configuration & Management

### 8. Configuration Management

#### A. **RapidScada.Configuration** (Configuration Service)
**Location:** `src/Services/RapidScada.Configuration/`
**Legacy Source:** `ScadaAdmin/` (Windows Forms app)
**Purpose:** Centralized configuration management
**Key Features:**
- Device templates
- Tag templates
- Communication line presets
- Formula library
- Import/Export configuration
- Backup/Restore

**Implementation Priority:** MEDIUM
**Estimated Effort:** 5-7 days

---

## 🎯 Priority 6: Advanced Features

### 9. Alarm & Event Management

#### A. **RapidScada.Alarms** (Alarm Engine)
**Location:** `src/Services/RapidScada.Alarms/`
**Legacy Source:** `ScadaServer/` (event processing)
**Purpose:** Intelligent alarm detection and management
**Key Features:**
- Alarm rules engine
- Priority levels
- Escalation policies
- Acknowledgement workflow
- Alarm suppression
- Correlation rules

**Implementation Priority:** MEDIUM
**Estimated Effort:** 6-8 days

---

### 10. Analytics & Predictive Maintenance

#### A. **RapidScada.Analytics** (Data Analytics)
**Location:** `src/Services/RapidScada.Analytics/`
**Legacy Source:** NEW
**Purpose:** Advanced analytics and ML
**Key Features:**
- Anomaly detection
- Trend analysis
- Predictive maintenance
- Statistical calculations
- ML.NET integration
- Time-series forecasting

**Implementation Priority:** LOW (Future)
**Estimated Effort:** 10-15 days

---

## 📋 Implementation Order (Recommended)

### Phase 1: Core Services (Weeks 1-4)
1. ✅ Done: Domain, Application, Infrastructure, Persistence
2. ✅ Done: Modbus Driver
3. ✅ Done: Basic API
4. ⏭️ **Next:** RapidScada.Archiver (Historical data)
5. ⏭️ **Next:** RapidScada.Identity (Authentication)
6. ⏭️ **Next:** RapidScada.Realtime (SignalR)

### Phase 2: Additional Drivers (Weeks 5-8)
7. RapidScada.Drivers.Mqtt
8. RapidScada.Drivers.OpcUa
9. RapidScada.Drivers.Snmp
10. RapidScada.Drivers.Http

### Phase 3: Notifications & Alarms (Weeks 9-11)
11. RapidScada.Notifications
12. RapidScada.Alarms

### Phase 4: Web UI (Weeks 12-16)
13. RapidScada.WebUI (React/Blazor)

### Phase 5: Reporting & Configuration (Weeks 17-20)
14. RapidScada.Reporting
15. RapidScada.Configuration

### Phase 6: Advanced Features (Weeks 21+)
16. RapidScada.Analytics
17. Mobile apps (optional)
18. Kubernetes deployment (optional)

---

## 🛠️ Technology Stack for New Components

| Component | Technology |
|-----------|-----------|
| **Archiver** | TimescaleDB, Dapper (for raw SQL performance) |
| **Identity** | ASP.NET Core Identity, JWT, IdentityServer (optional) |
| **Realtime** | SignalR, Redis backplane |
| **Notifications** | Hangfire (background jobs), SMTP, Twilio SDK |
| **MQTT Driver** | MQTTnet library |
| **OPC UA Driver** | OPC Foundation .NET SDK |
| **SNMP Driver** | Lextm.SharpSnmpLib |
| **Reporting** | EPPlus, QuestPDF, Handlebars.Net |
| **WebUI** | React 18, TypeScript, Vite, TanStack Query, Recharts |
| **Alarms** | Stateless (state machine), Hangfire |
| **Analytics** | ML.NET, MathNet.Numerics |

---

## 📊 Current vs Target Architecture

### Current (What You Have):
```
┌─────────────────────────────────────┐
│         REST API (WebApi)           │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│   Application Layer (CQRS)          │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│   Domain Layer (DDD)                │
└─────────────┬───────────────────────┘
              │
┌─────────────▼───────────────────────┐
│   Persistence (EF Core + PostgreSQL)│
└─────────────────────────────────────┘
```

### Target (Full System):
```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Web UI      │  │  Mobile App  │  │  Desktop App │
│  (React)     │  │  (Flutter)   │  │  (Avalonia)  │
└──────┬───────┘  └──────┬───────┘  └──────┬───────┘
       │                 │                 │
       └─────────────────┴─────────────────┘
                         │
       ┌─────────────────▼─────────────────┐
       │   API Gateway (YARP/Ocelot)       │
       └─────────────────┬─────────────────┘
                         │
       ┌─────────────────┴─────────────────┐
       │                                   │
┌──────▼──────┐                    ┌──────▼──────┐
│  REST API   │                    │  SignalR    │
│  (Carter)   │                    │  (Realtime) │
└──────┬──────┘                    └──────┬──────┘
       │                                   │
       └───────────┬───────────────────────┘
                   │
    ┌──────────────▼──────────────┐
    │   Application Services      │
    ├─────────────────────────────┤
    │  - Commands/Queries (CQRS)  │
    │  - Event Handlers           │
    │  - Business Logic           │
    └──────────────┬──────────────┘
                   │
    ┌──────────────▼──────────────┐
    │   Domain Layer (DDD)        │
    ├─────────────────────────────┤
    │  - Entities                 │
    │  - Value Objects            │
    │  - Domain Events            │
    │  - Business Rules           │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────────────────┐
    │                                         │
┌───▼────────┐  ┌──────────┐  ┌─────────┐  ┌▼──────────┐
│PostgreSQL  │  │TimescaleDB│ │  Redis  │  │ RabbitMQ  │
│(Config)    │  │(History)  │  │ (Cache) │  │  (Queue)  │
└────────────┘  └───────────┘  └─────────┘  └───────────┘
```

---

## 🚀 Next Immediate Steps

Based on your current state, here's what to build next:

### 1. **RapidScada.Archiver** (CRITICAL)
You need historical data storage immediately for any SCADA system.

### 2. **RapidScada.Identity** (CRITICAL)
Secure the API with proper authentication.

### 3. **RapidScada.Realtime** (HIGH)
Enable real-time data push to clients.

### 4. **Additional Drivers** (HIGH)
MQTT and OPC UA are essential for modern industrial systems.

---

**Which component would you like me to create next?**
