using MediatR;
using RapidScada.Application.Abstractions;
using RapidScada.Application.DTOs;
using RapidScada.Domain.Common;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Application.Queries;

/// <summary>
/// Handler for GetDeviceTagsQuery
/// </summary>
public sealed class GetDeviceTagsQueryHandler : IRequestHandler<GetDeviceTagsQuery, Result<List<TagDto>>>
{
    private readonly ITagRepository _tagRepository;

    public GetDeviceTagsQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<List<TagDto>>> Handle(GetDeviceTagsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tags = await _tagRepository.GetByDeviceAsync(DeviceId.Create(request.DeviceId), cancellationToken);
            
            var tagDtos = tags.Select(t => new TagDto(
                Id: t.Id.Value,
                Number: t.Number,
                Name: t.Name,
                DeviceId: t.DeviceId.Value,
                DeviceName: "Device", // TODO: Join with Device
                TagType: t.TagType.ToString(),
                Units: t.Units,
                CurrentValue: t.CurrentValue?.Value,
                LastUpdateAt: t.LastUpdateAt,
                Status: t.Status.ToString(),
                Quality: t.CurrentValue?.Quality,
                LowLimit: t.LowLimit,
                HighLimit: t.HighLimit
            )).ToList();

            return Result<List<TagDto>>.Success(tagDtos);
        }
        catch (Exception ex)
        {
            return Result<List<TagDto>>.Failure<List<TagDto>>(Error.Failure("GetTagsFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for GetTagByIdQuery
/// </summary>
public sealed class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, Result<TagDto>>
{
    private readonly ITagRepository _tagRepository;

    public GetTagByIdQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<TagDto>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tag = await _tagRepository.GetByIdAsync(TagId.Create(request.TagId), cancellationToken);
            
            if (tag == null)
                return Result<TagDto>.Failure<TagDto>(Error.NotFound("TagNotFound", $"Tag with ID {request.TagId} not found"));

            var tagDto = new TagDto(
                Id: tag.Id.Value,
                Number: tag.Number,
                Name: tag.Name,
                DeviceId: tag.DeviceId.Value,
                DeviceName: "Device", // TODO: Join with Device
                TagType: tag.TagType.ToString(),
                Units: tag.Units,
                CurrentValue: tag.CurrentValue?.Value,
                LastUpdateAt: tag.LastUpdateAt,
                Status: tag.Status.ToString(),
                Quality: tag.CurrentValue?.Quality,
                LowLimit: tag.LowLimit,
                HighLimit: tag.HighLimit
            );

            return Result<TagDto>.Success<TagDto>(tagDto);
        }
        catch (Exception ex)
        {
            return Result<TagDto>.Failure<TagDto>(Error.Failure("GetTagFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for GetCurrentTagValuesQuery
/// </summary>
public sealed class GetCurrentTagValuesQueryHandler : IRequestHandler<GetCurrentTagValuesQuery, Result<List<TagDto>>>
{
    private readonly ITagRepository _tagRepository;

    public GetCurrentTagValuesQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<List<TagDto>>> Handle(GetCurrentTagValuesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tags = await _tagRepository.GetAllAsync(cancellationToken);
            
            var tagDtos = tags.Select(t => new TagDto(
                Id: t.Id.Value,
                Number: t.Number,
                Name: t.Name,
                DeviceId: t.DeviceId.Value,
                DeviceName: "Device", // TODO: Join with Device
                TagType: t.TagType.ToString(),
                Units: t.Units,
                CurrentValue: t.CurrentValue?.Value,
                LastUpdateAt: t.LastUpdateAt,
                Status: t.Status.ToString(),
                Quality: t.CurrentValue?.Quality,
                LowLimit: t.LowLimit,
                HighLimit: t.HighLimit
            )).ToList();

            return Result<List<TagDto>>.Success(tagDtos);
        }
        catch (Exception ex)
        {
            return Result<List<TagDto>>.Failure<List<TagDto>>(Error.Failure("GetTagValuesFailed", ex.Message));
        }
    }
}
