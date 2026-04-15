using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RapidScada.Application.Commands;
using RapidScada.Application.DTOs;
using RapidScada.Application.Queries;

namespace RapidScada.WebApi.Endpoints;

/// <summary>
/// Tag data endpoints
/// </summary>
public sealed class TagEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tags")
            .WithTags("Tags")
            .WithOpenApi();

        // GET /api/tags/current
        group.MapGet("/current", GetCurrentTagValues)
            .WithName("GetCurrentTagValues")
            .WithSummary("Get current values for all tags")
            .Produces<List<TagDto>>();

        // GET /api/tags/alarms
        group.MapGet("/alarms", GetAlarmTags)
            .WithName("GetAlarmTags")
            .WithSummary("Get tags in alarm state")
            .Produces<List<TagDto>>();

        // GET /api/tags/{id}
        group.MapGet("/{id:int}", GetTagById)
            .WithName("GetTagById")
            .WithSummary("Get tag by ID")
            .Produces<TagDto>()
            .Produces(404);

        // POST /api/tags
        group.MapPost("/", CreateTag)
            .WithName("CreateTag")
            .WithSummary("Create a new tag")
            .Produces<TagDto>(201)
            .Produces<ProblemDetails>(400);

        // PUT /api/tags/{id}/value
        group.MapPut("/{id:int}/value", UpdateTagValue)
            .WithName("UpdateTagValue")
            .WithSummary("Update tag value")
            .Produces(204)
            .Produces<ProblemDetails>(400);
    }

    private static async Task<IResult> GetCurrentTagValues(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentTagValuesQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> GetAlarmTags(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAlarmTagsQuery(), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(result.Error.Message);
    }

    private static async Task<IResult> GetTagById(
        int id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTagByIdQuery(id), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound();
    }

    private static async Task<IResult> CreateTag(
        CreateTagDto dto,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddTagCommand(dto), cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetTagById", new { id = result.Value.Id }, result.Value)
            : Results.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Failed to create tag",
                Detail = result.Error.Message
            });
    }

    private static async Task<IResult> UpdateTagValue(
        int id,
        [FromBody] TagValueUpdateDto dto,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateTagValueCommand(id, dto.Value, dto.Quality),
            cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Failed to update tag value",
                Detail = result.Error.Message
            });
    }
}

/// <summary>
/// DTO for tag value update
/// </summary>
public sealed record TagValueUpdateDto(object Value, double Quality = 1.0);
