using MediatR;
using Microsoft.Extensions.Logging;
using RapidScada.Application.Abstractions;
using RapidScada.Application.DTOs;
using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Application.Commands.Handlers;

/// <summary>
/// Handler for creating a new device
/// </summary>
public sealed class CreateDeviceCommandHandler : IRequestHandler<CreateDeviceCommand, Result<DeviceDto>>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly ICommunicationLineRepository _lineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateDeviceCommandHandler> _logger;

    public CreateDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        ICommunicationLineRepository lineRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateDeviceCommandHandler> logger)
    {
        _deviceRepository = deviceRepository;
        _lineRepository = lineRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DeviceDto>> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Device;

        // Validate communication line exists
        var lineId = CommunicationLineId.Create(dto.CommunicationLineId);
        var lineExists = await _lineRepository.ExistsAsync(lineId, cancellationToken);
        if (!lineExists)
        {
            return Result.Failure<DeviceDto>(
                Error.NotFound(nameof(CommunicationLine), dto.CommunicationLineId));
        }

        // Create value objects
        var nameResult = DeviceName.Create(dto.Name);
        if (nameResult.IsFailure)
        {
            return Result.Failure<DeviceDto>(nameResult.Error);
        }

        var addressResult = DeviceAddress.Create(dto.Address);
        if (addressResult.IsFailure)
        {
            return Result.Failure<DeviceDto>(addressResult.Error);
        }

        CallSign? callSign = null;
        if (!string.IsNullOrWhiteSpace(dto.CallSign))
        {
            var callSignResult = CallSign.Create(dto.CallSign);
            if (callSignResult.IsFailure)
            {
                return Result.Failure<DeviceDto>(callSignResult.Error);
            }
            callSign = callSignResult.Value;
        }

        // Create device entity
        var deviceResult = Device.Create(
            DeviceId.New(),
            nameResult.Value,
            DeviceTypeId.Create(dto.DeviceTypeId),
            addressResult.Value,
            lineId,
            callSign,
            dto.Description);

        if (deviceResult.IsFailure)
        {
            return Result.Failure<DeviceDto>(deviceResult.Error);
        }

        var device = deviceResult.Value;

        // Persist
        await _deviceRepository.AddAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device created: {DeviceId} - {DeviceName}",
            device.Id,
            device.Name.Value);

        // Map to DTO
        var result = new DeviceDto(
            device.Id.Value,
            device.Name.Value,
            device.DeviceTypeId.Value,
            "Unknown", // Would need device type lookup
            device.Address.Value,
            device.CallSign?.Value,
            device.CommunicationLineId.Value,
            "Unknown", // Would need line lookup
            device.Description,
            device.Status.ToString(),
            device.CreatedAt,
            device.LastCommunicationAt,
            0);

        return Result.Success(result);
    }
}

/// <summary>
/// Handler for updating device configuration
/// </summary>
public sealed class UpdateDeviceCommandHandler : IRequestHandler<UpdateDeviceCommand, Result>
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateDeviceCommandHandler> _logger;

    public UpdateDeviceCommandHandler(
        IDeviceRepository deviceRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateDeviceCommandHandler> logger)
    {
        _deviceRepository = deviceRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Device;
        var deviceId = DeviceId.Create(dto.DeviceId);

        var device = await _deviceRepository.GetByIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return Result.Failure(Error.NotFound(nameof(Device), dto.DeviceId));
        }

        var nameResult = DeviceName.Create(dto.Name);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        var addressResult = DeviceAddress.Create(dto.Address);
        if (addressResult.IsFailure)
        {
            return Result.Failure(addressResult.Error);
        }

        CallSign? callSign = null;
        if (!string.IsNullOrWhiteSpace(dto.CallSign))
        {
            var callSignResult = CallSign.Create(dto.CallSign);
            if (callSignResult.IsFailure)
            {
                return Result.Failure(callSignResult.Error);
            }
            callSign = callSignResult.Value;
        }

        var updateResult = device.UpdateConfiguration(
            nameResult.Value,
            addressResult.Value,
            callSign,
            dto.Description);

        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        await _deviceRepository.UpdateAsync(device, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Device updated: {DeviceId} - {DeviceName}",
            device.Id,
            device.Name.Value);

        return Result.Success();
    }
}

/// <summary>
/// Handler for bulk updating tag values
/// </summary>
public sealed class BulkUpdateTagValuesCommandHandler : IRequestHandler<BulkUpdateTagValuesCommand, Result>
{
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkUpdateTagValuesCommandHandler> _logger;

    public BulkUpdateTagValuesCommandHandler(
        ITagRepository tagRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkUpdateTagValuesCommandHandler> logger)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(BulkUpdateTagValuesCommand request, CancellationToken cancellationToken)
    {
        var updates = request.Updates
            .Select(u => (TagId.Create(u.TagId), TagValue.Create(u.Value, u.Quality)))
            .ToList();

        await _tagRepository.BulkUpdateValuesAsync(updates, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Bulk updated {Count} tag values", updates.Count);

        return Result.Success();
    }
}
