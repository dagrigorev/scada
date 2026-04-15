using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Application.Abstractions;

/// <summary>
/// Interface for device communication drivers
/// </summary>
public interface IDeviceDriver
{
    /// <summary>
    /// Driver name and version information
    /// </summary>
    DriverInfo Info { get; }

    /// <summary>
    /// Initialize the driver with configuration
    /// </summary>
    Task<Result> InitializeAsync(
        Device device,
        ConnectionSettings connectionSettings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connect to the device
    /// </summary>
    Task<Result> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the device
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Read all tag values from the device
    /// </summary>
    Task<Result<IReadOnlyList<TagReading>>> ReadTagsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Read specific tag values from the device
    /// </summary>
    Task<Result<IReadOnlyList<TagReading>>> ReadTagsAsync(
        IEnumerable<int> tagNumbers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a value to a device tag
    /// </summary>
    Task<Result> WriteTagAsync(
        int tagNumber,
        object value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a command to the device
    /// </summary>
    Task<Result> SendCommandAsync(
        string command,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the driver is connected
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get driver statistics
    /// </summary>
    DriverStatistics GetStatistics();
}

/// <summary>
/// Factory for creating device drivers
/// </summary>
public interface IDeviceDriverFactory
{
    /// <summary>
    /// Create a driver for the specified device type
    /// </summary>
    Result<IDeviceDriver> CreateDriver(DeviceTypeId deviceTypeId);

    /// <summary>
    /// Get all supported device types
    /// </summary>
    IReadOnlyList<DeviceTypeInfo> GetSupportedDeviceTypes();

    /// <summary>
    /// Check if a device type is supported
    /// </summary>
    bool IsSupported(DeviceTypeId deviceTypeId);
}

/// <summary>
/// Information about a device driver
/// </summary>
public sealed record DriverInfo(
    string Name,
    string Version,
    string Manufacturer,
    string Description,
    IReadOnlyList<string> SupportedProtocols);

/// <summary>
/// Information about a device type
/// </summary>
public sealed record DeviceTypeInfo(
    DeviceTypeId Id,
    string Name,
    string Description,
    DriverInfo DriverInfo);

/// <summary>
/// Result of reading a tag from a device
/// </summary>
public sealed record TagReading(
    int TagNumber,
    object Value,
    DateTime Timestamp,
    double Quality = 1.0);

/// <summary>
/// Driver execution statistics
/// </summary>
public sealed record DriverStatistics(
    int SuccessfulReads,
    int FailedReads,
    int SuccessfulWrites,
    int FailedWrites,
    TimeSpan AverageReadTime,
    TimeSpan AverageWriteTime,
    DateTime LastCommunication,
    string? LastError);
