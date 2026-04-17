using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace RapidScada.WebApi.Endpoints;

/// <summary>
/// System monitoring and management endpoints
/// </summary>
public sealed class SystemEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system")
            .WithTags("System")
            .WithOpenApi();

        // GET /api/system/services
        group.MapGet("/services", GetServicesStatus)
            .WithName("GetServicesStatus")
            .WithSummary("Get status of all backend services")
            .Produces<List<ServiceStatusDto>>();

        // GET /api/system/health
        group.MapGet("/health", GetSystemHealth)
            .WithName("GetSystemHealth")
            .WithSummary("Get overall system health")
            .Produces<SystemHealthDto>();

        // POST /api/system/services/{serviceName}/restart
        group.MapPost("/services/{serviceName}/restart", RestartService)
            .WithName("RestartService")
            .WithSummary("Restart a specific service")
            .Produces(204)
            .Produces(404);

        // GET /api/system/logs
        group.MapGet("/logs", GetSystemLogs)
            .WithName("GetSystemLogs")
            .WithSummary("Get system logs")
            .Produces<List<LogEntryDto>>();
    }

    private static async Task<IResult> GetServicesStatus(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var services = new List<ServiceStatusDto>
        {
            new ServiceStatusDto
            {
                Name = "RapidScada.Identity",
                DisplayName = "Identity Service",
                Description = "Authentication and authorization service",
                Status = "Running",
                Port = 5003,
                Uptime = "2 hours 15 minutes",
                CpuUsage = 2.5,
                MemoryUsage = 125.0,
                LastError = null
            },
            new ServiceStatusDto
            {
                Name = "RapidScada.WebApi",
                DisplayName = "Web API Service",
                Description = "Main REST API backend",
                Status = "Running",
                Port = 5001,
                Uptime = "2 hours 15 minutes",
                CpuUsage = 5.8,
                MemoryUsage = 180.5,
                LastError = null
            },
            new ServiceStatusDto
            {
                Name = "RapidScada.Realtime",
                DisplayName = "Realtime Service",
                Description = "SignalR hub for real-time updates",
                Status = "Running",
                Port = 5005,
                Uptime = "2 hours 10 minutes",
                CpuUsage = 3.2,
                MemoryUsage = 95.0,
                LastError = null
            },
            new ServiceStatusDto
            {
                Name = "RapidScada.Communicator",
                DisplayName = "Communicator Service",
                Description = "Device communication and polling",
                Status = "Stopped",
                Port = 5007,
                Uptime = null,
                CpuUsage = 0,
                MemoryUsage = 0,
                LastError = "Service not started"
            },
            new ServiceStatusDto
            {
                Name = "RapidScada.Archiver",
                DisplayName = "Archiver Service",
                Description = "Historical data storage",
                Status = "Running",
                Port = 5009,
                Uptime = "2 hours 15 minutes",
                CpuUsage = 1.8,
                MemoryUsage = 210.0,
                LastError = null
            }
        };

        return Results.Ok(services);
    }

    private static async Task<IResult> GetSystemHealth(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var health = new SystemHealthDto
        {
            OverallStatus = "Healthy",
            ServicesRunning = 4,
            ServicesStopped = 1,
            TotalServices = 5,
            DatabaseStatus = "Connected",
            CpuUsagePercent = 3.8,
            MemoryUsagePercent = 45.2,
            DiskUsagePercent = 62.5,
            Timestamp = DateTime.UtcNow
        };

        return Results.Ok(health);
    }

    private static async Task<IResult> RestartService(
        string serviceName,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Mock restart
        return Results.NoContent();
    }

    private static async Task<IResult> GetSystemLogs(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] string? serviceName = null,
        [FromQuery] string? level = null,
        [FromQuery] int limit = 100)
    {
        var logs = new List<LogEntryDto>
        {
            new LogEntryDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                Level = "Information",
                ServiceName = "RapidScada.WebApi",
                Message = "HTTP GET /api/devices responded 200 in 45.2 ms"
            },
            new LogEntryDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-10),
                Level = "Warning",
                ServiceName = "RapidScada.Communicator",
                Message = "Device timeout: Device_01"
            },
            new LogEntryDto
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-15),
                Level = "Error",
                ServiceName = "RapidScada.WebApi",
                Message = "Failed to retrieve devices: Connection timeout"
            }
        };

        return Results.Ok(logs);
    }
}

/// <summary>
/// Service status DTO
/// </summary>
public sealed record ServiceStatusDto
{
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Port { get; init; }
    public string? Uptime { get; init; }
    public double CpuUsage { get; init; }
    public double MemoryUsage { get; init; }
    public string? LastError { get; init; }
}

/// <summary>
/// System health DTO
/// </summary>
public sealed record SystemHealthDto
{
    public string OverallStatus { get; init; } = string.Empty;
    public int ServicesRunning { get; init; }
    public int ServicesStopped { get; init; }
    public int TotalServices { get; init; }
    public string DatabaseStatus { get; init; } = string.Empty;
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public double DiskUsagePercent { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Log entry DTO
/// </summary>
public sealed record LogEntryDto
{
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
