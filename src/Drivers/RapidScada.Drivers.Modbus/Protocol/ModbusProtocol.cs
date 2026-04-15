namespace RapidScada.Drivers.Modbus.Protocol;

/// <summary>
/// Modbus function codes
/// </summary>
public enum ModbusFunctionCode : byte
{
    ReadCoils = 0x01,
    ReadDiscreteInputs = 0x02,
    ReadHoldingRegisters = 0x03,
    ReadInputRegisters = 0x04,
    WriteSingleCoil = 0x05,
    WriteSingleRegister = 0x06,
    WriteMultipleCoils = 0x0F,
    WriteMultipleRegisters = 0x10
}

/// <summary>
/// Modbus data types
/// </summary>
public enum ModbusDataType
{
    Bool,       // Coil or discrete input
    UInt16,     // Single register
    Int16,      // Single register
    UInt32,     // Two registers
    Int32,      // Two registers
    Float,      // Two registers (IEEE 754)
    Double,     // Four registers (IEEE 754)
    String      // Multiple registers
}

/// <summary>
/// Modbus register element configuration
/// </summary>
public sealed record ModbusElement(
    int TagNumber,
    string Name,
    ModbusFunctionCode FunctionCode,
    ushort Address,
    ModbusDataType DataType,
    int Length = 1)
{
    /// <summary>
    /// Calculate number of registers/coils needed
    /// </summary>
    public int RegisterCount => DataType switch
    {
        ModbusDataType.Bool => 1,
        ModbusDataType.UInt16 or ModbusDataType.Int16 => 1,
        ModbusDataType.UInt32 or ModbusDataType.Int32 or ModbusDataType.Float => 2,
        ModbusDataType.Double => 4,
        ModbusDataType.String => Length,
        _ => throw new InvalidOperationException($"Unknown data type: {DataType}")
    };
}

/// <summary>
/// Modbus device template
/// </summary>
public sealed class ModbusDeviceTemplate
{
    private readonly List<ModbusElement> _elements = [];

    public string Name { get; init; } = "Modbus Device";
    public byte SlaveAddress { get; init; } = 1;
    public IReadOnlyList<ModbusElement> Elements => _elements.AsReadOnly();

    public void AddElement(ModbusElement element)
    {
        _elements.Add(element);
    }

    public void AddElements(IEnumerable<ModbusElement> elements)
    {
        _elements.AddRange(elements);
    }
}

/// <summary>
/// Modbus PDU (Protocol Data Unit)
/// </summary>
public sealed class ModbusPdu
{
    public ModbusFunctionCode FunctionCode { get; init; }
    public byte[] Data { get; init; } = Array.Empty<byte>();

    public byte[] ToBytes()
    {
        var result = new byte[1 + Data.Length];
        result[0] = (byte)FunctionCode;
        Array.Copy(Data, 0, result, 1, Data.Length);
        return result;
    }

    public static ModbusPdu FromBytes(byte[] bytes)
    {
        if (bytes.Length < 1)
        {
            throw new ArgumentException("PDU must be at least 1 byte", nameof(bytes));
        }

        return new ModbusPdu
        {
            FunctionCode = (ModbusFunctionCode)bytes[0],
            Data = bytes.Length > 1 ? bytes[1..] : Array.Empty<byte>()
        };
    }
}

/// <summary>
/// Modbus ADU (Application Data Unit) for RTU
/// </summary>
public sealed class ModbusRtuAdu
{
    public byte SlaveAddress { get; init; }
    public ModbusPdu Pdu { get; init; } = null!;
    public ushort Crc { get; private set; }

    public byte[] ToBytes()
    {
        var pduBytes = Pdu.ToBytes();
        var result = new byte[1 + pduBytes.Length + 2];
        
        result[0] = SlaveAddress;
        Array.Copy(pduBytes, 0, result, 1, pduBytes.Length);
        
        Crc = CalculateCrc(result[..(1 + pduBytes.Length)]);
        result[^2] = (byte)(Crc & 0xFF);
        result[^1] = (byte)((Crc >> 8) & 0xFF);
        
        return result;
    }

    public static ModbusRtuAdu FromBytes(byte[] bytes)
    {
        if (bytes.Length < 4)
        {
            throw new ArgumentException("RTU ADU must be at least 4 bytes", nameof(bytes));
        }

        var crc = (ushort)(bytes[^2] | (bytes[^1] << 8));
        var calculatedCrc = CalculateCrc(bytes[..^2]);

        if (crc != calculatedCrc)
        {
            throw new InvalidOperationException($"CRC mismatch: expected {calculatedCrc:X4}, got {crc:X4}");
        }

        return new ModbusRtuAdu
        {
            SlaveAddress = bytes[0],
            Pdu = ModbusPdu.FromBytes(bytes[1..^2]),
            Crc = crc
        };
    }

    private static ushort CalculateCrc(byte[] data)
    {
        ushort crc = 0xFFFF;

        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        return crc;
    }
}

/// <summary>
/// Modbus ADU for TCP
/// </summary>
public sealed class ModbusTcpAdu
{
    public ushort TransactionId { get; init; }
    public ushort ProtocolId { get; init; } = 0;
    public byte UnitId { get; init; } = 1;
    public ModbusPdu Pdu { get; init; } = null!;

    public byte[] ToBytes()
    {
        var pduBytes = Pdu.ToBytes();
        var length = (ushort)(1 + pduBytes.Length); // UnitId + PDU
        
        var result = new byte[7 + pduBytes.Length];
        result[0] = (byte)((TransactionId >> 8) & 0xFF);
        result[1] = (byte)(TransactionId & 0xFF);
        result[2] = (byte)((ProtocolId >> 8) & 0xFF);
        result[3] = (byte)(ProtocolId & 0xFF);
        result[4] = (byte)((length >> 8) & 0xFF);
        result[5] = (byte)(length & 0xFF);
        result[6] = UnitId;
        Array.Copy(pduBytes, 0, result, 7, pduBytes.Length);
        
        return result;
    }

    public static ModbusTcpAdu FromBytes(byte[] bytes)
    {
        if (bytes.Length < 8)
        {
            throw new ArgumentException("TCP ADU must be at least 8 bytes", nameof(bytes));
        }

        var transactionId = (ushort)((bytes[0] << 8) | bytes[1]);
        var protocolId = (ushort)((bytes[2] << 8) | bytes[3]);
        var length = (ushort)((bytes[4] << 8) | bytes[5]);
        var unitId = bytes[6];

        return new ModbusTcpAdu
        {
            TransactionId = transactionId,
            ProtocolId = protocolId,
            UnitId = unitId,
            Pdu = ModbusPdu.FromBytes(bytes[7..])
        };
    }
}
