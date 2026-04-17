using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RapidScada.Application.DTOs;

namespace RapidScada.WebApi.Endpoints;

/// <summary>
/// Alarm management endpoints
/// </summary>
public sealed class AlarmEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alarms")
            .WithTags("Alarms")
            .WithOpenApi();

        // GET /api/alarms/active
        group.MapGet("/active", GetActiveAlarms)
            .WithName("GetActiveAlarms")
            .WithSummary("Get all active alarms")
            .Produces<List<AlarmDto>>();

        // GET /api/alarms/history
        group.MapGet("/history", GetAlarmHistory)
            .WithName("GetAlarmHistory")
            .WithSummary("Get alarm history")
            .Produces<List<AlarmDto>>();

        // POST /api/alarms/{id}/acknowledge
        group.MapPost("/{id}/acknowledge", AcknowledgeAlarm)
            .WithName("AcknowledgeAlarm")
            .WithSummary("Acknowledge an alarm")
            .Produces(204)
            .Produces(404);

        // GET /api/alarms/statistics
        group.MapGet("/statistics", GetAlarmStatistics)
            .WithName("GetAlarmStatistics")
            .WithSummary("Get alarm statistics")
            .Produces<AlarmStatisticsDto>();
    }

    private static async Task<IResult> GetActiveAlarms(
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Return mock data for now
        var alarms = new List<AlarmDto>
        {
            new AlarmDto
            {
                Id = "alarm_001",
                Message = "High temperature detected",
                Severity = "Critical",
                TagId = 1,
                TagName = "Temperature_01",
                Timestamp = DateTime.UtcNow.AddMinutes(-15),
                IsActive = true,
                IsAcknowledged = false
            },
            new AlarmDto
            {
                Id = "alarm_002",
                Message = "Low pressure warning",
                Severity = "Warning",
                TagId = 5,
                TagName = "Pressure_01",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                IsActive = true,
                IsAcknowledged = false
            }
        };

        return Results.Ok(alarms);
    }

    private static async Task<IResult> GetAlarmHistory(
        ISender sender,
        CancellationToken cancellationToken,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? severity = null)
    {
        // Return mock data
        var alarms = new List<AlarmDto>
        {
            new AlarmDto
            {
                Id = "alarm_003",
                Message = "Connection lost to Device_01",
                Severity = "Critical",
                TagId = 3,
                TagName = "Status_01",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                IsActive = false,
                IsAcknowledged = true,
                AcknowledgedAt = DateTime.UtcNow.AddHours(-1)
            }
        };

        return Results.Ok(alarms);
    }

    private static async Task<IResult> AcknowledgeAlarm(
        string id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Mock acknowledgement
        return Results.NoContent();
    }

    private static async Task<IResult> GetAlarmStatistics(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var stats = new AlarmStatisticsDto
        {
            TotalActive = 2,
            Critical = 1,
            Warning = 1,
            Info = 0,
            AcknowledgedToday = 5,
            UnacknowledgedCount = 2
        };

        return Results.Ok(stats);
    }
}

/// <summary>
/// Alarm DTO
/// </summary>
public sealed record AlarmDto
{
    public string Id { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public int TagId { get; init; }
    public string TagName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsActive { get; init; }
    public bool IsAcknowledged { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
}

/// <summary>
/// Alarm statistics DTO
/// </summary>
public sealed record AlarmStatisticsDto
{
    public int TotalActive { get; init; }
    public int Critical { get; init; }
    public int Warning { get; init; }
    public int Info { get; init; }
    public int AcknowledgedToday { get; init; }
    public int UnacknowledgedCount { get; init; }
}
