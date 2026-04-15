using RapidScada.Domain.Common;
using RapidScada.Domain.Events;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Domain.Entities;

/// <summary>
/// Represents a physical device (КП - Контролируемый Пункт) in the SCADA system
/// </summary>
public sealed class Device : Entity<DeviceId>
{
    private readonly List<Tag> _tags = [];

    private Device(DeviceId id) : base(id)
    {
    }

    public DeviceName Name { get; private set; } = null!;
    public DeviceTypeId DeviceTypeId { get; private set; } = null!;
    public DeviceAddress Address { get; private set; } = null!;
    public CallSign? CallSign { get; private set; }
    public CommunicationLineId CommunicationLineId { get; private set; } = null!;
    public string? Description { get; private set; }
    public DeviceStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastCommunicationAt { get; private set; }
    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Create a new device
    /// </summary>
    public static Result<Device> Create(
        DeviceId id,
        DeviceName name,
        DeviceTypeId deviceTypeId,
        DeviceAddress address,
        CommunicationLineId communicationLineId,
        CallSign? callSign = null,
        string? description = null)
    {
        var device = new Device(id)
        {
            Name = name,
            DeviceTypeId = deviceTypeId,
            Address = address,
            CallSign = callSign,
            CommunicationLineId = communicationLineId,
            Description = description,
            Status = DeviceStatus.Offline,
            CreatedAt = DateTime.UtcNow
        };

        device.RaiseDomainEvent(new DeviceCreatedEvent(device.Id, device.Name.Value));

        return Result.Success(device);
    }

    /// <summary>
    /// Update device configuration
    /// </summary>
    public Result UpdateConfiguration(
        DeviceName name,
        DeviceAddress address,
        CallSign? callSign,
        string? description)
    {
        Name = name;
        Address = address;
        CallSign = callSign;
        Description = description;

        RaiseDomainEvent(new DeviceConfigurationUpdatedEvent(Id, Name.Value));

        return Result.Success();
    }

    /// <summary>
    /// Add a tag to the device
    /// </summary>
    public Result AddTag(Tag tag)
    {
        if (_tags.Any(t => t.Number == tag.Number))
        {
            return Result.Failure(Error.Conflict($"Tag with number {tag.Number} already exists"));
        }

        _tags.Add(tag);
        return Result.Success();
    }

    /// <summary>
    /// Update device status based on communication result
    /// </summary>
    public void UpdateCommunicationStatus(bool success)
    {
        var previousStatus = Status;
        Status = success ? DeviceStatus.Online : DeviceStatus.Offline;
        LastCommunicationAt = DateTime.UtcNow;

        if (previousStatus != Status)
        {
            RaiseDomainEvent(new DeviceStatusChangedEvent(
                Id,
                Name.Value,
                previousStatus,
                Status,
                LastCommunicationAt.Value));
        }
    }

    /// <summary>
    /// Mark device as in error state
    /// </summary>
    public void SetErrorState(string errorMessage)
    {
        var previousStatus = Status;
        Status = DeviceStatus.Error;
        LastCommunicationAt = DateTime.UtcNow;

        if (previousStatus != Status)
        {
            RaiseDomainEvent(new DeviceStatusChangedEvent(
                Id,
                Name.Value,
                previousStatus,
                Status,
                LastCommunicationAt.Value,
                errorMessage));
        }
    }
}

/// <summary>
/// Device operational status
/// </summary>
public enum DeviceStatus
{
    Offline = 0,
    Online = 1,
    Error = 2,
    Maintenance = 3
}
