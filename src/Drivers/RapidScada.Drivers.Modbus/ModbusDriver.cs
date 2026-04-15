using Microsoft.Extensions.Logging;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using RapidScada.Drivers.Abstractions;
using RapidScada.Drivers.Modbus.Protocol;
using RapidScada.Drivers.Modbus.Transport;

namespace RapidScada.Drivers.Modbus;

/// <summary>
/// Modbus RTU and TCP driver implementation
/// </summary>
public sealed class ModbusDriver : DeviceDriverBase
{
    private readonly bool _isTcp;
    private IModbusTransport? _transport;
    private ModbusDeviceTemplate? _template;
    private byte _slaveAddress;
    private ushort _transactionId;

    public ModbusDriver(ILogger<ModbusDriver> logger, bool isTcp = false)
        : base(logger)
    {
        _isTcp = isTcp;
    }

    public override DriverInfo Info => new(
        Name: _isTcp ? "Modbus TCP" : "Modbus RTU",
        Version: "8.0.0",
        Manufacturer: "Rapid SCADA Modern",
        Description: $"Modbus {(_isTcp ? "TCP" : "RTU")} protocol driver with support for all standard function codes",
        SupportedProtocols: _isTcp
            ? new[] { "Modbus TCP" }
            : new[] { "Modbus RTU", "Modbus ASCII" });

    protected override async Task<Result> OnInitializeAsync(
        Device device,
        ConnectionSettings connectionSettings,
        CancellationToken cancellationToken)
    {
        // Create transport based on connection settings
        _transport = connectionSettings switch
        {
            TcpClientSettings tcp => new ModbusTcpTransport(tcp, Logger),
            SerialPortSettings serial => new ModbusRtuTransport(serial, Logger),
            _ => throw new InvalidOperationException($"Unsupported connection settings type: {connectionSettings.GetType()}")
        };

        // Load or create default template
        _template = CreateDefaultTemplate(device);
        _slaveAddress = (byte)device.Address.Value;

        Logger.LogInformation(
            "Initialized Modbus driver with {ElementCount} elements",
            _template.Elements.Count);

        return await Task.FromResult(Result.Success());
    }

    protected override async Task<Result> OnConnectAsync(CancellationToken cancellationToken)
    {
        if (_transport is null)
        {
            return Result.Failure(Error.Validation("Transport not initialized"));
        }

        try
        {
            await _transport.ConnectAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to connect");
            return Result.Failure(Error.Validation($"Connection failed: {ex.Message}"));
        }
    }

    protected override async Task OnDisconnectAsync()
    {
        if (_transport is not null)
        {
            await _transport.DisconnectAsync();
        }
    }

    protected override async Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        CancellationToken cancellationToken)
    {
        if (_template is null || _transport is null)
        {
            return Result.Failure<IReadOnlyList<TagReading>>(Error.Validation("Driver not initialized"));
        }

        var readings = new List<TagReading>();

        // Group elements by function code and optimize read operations
        var groups = _template.Elements
            .GroupBy(e => new { e.FunctionCode, AddressBlock = e.Address / 100 })
            .ToList();

        foreach (var group in groups)
        {
            try
            {
                var result = await ReadElementGroupAsync(group.ToList(), cancellationToken);
                if (result.IsSuccess)
                {
                    readings.AddRange(result.Value);
                }
                else
                {
                    Logger.LogWarning("Failed to read group: {Error}", result.Error.Message);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading element group");
            }
        }

        return Result.Success<IReadOnlyList<TagReading>>(readings);
    }

    protected override async Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        IEnumerable<int> tagNumbers,
        CancellationToken cancellationToken)
    {
        if (_template is null)
        {
            return Result.Failure<IReadOnlyList<TagReading>>(Error.Validation("Driver not initialized"));
        }

        var elements = _template.Elements
            .Where(e => tagNumbers.Contains(e.TagNumber))
            .ToList();

        if (elements.Count == 0)
        {
            return Result.Success<IReadOnlyList<TagReading>>(Array.Empty<TagReading>());
        }

        var readings = new List<TagReading>();

        foreach (var element in elements)
        {
            try
            {
                var result = await ReadSingleElementAsync(element, cancellationToken);
                if (result.IsSuccess)
                {
                    readings.Add(result.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading tag {TagNumber}", element.TagNumber);
            }
        }

        return Result.Success<IReadOnlyList<TagReading>>(readings);
    }

    protected override async Task<Result> OnWriteTagAsync(
        int tagNumber,
        object value,
        CancellationToken cancellationToken)
    {
        if (_template is null || _transport is null)
        {
            return Result.Failure(Error.Validation("Driver not initialized"));
        }

        var element = _template.Elements.FirstOrDefault(e => e.TagNumber == tagNumber);
        if (element is null)
        {
            return Result.Failure(Error.NotFound("Tag", tagNumber));
        }

        try
        {
            ModbusPdu request;

            if (element.FunctionCode == ModbusFunctionCode.ReadCoils)
            {
                // Write single coil
                var boolValue = Convert.ToBoolean(value);
                request = ModbusRequestBuilder.WriteSingleCoil(element.Address, boolValue);
            }
            else
            {
                // Write single or multiple registers
                var registerValue = Convert.ToUInt16(value);
                request = ModbusRequestBuilder.WriteSingleRegister(element.Address, registerValue);
            }

            var response = _isTcp
                ? await SendTcpRequestAsync(request, cancellationToken)
                : await SendRtuRequestAsync(request, cancellationToken);

            if (response.IsFailure)
            {
                return Result.Failure(response.Error);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error writing tag {TagNumber}", tagNumber);
            return Result.Failure(Error.Validation($"Write failed: {ex.Message}"));
        }
    }

    protected override Task<Result> OnSendCommandAsync(
        string command,
        object? parameters,
        CancellationToken cancellationToken)
    {
        // Modbus doesn't have specific commands beyond read/write
        return Task.FromResult(Result.Failure(Error.Validation("Custom commands not supported")));
    }

    private async Task<Result<IReadOnlyList<TagReading>>> ReadElementGroupAsync(
        List<ModbusElement> elements,
        CancellationToken cancellationToken)
    {
        if (elements.Count == 0 || _transport is null)
        {
            return Result.Success<IReadOnlyList<TagReading>>(Array.Empty<TagReading>());
        }

        var firstElement = elements.First();
        var minAddress = elements.Min(e => e.Address);
        var maxAddress = elements.Max(e => (ushort)(e.Address + e.RegisterCount - 1));
        var quantity = (ushort)(maxAddress - minAddress + 1);

        ModbusPdu request = firstElement.FunctionCode switch
        {
            ModbusFunctionCode.ReadCoils => ModbusRequestBuilder.ReadCoils(minAddress, quantity),
            ModbusFunctionCode.ReadDiscreteInputs => ModbusRequestBuilder.ReadDiscreteInputs(minAddress, quantity),
            ModbusFunctionCode.ReadHoldingRegisters => ModbusRequestBuilder.ReadHoldingRegisters(minAddress, quantity > 125 ? (ushort)125 : quantity),
            ModbusFunctionCode.ReadInputRegisters => ModbusRequestBuilder.ReadInputRegisters(minAddress, quantity > 125 ? (ushort)125 : quantity),
            _ => throw new InvalidOperationException($"Unsupported function code: {firstElement.FunctionCode}")
        };

        var responseResult = _isTcp
            ? await SendTcpRequestAsync(request, cancellationToken)
            : await SendRtuRequestAsync(request, cancellationToken);

        if (responseResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<TagReading>>(responseResult.Error);
        }

        var readings = new List<TagReading>();
        var response = responseResult.Value;

        if (firstElement.FunctionCode is ModbusFunctionCode.ReadCoils or ModbusFunctionCode.ReadDiscreteInputs)
        {
            var coils = ModbusResponseParser.ParseCoils(response, quantity);
            foreach (var element in elements)
            {
                var index = element.Address - minAddress;
                readings.Add(new TagReading(element.TagNumber, coils[index], DateTime.UtcNow));
            }
        }
        else
        {
            var registers = ModbusResponseParser.ParseRegisters(response);
            foreach (var element in elements)
            {
                var startIndex = element.Address - minAddress;
                var elementRegisters = registers[startIndex..(startIndex + element.RegisterCount)];
                var value = ModbusResponseParser.ConvertRegisters(elementRegisters, element.DataType);
                readings.Add(new TagReading(element.TagNumber, value, DateTime.UtcNow));
            }
        }

        return Result.Success<IReadOnlyList<TagReading>>(readings);
    }

    private async Task<Result<TagReading>> ReadSingleElementAsync(
        ModbusElement element,
        CancellationToken cancellationToken)
    {
        var result = await ReadElementGroupAsync([element], cancellationToken);
        return result.IsSuccess && result.Value.Count > 0
            ? Result.Success(result.Value[0])
            : Result.Failure<TagReading>(result.Error);
    }

    private async Task<Result<ModbusPdu>> SendTcpRequestAsync(
        ModbusPdu request,
        CancellationToken cancellationToken)
    {
        if (_transport is not ModbusTcpTransport tcpTransport)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Invalid transport type"));
        }

        var adu = new ModbusTcpAdu
        {
            TransactionId = _transactionId++,
            UnitId = _slaveAddress,
            Pdu = request
        };

        return await tcpTransport.SendRequestAsync(adu, cancellationToken);
    }

    private async Task<Result<ModbusPdu>> SendRtuRequestAsync(
        ModbusPdu request,
        CancellationToken cancellationToken)
    {
        if (_transport is not ModbusRtuTransport rtuTransport)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Invalid transport type"));
        }

        var adu = new ModbusRtuAdu
        {
            SlaveAddress = _slaveAddress,
            Pdu = request
        };

        return await rtuTransport.SendRequestAsync(adu, cancellationToken);
    }

    private static ModbusDeviceTemplate CreateDefaultTemplate(Device device)
    {
        var template = new ModbusDeviceTemplate
        {
            Name = device.Name.Value,
            SlaveAddress = (byte)device.Address.Value
        };

        // Add default elements based on device tags
        var tagNumber = 1;
        foreach (var tag in device.Tags)
        {
            var element = new ModbusElement(
                TagNumber: tag.Number,
                Name: tag.Name,
                FunctionCode: ModbusFunctionCode.ReadHoldingRegisters,
                Address: (ushort)((tagNumber - 1) * 2),
                DataType: MapTagTypeToModbusType(tag.TagType));

            template.AddElement(element);
            tagNumber++;
        }

        return template;
    }

    private static ModbusDataType MapTagTypeToModbusType(TagType tagType)
    {
        return tagType switch
        {
            TagType.Boolean => ModbusDataType.Bool,
            TagType.Integer => ModbusDataType.Int16,
            TagType.Real => ModbusDataType.Float,
            TagType.String => ModbusDataType.String,
            _ => ModbusDataType.UInt16
        };
    }
}
