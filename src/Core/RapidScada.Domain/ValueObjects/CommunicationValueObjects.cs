using RapidScada.Domain.Common;
using System.Text.Json;

namespace RapidScada.Domain.ValueObjects;

/// <summary>
/// Connection settings for communication channels
/// </summary>
public abstract class ConnectionSettings : ValueObject
{
    protected ConnectionSettings(string host, int port, int timeoutMs)
    {
        Host = host;
        Port = port;
        TimeoutMs = timeoutMs;
    }

    public string Host { get; }
    public int Port { get; }
    public int TimeoutMs { get; }
}

/// <summary>
/// TCP Client connection settings
/// </summary>
public sealed class TcpClientSettings : ConnectionSettings
{
    private TcpClientSettings(string host, int port, int timeoutMs, bool useKeepAlive)
        : base(host, port, timeoutMs)
    {
        UseKeepAlive = useKeepAlive;
    }

    public bool UseKeepAlive { get; }

    public static Result<TcpClientSettings> Create(string host, int port, int timeoutMs = 5000, bool useKeepAlive = true)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return Result.Failure<TcpClientSettings>(Error.InvalidValue(nameof(host), "Host cannot be empty"));
        }

        if (port <= 0 || port > 65535)
        {
            return Result.Failure<TcpClientSettings>(Error.InvalidValue(nameof(port), "Port must be between 1 and 65535"));
        }

        if (timeoutMs <= 0)
        {
            return Result.Failure<TcpClientSettings>(Error.InvalidValue(nameof(timeoutMs), "Timeout must be positive"));
        }

        return Result.Success(new TcpClientSettings(host, port, timeoutMs, useKeepAlive));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Host;
        yield return Port;
        yield return TimeoutMs;
        yield return UseKeepAlive;
    }
}

/// <summary>
/// Serial port connection settings
/// </summary>
public sealed class SerialPortSettings : ConnectionSettings
{
    private SerialPortSettings(
        string portName,
        int baudRate,
        int dataBits,
        string parity,
        string stopBits,
        int timeoutMs)
        : base(portName, 0, timeoutMs)
    {
        PortName = portName;
        BaudRate = baudRate;
        DataBits = dataBits;
        Parity = parity;
        StopBits = stopBits;
    }

    public string PortName { get; }
    public int BaudRate { get; }
    public int DataBits { get; }
    public string Parity { get; }
    public string StopBits { get; }

    public static Result<SerialPortSettings> Create(
        string portName,
        int baudRate = 9600,
        int dataBits = 8,
        string parity = "None",
        string stopBits = "One",
        int timeoutMs = 1000)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            return Result.Failure<SerialPortSettings>(Error.InvalidValue(nameof(portName), "Port name cannot be empty"));
        }

        if (baudRate <= 0)
        {
            return Result.Failure<SerialPortSettings>(Error.InvalidValue(nameof(baudRate), "Baud rate must be positive"));
        }

        if (dataBits < 5 || dataBits > 8)
        {
            return Result.Failure<SerialPortSettings>(Error.InvalidValue(nameof(dataBits), "Data bits must be between 5 and 8"));
        }

        return Result.Success(new SerialPortSettings(portName, baudRate, dataBits, parity, stopBits, timeoutMs));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PortName;
        yield return BaudRate;
        yield return DataBits;
        yield return Parity;
        yield return StopBits;
        yield return TimeoutMs;
    }
}

/// <summary>
/// Tag value with quality and timestamp
/// </summary>
public sealed class TagValue : ValueObject
{
    private TagValue(object value, DateTime timestamp, double quality)
    {
        Value = value;
        Timestamp = timestamp;
        Quality = quality;
    }

    public object Value { get; }
    public DateTime Timestamp { get; }
    public double Quality { get; } // 0.0 (bad) to 1.0 (good)

    public static TagValue Create(object value, double quality = 1.0)
    {
        return new TagValue(value, DateTime.UtcNow, Math.Clamp(quality, 0.0, 1.0));
    }

    public static TagValue Create(object value, DateTime timestamp, double quality = 1.0)
    {
        return new TagValue(value, timestamp, Math.Clamp(quality, 0.0, 1.0));
    }

    public bool TryGetNumericValue(out double numericValue)
    {
        return double.TryParse(Value.ToString(), out numericValue);
    }

    public T? GetValue<T>()
    {
        try
        {
            if (Value is T typedValue)
            {
                return typedValue;
            }

            return (T?)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Timestamp;
        yield return Quality;
    }

    public override string ToString() => $"{Value} (Q: {Quality:P0}, T: {Timestamp:yyyy-MM-dd HH:mm:ss})";
}
