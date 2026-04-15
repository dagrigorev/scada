using Microsoft.Extensions.Logging;
using RapidScada.Domain.Common;
using RapidScada.Domain.ValueObjects;
using RapidScada.Drivers.Modbus.Protocol;
using System.IO.Ports;
using System.Net.Sockets;

namespace RapidScada.Drivers.Modbus.Transport;

/// <summary>
/// Interface for Modbus transport layer
/// </summary>
public interface IModbusTransport
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    bool IsConnected { get; }
}

/// <summary>
/// Modbus TCP transport implementation
/// </summary>
public sealed class ModbusTcpTransport : IModbusTransport
{
    private readonly TcpClientSettings _settings;
    private readonly ILogger _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;

    public ModbusTcpTransport(TcpClientSettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool IsConnected => _client?.Connected ?? false;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _client = new TcpClient();
            
            using var cts = new CancellationTokenSource(_settings.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            await _client.ConnectAsync(_settings.Host, _settings.Port, linkedCts.Token);
            _stream = _client.GetStream();

            _logger.LogInformation("Connected to {Host}:{Port}", _settings.Host, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Host}:{Port}", _settings.Host, _settings.Port);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        _client?.Close();
        _client?.Dispose();
        _client = null;

        _logger.LogInformation("Disconnected from {Host}:{Port}", _settings.Host, _settings.Port);
    }

    public async Task<Result<ModbusPdu>> SendRequestAsync(
        ModbusTcpAdu request,
        CancellationToken cancellationToken = default)
    {
        if (_stream is null || !IsConnected)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Not connected"));
        }

        try
        {
            var requestBytes = request.ToBytes();
            
            await _stream.WriteAsync(requestBytes, cancellationToken);
            await _stream.FlushAsync(cancellationToken);

            var responseBuffer = new byte[260]; // Max Modbus TCP frame size
            
            using var cts = new CancellationTokenSource(_settings.TimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

            var bytesRead = await _stream.ReadAsync(responseBuffer, linkedCts.Token);
            
            if (bytesRead < 8)
            {
                return Result.Failure<ModbusPdu>(Error.Validation("Invalid response length"));
            }

            var response = ModbusTcpAdu.FromBytes(responseBuffer[..bytesRead]);

            if (response.TransactionId != request.TransactionId)
            {
                return Result.Failure<ModbusPdu>(Error.Validation("Transaction ID mismatch"));
            }

            return Result.Success(response.Pdu);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Request timeout"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request");
            return Result.Failure<ModbusPdu>(Error.Validation($"Request failed: {ex.Message}"));
        }
    }
}

/// <summary>
/// Modbus RTU transport implementation
/// </summary>
public sealed class ModbusRtuTransport : IModbusTransport
{
    private readonly SerialPortSettings _settings;
    private readonly ILogger _logger;
    private SerialPort? _port;

    public ModbusRtuTransport(SerialPortSettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool IsConnected => _port?.IsOpen ?? false;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _port = new SerialPort
            {
                PortName = _settings.PortName,
                BaudRate = _settings.BaudRate,
                DataBits = _settings.DataBits,
                Parity = ParseParity(_settings.Parity),
                StopBits = ParseStopBits(_settings.StopBits),
                ReadTimeout = _settings.TimeoutMs,
                WriteTimeout = _settings.TimeoutMs
            };

            await Task.Run(() => _port.Open(), cancellationToken);

            _logger.LogInformation(
                "Opened serial port {PortName} at {BaudRate} baud",
                _settings.PortName,
                _settings.BaudRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open serial port {PortName}", _settings.PortName);
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_port is not null)
        {
            await Task.Run(() =>
            {
                if (_port.IsOpen)
                {
                    _port.Close();
                }
                _port.Dispose();
            });

            _port = null;
            _logger.LogInformation("Closed serial port {PortName}", _settings.PortName);
        }
    }

    public async Task<Result<ModbusPdu>> SendRequestAsync(
        ModbusRtuAdu request,
        CancellationToken cancellationToken = default)
    {
        if (_port is null || !IsConnected)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Not connected"));
        }

        try
        {
            // Clear buffers
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();

            // Calculate inter-frame delay (3.5 character times)
            var charTimeMs = (11.0 / _settings.BaudRate) * 1000;
            var interFrameDelayMs = (int)Math.Ceiling(charTimeMs * 3.5);
            
            await Task.Delay(interFrameDelayMs, cancellationToken);

            // Send request
            var requestBytes = request.ToBytes();
            await _port.BaseStream.WriteAsync(requestBytes, cancellationToken);
            await _port.BaseStream.FlushAsync(cancellationToken);

            // Wait for response
            await Task.Delay(interFrameDelayMs, cancellationToken);

            var responseBuffer = new byte[256]; // Max Modbus RTU frame size
            var bytesRead = 0;
            var startTime = DateTime.UtcNow;

            while (bytesRead < 4 && (DateTime.UtcNow - startTime).TotalMilliseconds < _settings.TimeoutMs)
            {
                if (_port.BytesToRead > 0)
                {
                    bytesRead += await _port.BaseStream.ReadAsync(
                        responseBuffer.AsMemory(bytesRead),
                        cancellationToken);
                }
                else
                {
                    await Task.Delay(10, cancellationToken);
                }
            }

            if (bytesRead < 4)
            {
                return Result.Failure<ModbusPdu>(Error.Validation("Response timeout"));
            }

            var response = ModbusRtuAdu.FromBytes(responseBuffer[..bytesRead]);

            if (response.SlaveAddress != request.SlaveAddress)
            {
                return Result.Failure<ModbusPdu>(Error.Validation("Slave address mismatch"));
            }

            return Result.Success(response.Pdu);
        }
        catch (OperationCanceledException)
        {
            return Result.Failure<ModbusPdu>(Error.Validation("Request timeout"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending RTU request");
            return Result.Failure<ModbusPdu>(Error.Validation($"Request failed: {ex.Message}"));
        }
    }

    private static Parity ParseParity(string parity)
    {
        return parity.ToLowerInvariant() switch
        {
            "none" => Parity.None,
            "odd" => Parity.Odd,
            "even" => Parity.Even,
            "mark" => Parity.Mark,
            "space" => Parity.Space,
            _ => Parity.None
        };
    }

    private static StopBits ParseStopBits(string stopBits)
    {
        return stopBits.ToLowerInvariant() switch
        {
            "none" => StopBits.None,
            "one" => StopBits.One,
            "two" => StopBits.Two,
            "onepointfive" or "1.5" => StopBits.OnePointFive,
            _ => StopBits.One
        };
    }
}
