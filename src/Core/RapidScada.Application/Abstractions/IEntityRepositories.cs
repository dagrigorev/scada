using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Application.Abstractions;

/// <summary>
/// Repository for device entities
/// </summary>
public interface IDeviceRepository : IRepository<Device, DeviceId>
{
    /// <summary>
    /// Get devices by communication line
    /// </summary>
    Task<IReadOnlyList<Device>> GetByCommunicationLineAsync(
        CommunicationLineId lineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get devices by status
    /// </summary>
    Task<IReadOnlyList<Device>> GetByStatusAsync(
        DeviceStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get devices that haven't communicated since a given time
    /// </summary>
    Task<IReadOnlyList<Device>> GetStaleDevicesAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get device with tags loaded
    /// </summary>
    Task<Device?> GetWithTagsAsync(
        DeviceId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for communication line entities
/// </summary>
public interface ICommunicationLineRepository : IRepository<CommunicationLine, CommunicationLineId>
{
    /// <summary>
    /// Get all active communication lines
    /// </summary>
    Task<IReadOnlyList<CommunicationLine>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get communication lines by channel type
    /// </summary>
    Task<IReadOnlyList<CommunicationLine>> GetByChannelTypeAsync(
        ChannelType channelType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get communication line with devices loaded
    /// </summary>
    Task<CommunicationLine?> GetWithDevicesAsync(
        CommunicationLineId id,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository for tag entities
/// </summary>
public interface ITagRepository : IRepository<Tag, TagId>
{
    /// <summary>
    /// Get tags by device
    /// </summary>
    Task<IReadOnlyList<Tag>> GetByDeviceAsync(
        DeviceId deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tag by number within a device
    /// </summary>
    Task<Tag?> GetByNumberAsync(
        DeviceId deviceId,
        int tagNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tags with current values
    /// </summary>
    Task<IReadOnlyList<Tag>> GetWithCurrentValuesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get tags in alarm state
    /// </summary>
    Task<IReadOnlyList<Tag>> GetAlarmsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update tag values
    /// </summary>
    Task BulkUpdateValuesAsync(
        IEnumerable<(TagId TagId, TagValue Value)> updates,
        CancellationToken cancellationToken = default);
}
