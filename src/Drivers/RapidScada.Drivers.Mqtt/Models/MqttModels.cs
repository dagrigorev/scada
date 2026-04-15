using System.Text.Json.Serialization;

namespace RapidScada.Drivers.Mqtt.Models;

/// <summary>
/// MQTT broker connection configuration
/// </summary>
public sealed class MqttConnectionSettings
{
    public string BrokerAddress { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseTls { get; set; } = false;
    public int KeepAlivePeriodSeconds { get; set; } = 60;
    public int TimeoutSeconds { get; set; } = 30;
    public MqttQualityOfService QoS { get; set; } = MqttQualityOfService.AtLeastOnce;
    public bool CleanSession { get; set; } = true;
}

/// <summary>
/// MQTT subscription configuration
/// </summary>
public sealed class MqttSubscription
{
    public string Topic { get; set; } = string.Empty;
    public MqttQualityOfService QoS { get; set; } = MqttQualityOfService.AtLeastOnce;
    public int TagNumber { get; set; }
    public string? JsonPath { get; set; } // For extracting value from JSON payloads
    public PayloadFormat Format { get; set; } = PayloadFormat.PlainText;
}

/// <summary>
/// MQTT publish configuration
/// </summary>
public sealed class MqttPublishSettings
{
    public string Topic { get; set; } = string.Empty;
    public MqttQualityOfService QoS { get; set; } = MqttQualityOfService.AtLeastOnce;
    public bool Retain { get; set; } = false;
    public PayloadFormat Format { get; set; } = PayloadFormat.PlainText;
    public string? Template { get; set; } // For formatting outgoing messages
}

/// <summary>
/// MQTT template for device configuration
/// </summary>
public sealed class MqttDeviceTemplate
{
    public MqttConnectionSettings ConnectionSettings { get; set; } = new();
    public List<MqttSubscription> Subscriptions { get; set; } = new();
    public List<MqttPublishSettings> PublishSettings { get; set; } = new();
}

/// <summary>
/// MQTT Quality of Service levels
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MqttQualityOfService
{
    AtMostOnce = 0,      // Fire and forget
    AtLeastOnce = 1,     // Acknowledged delivery
    ExactlyOnce = 2      // Assured delivery
}

/// <summary>
/// Payload format types
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PayloadFormat
{
    PlainText,           // Raw text/number
    Json,                // JSON object
    Binary,              // Raw bytes
    Csv                  // Comma-separated values
}

/// <summary>
/// MQTT message received event
/// </summary>
public sealed record MqttMessageReceived
{
    public string Topic { get; init; } = string.Empty;
    public byte[] Payload { get; init; } = Array.Empty<byte>();
    public MqttQualityOfService QoS { get; init; }
    public bool Retain { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
