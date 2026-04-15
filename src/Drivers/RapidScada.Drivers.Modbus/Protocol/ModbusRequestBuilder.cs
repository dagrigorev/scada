using System.Text;

namespace RapidScada.Drivers.Modbus.Protocol;

/// <summary>
/// Builder for Modbus requests
/// </summary>
public static class ModbusRequestBuilder
{
    /// <summary>
    /// Build a read coils request
    /// </summary>
    public static ModbusPdu ReadCoils(ushort startAddress, ushort quantity)
    {
        if (quantity < 1 || quantity > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 2000");
        }

        var data = new byte[4];
        data[0] = (byte)((startAddress >> 8) & 0xFF);
        data[1] = (byte)(startAddress & 0xFF);
        data[2] = (byte)((quantity >> 8) & 0xFF);
        data[3] = (byte)(quantity & 0xFF);

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.ReadCoils,
            Data = data
        };
    }

    /// <summary>
    /// Build a read discrete inputs request
    /// </summary>
    public static ModbusPdu ReadDiscreteInputs(ushort startAddress, ushort quantity)
    {
        if (quantity < 1 || quantity > 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 2000");
        }

        var data = new byte[4];
        data[0] = (byte)((startAddress >> 8) & 0xFF);
        data[1] = (byte)(startAddress & 0xFF);
        data[2] = (byte)((quantity >> 8) & 0xFF);
        data[3] = (byte)(quantity & 0xFF);

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.ReadDiscreteInputs,
            Data = data
        };
    }

    /// <summary>
    /// Build a read holding registers request
    /// </summary>
    public static ModbusPdu ReadHoldingRegisters(ushort startAddress, ushort quantity)
    {
        if (quantity < 1 || quantity > 125)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 125");
        }

        var data = new byte[4];
        data[0] = (byte)((startAddress >> 8) & 0xFF);
        data[1] = (byte)(startAddress & 0xFF);
        data[2] = (byte)((quantity >> 8) & 0xFF);
        data[3] = (byte)(quantity & 0xFF);

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.ReadHoldingRegisters,
            Data = data
        };
    }

    /// <summary>
    /// Build a read input registers request
    /// </summary>
    public static ModbusPdu ReadInputRegisters(ushort startAddress, ushort quantity)
    {
        if (quantity < 1 || quantity > 125)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be between 1 and 125");
        }

        var data = new byte[4];
        data[0] = (byte)((startAddress >> 8) & 0xFF);
        data[1] = (byte)(startAddress & 0xFF);
        data[2] = (byte)((quantity >> 8) & 0xFF);
        data[3] = (byte)(quantity & 0xFF);

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.ReadInputRegisters,
            Data = data
        };
    }

    /// <summary>
    /// Build a write single coil request
    /// </summary>
    public static ModbusPdu WriteSingleCoil(ushort address, bool value)
    {
        var data = new byte[4];
        data[0] = (byte)((address >> 8) & 0xFF);
        data[1] = (byte)(address & 0xFF);
        data[2] = value ? (byte)0xFF : (byte)0x00;
        data[3] = 0x00;

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.WriteSingleCoil,
            Data = data
        };
    }

    /// <summary>
    /// Build a write single register request
    /// </summary>
    public static ModbusPdu WriteSingleRegister(ushort address, ushort value)
    {
        var data = new byte[4];
        data[0] = (byte)((address >> 8) & 0xFF);
        data[1] = (byte)(address & 0xFF);
        data[2] = (byte)((value >> 8) & 0xFF);
        data[3] = (byte)(value & 0xFF);

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.WriteSingleRegister,
            Data = data
        };
    }

    /// <summary>
    /// Build a write multiple registers request
    /// </summary>
    public static ModbusPdu WriteMultipleRegisters(ushort startAddress, ushort[] values)
    {
        if (values.Length < 1 || values.Length > 123)
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Value count must be between 1 and 123");
        }

        var byteCount = (byte)(values.Length * 2);
        var data = new byte[5 + byteCount];
        
        data[0] = (byte)((startAddress >> 8) & 0xFF);
        data[1] = (byte)(startAddress & 0xFF);
        data[2] = (byte)((values.Length >> 8) & 0xFF);
        data[3] = (byte)(values.Length & 0xFF);
        data[4] = byteCount;

        for (int i = 0; i < values.Length; i++)
        {
            data[5 + i * 2] = (byte)((values[i] >> 8) & 0xFF);
            data[5 + i * 2 + 1] = (byte)(values[i] & 0xFF);
        }

        return new ModbusPdu
        {
            FunctionCode = ModbusFunctionCode.WriteMultipleRegisters,
            Data = data
        };
    }
}

/// <summary>
/// Parser for Modbus responses
/// </summary>
public static class ModbusResponseParser
{
    /// <summary>
    /// Parse coils from response
    /// </summary>
    public static bool[] ParseCoils(ModbusPdu response, int count)
    {
        if (response.Data.Length < 1)
        {
            throw new InvalidOperationException("Invalid response format");
        }

        var byteCount = response.Data[0];
        var coils = new bool[count];

        for (int i = 0; i < count; i++)
        {
            var byteIndex = i / 8;
            var bitIndex = i % 8;
            coils[i] = (response.Data[1 + byteIndex] & (1 << bitIndex)) != 0;
        }

        return coils;
    }

    /// <summary>
    /// Parse registers from response
    /// </summary>
    public static ushort[] ParseRegisters(ModbusPdu response)
    {
        if (response.Data.Length < 1)
        {
            throw new InvalidOperationException("Invalid response format");
        }

        var byteCount = response.Data[0];
        var registerCount = byteCount / 2;
        var registers = new ushort[registerCount];

        for (int i = 0; i < registerCount; i++)
        {
            registers[i] = (ushort)((response.Data[1 + i * 2] << 8) | response.Data[1 + i * 2 + 1]);
        }

        return registers;
    }

    /// <summary>
    /// Convert registers to typed value
    /// </summary>
    public static object ConvertRegisters(ushort[] registers, ModbusDataType dataType)
    {
        return dataType switch
        {
            ModbusDataType.Bool => registers[0] != 0,
            ModbusDataType.UInt16 => registers[0],
            ModbusDataType.Int16 => (short)registers[0],
            ModbusDataType.UInt32 => (uint)((registers[0] << 16) | registers[1]),
            ModbusDataType.Int32 => (int)((registers[0] << 16) | registers[1]),
            ModbusDataType.Float => ConvertToFloat(registers),
            ModbusDataType.Double => ConvertToDouble(registers),
            ModbusDataType.String => ConvertToString(registers),
            _ => throw new InvalidOperationException($"Unsupported data type: {dataType}")
        };
    }

    private static float ConvertToFloat(ushort[] registers)
    {
        var bytes = new byte[4];
        bytes[0] = (byte)(registers[1] & 0xFF);
        bytes[1] = (byte)((registers[1] >> 8) & 0xFF);
        bytes[2] = (byte)(registers[0] & 0xFF);
        bytes[3] = (byte)((registers[0] >> 8) & 0xFF);
        return BitConverter.ToSingle(bytes, 0);
    }

    private static double ConvertToDouble(ushort[] registers)
    {
        var bytes = new byte[8];
        for (int i = 0; i < 4; i++)
        {
            bytes[i * 2] = (byte)(registers[3 - i] & 0xFF);
            bytes[i * 2 + 1] = (byte)((registers[3 - i] >> 8) & 0xFF);
        }
        return BitConverter.ToDouble(bytes, 0);
    }

    private static string ConvertToString(ushort[] registers)
    {
        var bytes = new byte[registers.Length * 2];
        for (int i = 0; i < registers.Length; i++)
        {
            bytes[i * 2] = (byte)((registers[i] >> 8) & 0xFF);
            bytes[i * 2 + 1] = (byte)(registers[i] & 0xFF);
        }
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }
}
