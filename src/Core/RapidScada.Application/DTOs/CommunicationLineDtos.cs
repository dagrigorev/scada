namespace RapidScada.Application.DTOs;

/// <summary>
/// DTO for creating a communication line
/// </summary>
public sealed record CreateCommunicationLineDto(
    string Name,
    string ChannelType,
    ConnectionSettingsDto ConnectionSettings);

/// <summary>
/// DTO for updating a communication line
/// </summary>
public sealed record UpdateCommunicationLineDto(
    int Id,
    string Name,
    ConnectionSettingsDto ConnectionSettings);

/// <summary>
/// DTO for communication line response
/// </summary>
public sealed record CommunicationLineDto(
    int Id,
    string Name,
    string ChannelType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    int DeviceCount,
    ConnectionSettingsDto ConnectionSettings);

/// <summary>
/// DTO for communication line with devices
/// </summary>
public sealed record CommunicationLineDetailsDto(
    int Id,
    string Name,
    string ChannelType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastActivityAt,
    List<DeviceDto> Devices,
    ConnectionSettingsDto ConnectionSettings);

/// <summary>
/// Base DTO for connection settings
/// </summary>
public abstract record ConnectionSettingsDto(string Type);

/// <summary>
/// DTO for TCP client connection settings
/// </summary>
public sealed record TcpClientSettingsDto(
    string Host,
    int Port,
    int TimeoutMs,
    bool UseKeepAlive) : ConnectionSettingsDto("TcpClient");

/// <summary>
/// DTO for TCP server connection settings
/// </summary>
public sealed record TcpServerSettingsDto(
    int Port,
    int TimeoutMs,
    int MaxConnections) : ConnectionSettingsDto("TcpServer");

/// <summary>
/// DTO for serial port connection settings
/// </summary>
public sealed record SerialPortSettingsDto(
    string PortName,
    int BaudRate,
    int DataBits,
    string Parity,
    string StopBits,
    int TimeoutMs) : ConnectionSettingsDto("SerialPort");

/// <summary>
/// DTO for UDP connection settings
/// </summary>
public sealed record UdpSettingsDto(
    string Host,
    int Port,
    int TimeoutMs,
    bool Broadcast) : ConnectionSettingsDto("Udp");
