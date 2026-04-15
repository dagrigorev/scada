using MediatR;
using RapidScada.Application.DTOs;
using RapidScada.Domain.Common;

namespace RapidScada.Application.Commands;

/// <summary>
/// Command to create a new device
/// </summary>
public sealed record CreateDeviceCommand(CreateDeviceDto Device) : IRequest<Result<DeviceDto>>;

/// <summary>
/// Command to update device configuration
/// </summary>
public sealed record UpdateDeviceCommand(UpdateDeviceDto Device) : IRequest<Result>;

/// <summary>
/// Command to delete a device
/// </summary>
public sealed record DeleteDeviceCommand(int DeviceId) : IRequest<Result>;

/// <summary>
/// Command to add a tag to a device
/// </summary>
public sealed record AddTagCommand(CreateTagDto Tag) : IRequest<Result<TagDto>>;

/// <summary>
/// Command to update tag value
/// </summary>
public sealed record UpdateTagValueCommand(int TagId, object Value, double Quality = 1.0) : IRequest<Result>;

/// <summary>
/// Command to bulk update tag values (for performance)
/// </summary>
public sealed record BulkUpdateTagValuesCommand(
    List<(int TagId, object Value, double Quality)> Updates) : IRequest<Result>;

/// <summary>
/// Command to send a command to a device
/// </summary>
public sealed record SendDeviceCommandCommand(
    int DeviceId,
    string Command,
    object? Parameters = null) : IRequest<Result>;
