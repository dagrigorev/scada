using Carter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace RapidScada.WebApi.Endpoints;

/// <summary>
/// Service discovery endpoints for microservice architecture
/// </summary>
public sealed class DiscoveryEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/discovery")
            .WithTags("Discovery")
            .WithOpenApi();

        // Get all available services
        group.MapGet("/services", GetAllServices)
            .WithName("GetAllServices")
            .WithSummary("Get all available services with connection info");

        // Get service by name
        group.MapGet("/services/{serviceName}", GetServiceByName)
            .WithName("GetServiceByName")
            .WithSummary("Get specific service connection info");

        // Health check for all services
        group.MapGet("/health", GetServicesHealth)
            .WithName("GetServicesHealth")
            .WithSummary("Get health status of all services");

        // Get service endpoints
        group.MapGet("/endpoints", GetAllEndpoints)
            .WithName("GetAllEndpoints")
            .WithSummary("Get all API endpoints across all services");
    }

    private static Ok<ServiceDiscoveryResponse> GetAllServices(IConfiguration configuration)
    {
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        var baseUrl = isDevelopment ? "http://localhost" : "https://rapidscada.local";

        var services = new List<ServiceInfo>
        {
            new ServiceInfo(
                Name: "Identity",
                Description: "Authentication and user management service",
                Version: "1.0.0",
                BaseUrl: $"{baseUrl}:5003",
                HealthEndpoint: $"{baseUrl}:5003/health",
                Endpoints: new[]
                {
                    "/api/auth/login",
                    "/api/auth/register",
                    "/api/auth/logout",
                    "/api/auth/refresh",
                    "/api/users"
                },
                Capabilities: new[] { "Authentication", "JWT", "User Management" },
                Status: "Running",
                RequiresAuth: false
            ),
            new ServiceInfo(
                Name: "WebAPI",
                Description: "Main BFF service for devices, tags, and alarms",
                Version: "1.0.0",
                BaseUrl: $"{baseUrl}:5001",
                HealthEndpoint: $"{baseUrl}:5001/health",
                Endpoints: new[]
                {
                    "/api/devices",
                    "/api/tags",
                    "/api/alarms",
                    "/api/system"
                },
                Capabilities: new[] { "Device Management", "Tag Management", "Alarm Management" },
                Status: "Running",
                RequiresAuth: true
            ),
            new ServiceInfo(
                Name: "Realtime",
                Description: "SignalR hub for real-time data updates",
                Version: "1.0.0",
                BaseUrl: $"{baseUrl}:5005",
                HealthEndpoint: $"{baseUrl}:5005/health",
                Endpoints: new[]
                {
                    "/scadahub"
                },
                Capabilities: new[] { "Real-time Updates", "SignalR", "WebSockets" },
                Status: "Running",
                RequiresAuth: true
            ),
            new ServiceInfo(
                Name: "Communicator",
                Description: "Device polling and protocol communication",
                Version: "1.0.0",
                BaseUrl: $"{baseUrl}:5007",
                HealthEndpoint: $"{baseUrl}:5007/health",
                Endpoints: new[]
                {
                    "/api/communicator/lines",
                    "/api/communicator/protocols",
                    "/api/communicator/polling"
                },
                Capabilities: new[] { "Modbus TCP", "Modbus RTU", "MQTT", "OPC UA" },
                Status: "Running",
                RequiresAuth: true
            ),
            new ServiceInfo(
                Name: "Archiver",
                Description: "Historical data storage and retrieval",
                Version: "1.0.0",
                BaseUrl: $"{baseUrl}:5009",
                HealthEndpoint: $"{baseUrl}:5009/health",
                Endpoints: new[]
                {
                    "/api/archiver/history",
                    "/api/archiver/trends",
                    "/api/archiver/export"
                },
                Capabilities: new[] { "TimescaleDB", "Historical Data", "Trends", "Export" },
                Status: "Running",
                RequiresAuth: true
            )
        };

        var response = new ServiceDiscoveryResponse(
            Services: services,
            TotalServices: services.Count,
            Environment: isDevelopment ? "Development" : "Production",
            Timestamp: DateTime.UtcNow
        );

        return TypedResults.Ok(response);
    }

    private static Results<Ok<ServiceInfo>, NotFound<string>> GetServiceByName(
        string serviceName,
        IConfiguration configuration)
    {
        var allServices = GetAllServices(configuration).Value;
        var service = allServices?.Services.FirstOrDefault(s => 
            s.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

        if (service == null)
        {
            return TypedResults.NotFound($"Service '{serviceName}' not found");
        }

        return TypedResults.Ok(service);
    }

    private static async Task<Ok<ServiceHealthResponse>> GetServicesHealth(
        [FromServices] IConfiguration configuration,
        [FromServices] IHttpClientFactory httpClientFactory)
    {
        var allServices = GetAllServices(configuration).Value;
        var healthChecks = new List<ServiceHealthInfo>();

        var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);

        foreach (var service in allServices!.Services)
        {
            var healthInfo = await CheckServiceHealth(client, service);
            healthChecks.Add(healthInfo);
        }

        var overallStatus = healthChecks.All(h => h.IsHealthy) ? "Healthy" : 
                           healthChecks.Any(h => h.IsHealthy) ? "Degraded" : "Unhealthy";

        var response = new ServiceHealthResponse(
            OverallStatus: overallStatus,
            Services: healthChecks,
            Timestamp: DateTime.UtcNow
        );

        return TypedResults.Ok(response);
    }

    private static async Task<ServiceHealthInfo> CheckServiceHealth(
        HttpClient client, 
        ServiceInfo service)
    {
        try
        {
            var response = await client.GetAsync(service.HealthEndpoint);
            var responseTime = 0; // Could measure actual response time

            return new ServiceHealthInfo(
                ServiceName: service.Name,
                IsHealthy: response.IsSuccessStatusCode,
                Status: response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                ResponseTimeMs: responseTime,
                LastChecked: DateTime.UtcNow,
                Message: response.IsSuccessStatusCode ? "OK" : $"HTTP {(int)response.StatusCode}"
            );
        }
        catch (Exception ex)
        {
            return new ServiceHealthInfo(
                ServiceName: service.Name,
                IsHealthy: false,
                Status: "Unavailable",
                ResponseTimeMs: 0,
                LastChecked: DateTime.UtcNow,
                Message: ex.Message
            );
        }
    }

    private static Ok<EndpointsResponse> GetAllEndpoints(IConfiguration configuration)
    {
        var allServices = GetAllServices(configuration).Value;
        var endpoints = new List<EndpointInfo>();

        foreach (var service in allServices!.Services)
        {
            foreach (var endpoint in service.Endpoints)
            {
                endpoints.Add(new EndpointInfo(
                    ServiceName: service.Name,
                    Path: endpoint,
                    FullUrl: $"{service.BaseUrl}{endpoint}",
                    Method: "GET", // Simplified - could be extended
                    RequiresAuth: service.RequiresAuth,
                    Description: $"{service.Name} - {endpoint}"
                ));
            }
        }

        var response = new EndpointsResponse(
            Endpoints: endpoints,
            TotalEndpoints: endpoints.Count,
            Timestamp: DateTime.UtcNow
        );

        return TypedResults.Ok(response);
    }
}

/// <summary>
/// Service discovery response
/// </summary>
public sealed record ServiceDiscoveryResponse(
    IReadOnlyList<ServiceInfo> Services,
    int TotalServices,
    string Environment,
    DateTime Timestamp
);

/// <summary>
/// Information about a single service
/// </summary>
public sealed record ServiceInfo(
    string Name,
    string Description,
    string Version,
    string BaseUrl,
    string HealthEndpoint,
    IReadOnlyList<string> Endpoints,
    IReadOnlyList<string> Capabilities,
    string Status,
    bool RequiresAuth
);

/// <summary>
/// Service health response
/// </summary>
public sealed record ServiceHealthResponse(
    string OverallStatus,
    IReadOnlyList<ServiceHealthInfo> Services,
    DateTime Timestamp
);

/// <summary>
/// Health information for a single service
/// </summary>
public sealed record ServiceHealthInfo(
    string ServiceName,
    bool IsHealthy,
    string Status,
    int ResponseTimeMs,
    DateTime LastChecked,
    string Message
);

/// <summary>
/// All endpoints response
/// </summary>
public sealed record EndpointsResponse(
    IReadOnlyList<EndpointInfo> Endpoints,
    int TotalEndpoints,
    DateTime Timestamp
);

/// <summary>
/// Information about a single endpoint
/// </summary>
public sealed record EndpointInfo(
    string ServiceName,
    string Path,
    string FullUrl,
    string Method,
    bool RequiresAuth,
    string Description
);
