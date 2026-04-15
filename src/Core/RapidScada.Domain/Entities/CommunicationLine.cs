using RapidScada.Domain.Common;
using RapidScada.Domain.Events;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Domain.Entities;

/// <summary>
/// Represents a communication line that connects to multiple devices
/// </summary>
public sealed class CommunicationLine : Entity<CommunicationLineId>
{
    private readonly List<DeviceId> _deviceIds = [];

    private CommunicationLine(CommunicationLineId id) : base(id)
    {
    }

    public CommunicationLineName Name { get; private set; } = null!;
    public ChannelType ChannelType { get; private set; }
    public ConnectionSettings ConnectionSettings { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public ICollection<DeviceId> DeviceIds => _deviceIds.AsReadOnly();

    /// <summary>
    /// Create a new communication line
    /// </summary>
    public static Result<CommunicationLine> Create(
        CommunicationLineId id,
        CommunicationLineName name,
        ChannelType channelType,
        ConnectionSettings connectionSettings)
    {
        var line = new CommunicationLine(id)
        {
            Name = name,
            ChannelType = channelType,
            ConnectionSettings = connectionSettings,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        line.RaiseDomainEvent(new CommunicationLineCreatedEvent(line.Id, line.Name.Value));

        return Result.Success(line);
    }

    /// <summary>
    /// Add a device to this communication line
    /// </summary>
    public Result AddDevice(DeviceId deviceId)
    {
        if (_deviceIds.Contains(deviceId))
        {
            return Result.Failure(Error.Conflict($"Device {deviceId} is already on this line"));
        }

        _deviceIds.Add(deviceId);
        return Result.Success();
    }

    /// <summary>
    /// Remove a device from this communication line
    /// </summary>
    public Result RemoveDevice(DeviceId deviceId)
    {
        if (!_deviceIds.Remove(deviceId))
        {
            return Result.Failure(Error.NotFound(nameof(Device), deviceId));
        }

        return Result.Success();
    }

    /// <summary>
    /// Activate the communication line
    /// </summary>
    public Result Activate()
    {
        if (IsActive)
        {
            return Result.Failure(Error.Conflict("Communication line is already active"));
        }

        IsActive = true;
        LastActivityAt = DateTime.UtcNow;

        RaiseDomainEvent(new CommunicationLineActivatedEvent(Id, Name.Value));

        return Result.Success();
    }

    /// <summary>
    /// Deactivate the communication line
    /// </summary>
    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(Error.Conflict("Communication line is already inactive"));
        }

        IsActive = false;
        LastActivityAt = DateTime.UtcNow;

        RaiseDomainEvent(new CommunicationLineDeactivatedEvent(Id, Name.Value));

        return Result.Success();
    }

    /// <summary>
    /// Update connection settings
    /// </summary>
    public Result UpdateConnectionSettings(ConnectionSettings connectionSettings)
    {
        if (IsActive)
        {
            return Result.Failure(Error.Conflict("Cannot update settings while line is active"));
        }

        ConnectionSettings = connectionSettings;

        return Result.Success();
    }

    /// <summary>
    /// Record activity on this line
    /// </summary>
    public void RecordActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Types of communication channels
/// </summary>
public enum ChannelType
{
    SerialPort = 1,
    TcpClient = 2,
    TcpServer = 3,
    Udp = 4
}
