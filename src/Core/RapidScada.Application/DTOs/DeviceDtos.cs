namespace RapidScada.Application.DTOs;

/// <summary>
/// DTO for creating a new device
/// </summary>
public sealed record CreateDeviceDto(
    string Name,
    int DeviceTypeId,
    int Address,
    int CommunicationLineId,
    string? CallSign = null,
    string? Description = null);

/// <summary>
/// DTO for updating device configuration
/// </summary>
public sealed record UpdateDeviceDto(
    int DeviceId,
    string Name,
    int Address,
    string? CallSign = null,
    string? Description = null);

/// <summary>
/// DTO for device response
/// </summary>
public sealed record DeviceDto(
    int Id,
    string Name,
    int DeviceTypeId,
    string DeviceTypeName,
    int Address,
    string? CallSign,
    int CommunicationLineId,
    string CommunicationLineName,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? LastCommunicationAt,
    int TagCount);

/// <summary>
/// DTO for device with full details
/// </summary>
public sealed record DeviceDetailsDto(
    int Id,
    string Name,
    int DeviceTypeId,
    string DeviceTypeName,
    int Address,
    string? CallSign,
    int CommunicationLineId,
    string CommunicationLineName,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? LastCommunicationAt,
    List<TagDto> Tags,
    DriverStatisticsDto? Statistics);

/// <summary>
/// DTO for tag information
/// </summary>
public sealed record TagDto(
    int Id,
    int Number,
    string Name,
    int DeviceId,
    string DeviceName,
    string TagType,
    string? Units,
    object? CurrentValue,
    DateTime? LastUpdateAt,
    string Status,
    double? Quality,
    double? LowLimit,
    double? HighLimit);

/// <summary>
/// DTO for creating a tag
/// </summary>
public sealed record CreateTagDto(
    int Number,
    string Name,
    int DeviceId,
    string TagType,
    string? Units = null,
    double? LowLimit = null,
    double? HighLimit = null,
    string? Formula = null);

/// <summary>
/// DTO for tag value update
/// </summary>
public sealed record TagValueDto(
    int TagId,
    object Value,
    DateTime Timestamp,
    double Quality);

/// <summary>
/// DTO for driver statistics
/// </summary>
public sealed record DriverStatisticsDto(
    int SuccessfulReads,
    int FailedReads,
    int SuccessfulWrites,
    int FailedWrites,
    double AverageReadTimeMs,
    double AverageWriteTimeMs,
    DateTime? LastCommunication,
    string? LastError);
