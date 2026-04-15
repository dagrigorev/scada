using Microsoft.Extensions.Logging;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Common;
using RapidScada.Domain.ValueObjects;
using RapidScada.Drivers.Modbus;

namespace RapidScada.Communicator;

/// <summary>
/// Factory for creating device drivers
/// </summary>
public sealed class DeviceDriverFactory : IDeviceDriverFactory
{
    private readonly ILogger<DeviceDriverFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<int, DeviceTypeInfo> _supportedTypes;

    public DeviceDriverFactory(
        ILogger<DeviceDriverFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _supportedTypes = new Dictionary<int, DeviceTypeInfo>();

        RegisterDefaultDrivers();
    }

    public Result<IDeviceDriver> CreateDriver(DeviceTypeId deviceTypeId)
    {
        if (!_supportedTypes.TryGetValue(deviceTypeId.Value, out var typeInfo))
        {
            return Result.Failure<IDeviceDriver>(
                Error.NotFound("DeviceType", deviceTypeId.Value));
        }

        try
        {
            IDeviceDriver driver = deviceTypeId.Value switch
            {
                1 => new ModbusDriver(_loggerFactory.CreateLogger<ModbusDriver>(), isTcp: true),
                2 => new ModbusDriver(_loggerFactory.CreateLogger<ModbusDriver>(), isTcp: false),
                _ => throw new InvalidOperationException($"Unknown device type: {deviceTypeId}")
            };

            _logger.LogDebug(
                "Created driver {DriverName} for device type {TypeId}",
                typeInfo.DriverInfo.Name,
                deviceTypeId);

            return Result.Success(driver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create driver for device type {TypeId}", deviceTypeId);
            return Result.Failure<IDeviceDriver>(
                Error.Validation($"Failed to create driver: {ex.Message}"));
        }
    }

    public IReadOnlyList<DeviceTypeInfo> GetSupportedDeviceTypes()
    {
        return _supportedTypes.Values.ToList();
    }

    public bool IsSupported(DeviceTypeId deviceTypeId)
    {
        return _supportedTypes.ContainsKey(deviceTypeId.Value);
    }

    private void RegisterDefaultDrivers()
    {
        // Modbus TCP
        var modbusTcpInfo = new DriverInfo(
            Name: "Modbus TCP",
            Version: "8.0.0",
            Manufacturer: "Rapid SCADA Modern",
            Description: "Modbus TCP/IP protocol driver",
            SupportedProtocols: new[] { "Modbus TCP" });

        _supportedTypes.Add(1, new DeviceTypeInfo(
            Id: DeviceTypeId.Create(1),
            Name: "Modbus TCP Device",
            Description: "Device using Modbus TCP protocol over Ethernet",
            DriverInfo: modbusTcpInfo));

        // Modbus RTU
        var modbusRtuInfo = new DriverInfo(
            Name: "Modbus RTU",
            Version: "8.0.0",
            Manufacturer: "Rapid SCADA Modern",
            Description: "Modbus RTU protocol driver for serial communication",
            SupportedProtocols: new[] { "Modbus RTU" });

        _supportedTypes.Add(2, new DeviceTypeInfo(
            Id: DeviceTypeId.Create(2),
            Name: "Modbus RTU Device",
            Description: "Device using Modbus RTU protocol over RS-232/RS-485",
            DriverInfo: modbusRtuInfo));

        _logger.LogInformation(
            "Registered {Count} device driver types",
            _supportedTypes.Count);
    }

    /// <summary>
    /// Register a custom driver type
    /// </summary>
    public void RegisterDriver(int typeId, DeviceTypeInfo typeInfo, Func<IDeviceDriver> factory)
    {
        _supportedTypes[typeId] = typeInfo;
        _logger.LogInformation(
            "Registered custom driver: {TypeId} - {Name}",
            typeId,
            typeInfo.Name);
    }
}
