using MediatR;
using RapidScada.Application.DTOs;
using RapidScada.Domain.Common;

namespace RapidScada.Application.Queries;

/// <summary>
/// Query to get a device by ID
/// </summary>
public sealed record GetDeviceByIdQuery(int DeviceId) : IRequest<Result<DeviceDetailsDto>>;

/// <summary>
/// Query to get all devices
/// </summary>
public sealed record GetAllDevicesQuery : IRequest<Result<List<DeviceDto>>>;

/// <summary>
/// Query to get devices by communication line
/// </summary>
public sealed record GetDevicesByLineQuery(int LineId) : IRequest<Result<List<DeviceDto>>>;

/// <summary>
/// Query to get devices by status
/// </summary>
public sealed record GetDevicesByStatusQuery(string Status) : IRequest<Result<List<DeviceDto>>>;

/// <summary>
/// Query to get all tags for a device
/// </summary>
public sealed record GetDeviceTagsQuery(int DeviceId) : IRequest<Result<List<TagDto>>>;

/// <summary>
/// Query to get tag by ID
/// </summary>
public sealed record GetTagByIdQuery(int TagId) : IRequest<Result<TagDto>>;

/// <summary>
/// Query to get current tag values (real-time data)
/// </summary>
public sealed record GetCurrentTagValuesQuery : IRequest<Result<List<TagDto>>>;

/// <summary>
/// Query to get tags in alarm state
/// </summary>
public sealed record GetAlarmTagsQuery : IRequest<Result<List<TagDto>>>;

/// <summary>
/// Query to get device driver statistics
/// </summary>
public sealed record GetDeviceStatisticsQuery(int DeviceId) : IRequest<Result<DriverStatisticsDto>>;
