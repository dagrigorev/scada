using MediatR;
using RapidScada.Application.Abstractions;
using RapidScada.Application.DTOs;
using RapidScada.Domain.Common;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Application.Queries;

/// <summary>
/// Handler for GetAllDevicesQuery
/// </summary>
public sealed class GetAllDevicesQueryHandler : IRequestHandler<GetAllDevicesQuery, Result<List<DeviceDto>>>
{
    private readonly IDeviceRepository _deviceRepository;

    public GetAllDevicesQueryHandler(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Result<List<DeviceDto>>> Handle(GetAllDevicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _deviceRepository.GetAllAsync(cancellationToken);
            
            var deviceDtos = devices.Select(d => new DeviceDto(
                Id: d.Id.Value,
                Name: d.Name.Value,
                DeviceTypeId: d.DeviceTypeId.Value,
                DeviceTypeName: "Unknown", // TODO: Join with DeviceType
                Address: d.Address.Value,
                CallSign: d.CallSign?.Value,
                CommunicationLineId: d.CommunicationLineId.Value,
                CommunicationLineName: "Unknown", // TODO: Join with CommunicationLine
                Description: d.Description,
                Status: d.Status.ToString(),
                CreatedAt: d.CreatedAt,
                LastCommunicationAt: d.LastCommunicationAt,
                TagCount: d.Tags.Count
            )).ToList();

            return Result<List<DeviceDto>>.Success(deviceDtos);
        }
        catch (Exception ex)
        {
            return Result<List<DeviceDto>>.Failure<List<DeviceDto>>(new Error("GetDevicesFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for GetDeviceByIdQuery
/// </summary>
public sealed class GetDeviceByIdQueryHandler : IRequestHandler<GetDeviceByIdQuery, Result<DeviceDetailsDto>>
{
    private readonly IDeviceRepository _deviceRepository;

    public GetDeviceByIdQueryHandler(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Result<DeviceDetailsDto>> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var device = await _deviceRepository.GetByIdAsync(DeviceId.Create(request.DeviceId), cancellationToken);
            
            if (device == null)
                return Result<DeviceDetailsDto>.Failure<DeviceDetailsDto>(Error.NotFound("DeviceNotFound", $"Device with ID {request.DeviceId} not found"));

            var deviceDto = new DeviceDetailsDto(
                Id: device.Id.Value,
                Name: device.Name.Value,
                DeviceTypeId: device.DeviceTypeId.Value,
                DeviceTypeName: "Unknown", // TODO: Join with DeviceType
                Address: device.Address.Value,
                CallSign: device.CallSign?.Value,
                CommunicationLineId: device.CommunicationLineId.Value,
                CommunicationLineName: "Unknown", // TODO: Join with CommunicationLine
                Description: device.Description,
                Status: device.Status.ToString(),
                CreatedAt: device.CreatedAt,
                LastCommunicationAt: device.LastCommunicationAt,
                Tags: device.Tags.Select(t => new TagDto(
                    Id: t.Id.Value,
                    Number: t.Number,
                    Name: t.Name,
                    DeviceId: t.DeviceId.Value,
                    DeviceName: device.Name.Value,
                    TagType: t.TagType.ToString(),
                    Units: t.Units,
                    CurrentValue: t.CurrentValue,
                    LastUpdateAt: t.LastUpdateAt,
                    Status: "Active", // TODO: Determine status
                    Quality: t.Quality,
                    LowLimit: t.LowLimit,
                    HighLimit: t.HighLimit
                )).ToList(),
                Statistics: null // TODO: Add statistics
            );

            return Result<DeviceDetailsDto>.Success(deviceDto);
        }
        catch (Exception ex)
        {
            return Result<DeviceDetailsDto>.Failure<DeviceDetailsDto>(Error.Failure("GetDeviceFailed", ex.Message));
        }
    }
}

/// <summary>
/// Handler for GetDevicesByLineQuery
/// </summary>
public sealed class GetDevicesByLineQueryHandler : IRequestHandler<GetDevicesByLineQuery, Result<List<DeviceDto>>>
{
    private readonly IDeviceRepository _deviceRepository;

    public GetDevicesByLineQueryHandler(IDeviceRepository deviceRepository)
    {
        _deviceRepository = deviceRepository;
    }

    public async Task<Result<List<DeviceDto>>> Handle(GetDevicesByLineQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var devices = await _deviceRepository.GetByCommunicationLineAsync(
                CommunicationLineId.Create(request.LineId), 
                cancellationToken);
            
            var deviceDtos = devices.Select(d => new DeviceDto(
                Id: d.Id.Value,
                Name: d.Name.Value,
                DeviceTypeId: d.DeviceTypeId.Value,
                DeviceTypeName: "Unknown",
                Address: d.Address.Value,
                CallSign: d.CallSign?.Value,
                CommunicationLineId: d.CommunicationLineId.Value,
                CommunicationLineName: "Unknown",
                Description: d.Description,
                Status: d.Status.ToString(),
                CreatedAt: d.CreatedAt,
                LastCommunicationAt: d.LastCommunicationAt,
                TagCount: d.Tags.Count
            )).ToList();

            return Result<List<DeviceDto>>.Success(deviceDtos);
        }
        catch (Exception ex)
        {
            return Result<List<DeviceDto>>.Failure<List<DeviceDto>>(Error.Failure("GetDevicesFailed", ex.Message));
        }
    }
}
