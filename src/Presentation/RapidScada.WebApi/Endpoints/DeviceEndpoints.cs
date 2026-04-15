using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RapidScada.Application.Commands;
using RapidScada.Application.DTOs;
using RapidScada.Application.Queries;

namespace RapidScada.WebApi.Endpoints;

/// <summary>
/// Device management endpoints
/// </summary>
public sealed class DeviceEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices")
            .WithTags("Devices")
            .WithOpenApi();

        // GET /api/devices
        group.MapGet("/", GetAllDevices)
            .WithName("GetAllDevices")
            .WithSummary("Get all devices")
            .Produces<List<DeviceDto>>();

        // GET /api/devices/{id}
        group.MapGet("/{id:int}", GetDeviceById)
            .WithName("GetDeviceById")
            .WithSummary("Get device by ID")
            .Produces<DeviceDetailsDto>()
            .Produces(404);

        // POST /api/devices
        group.MapPost("/", CreateDevice)
            .WithName("CreateDevice")
            .WithSummary("Create a new device")
            .Produces<DeviceDto>(201)
            .Produces<ProblemDetails>(400);

        // PUT /api/devices/{id}
        group.MapPut("/{id:int}", UpdateDevice)
            .WithName("UpdateDevice")
            .WithSummary("Update device configuration")
            .Produces(204)
            .Produces<ProblemDetails>(400)
            .Produces(404);

        // DELETE /api/devices/{id}
        group.MapDelete("/{id:int}", DeleteDevice)
            .WithName("DeleteDevice")
            .WithSummary("Delete a device")
            .Produces(204)
            .Produces(404);

        // GET /api/devices/{id}/tags
        group.MapGet("/{id:int}/tags", GetDeviceTags)
            .WithName("GetDeviceTags")
            .WithSummary("Get all tags for a device")
            .Produces<List<TagDto>>();

        // GET /api/devices/status/{status}
        group.MapGet("/status/{status}", GetDevicesByStatus)
            .WithName("GetDevicesByStatus")
            .WithSummary("Get devices by status")
            .Produces<List<DeviceDto>>();
    }

    private static async Task<IResult> GetAllDevices(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllDevicesQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> GetDeviceById(
        int id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDeviceByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Device not found",
                Detail = result.Error.Message
            });
    }

    private static async Task<IResult> CreateDevice(
        CreateDeviceDto dto,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDeviceCommand(dto), cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetDeviceById", new { id = result.Value.Id }, result.Value)
            : Results.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Failed to create device",
                Detail = result.Error.Message
            });
    }

    private static async Task<IResult> UpdateDevice(
        int id,
        UpdateDeviceDto dto,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (id != dto.DeviceId)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "ID mismatch",
                Detail = "Route ID does not match DTO ID"
            });
        }

        var result = await sender.Send(new UpdateDeviceCommand(dto), cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> DeleteDevice(
        int id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteDeviceCommand(id), cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> GetDeviceTags(
        int id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDeviceTagsQuery(id), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> GetDevicesByStatus(
        string status,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDevicesByStatusQuery(status), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }
}
