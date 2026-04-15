using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Drivers.Mqtt.Models;

namespace RapidScada.Drivers.Mqtt;

/// <summary>
/// MQTT protocol driver for IoT device communication
/// </summary>
public sealed class MqttDriver : DeviceDriverBase
{
    private readonly ILogger<MqttDriver> _logger;
    private IManagedMqttClient? _mqttClient;
    private MqttDeviceTemplate? _template;
    private readonly Dictionary<string, MqttSubscription> _topicSubscriptions = new();
    private readonly Dictionary<string, MqttMessageReceived> _lastMessages = new();
    private readonly object _lock = new();

    public MqttDriver(ILogger<MqttDriver> logger)
        : base("MQTT Driver", "1.0.0")
    {
        _logger = logger;
    }

    public override Task<Result> InitializeAsync(
        Device device,
        object? connectionSettings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (connectionSettings is null)
                return Task.FromResult(Result.Failure(Error.Validation("Connection settings are required")));

            var json = JsonSerializer.Serialize(connectionSettings);
            _template = JsonSerializer.Deserialize<MqttDeviceTemplate>(json);

            if (_template is null)
                return Task.FromResult(Result.Failure(Error.Validation("Invalid MQTT template")));

            // Map subscriptions to topics
            _topicSubscriptions.Clear();
            foreach (var sub in _template.Subscriptions)
            {
                _topicSubscriptions[sub.Topic] = sub;
            }

            _logger.LogInformation(
                "MQTT driver initialized for device {DeviceId} with {Count} subscriptions",
                device.Id,
                _template.Subscriptions.Count);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MQTT driver");
            return Task.FromResult(Result.Failure(Error.Failure($"Initialization failed: {ex.Message}")));
        }
    }

    public override async Task<Result> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_template is null)
            return Result.Failure(Error.Validation("Driver not initialized"));

        try
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateManagedMqttClient();

            // Configure options
            var clientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_template.ConnectionSettings.BrokerAddress, _template.ConnectionSettings.Port)
                .WithClientId(_template.ConnectionSettings.ClientId ?? Guid.NewGuid().ToString())
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(_template.ConnectionSettings.KeepAlivePeriodSeconds))
                .WithTimeout(TimeSpan.FromSeconds(_template.ConnectionSettings.TimeoutSeconds))
                .WithCleanSession(_template.ConnectionSettings.CleanSession);

            // Add credentials if provided
            if (!string.IsNullOrEmpty(_template.ConnectionSettings.Username))
            {
                clientOptions = clientOptions.WithCredentials(
                    _template.ConnectionSettings.Username,
                    _template.ConnectionSettings.Password);
            }

            // Add TLS if enabled
            if (_template.ConnectionSettings.UseTls)
            {
                clientOptions = clientOptions.WithTls();
            }

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithClientOptions(clientOptions.Build())
                .Build();

            // Setup message handler
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

            // Connect
            await _mqttClient.StartAsync(managedOptions);

            // Subscribe to topics
            var subscriptions = _template.Subscriptions
                .Select(s => new MqttTopicFilterBuilder()
                    .WithTopic(s.Topic)
                    .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)s.QoS)
                    .Build())
                .ToList();

            await _mqttClient.SubscribeAsync(subscriptions);

            _logger.LogInformation(
                "Connected to MQTT broker at {Broker}:{Port} with {Count} subscriptions",
                _template.ConnectionSettings.BrokerAddress,
                _template.ConnectionSettings.Port,
                subscriptions.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MQTT broker");
            return Result.Failure(Error.Failure($"Connection failed: {ex.Message}"));
        }
    }

    public override async Task DisconnectAsync()
    {
        if (_mqttClient is not null)
        {
            await _mqttClient.StopAsync();
            _mqttClient.Dispose();
            _mqttClient = null;
        }

        _logger.LogInformation("Disconnected from MQTT broker");
    }

    protected override Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        CancellationToken cancellationToken = default)
    {
        var readings = new List<TagReading>();

        lock (_lock)
        {
            foreach (var kvp in _lastMessages)
            {
                var topic = kvp.Key;
                var message = kvp.Value;

                if (!_topicSubscriptions.TryGetValue(topic, out var subscription))
                    continue;

                try
                {
                    var value = ParsePayload(message.Payload, subscription);
                    
                    readings.Add(new TagReading(
                        subscription.TagNumber,
                        value,
                        message.Timestamp,
                        Quality: 1.0));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Error parsing MQTT message from topic {Topic}",
                        topic);
                }
            }
        }

        return Task.FromResult(Result.Success<IReadOnlyList<TagReading>>(readings));
    }

    protected override Task<Result> OnWriteTagAsync(
        int tagNumber,
        object value,
        CancellationToken cancellationToken = default)
    {
        if (_mqttClient is null || _template is null)
            return Task.FromResult(Result.Failure(Error.Validation("Not connected")));

        // Find publish settings for this tag (you can enhance this with tag-to-topic mapping)
        var publishSettings = _template.PublishSettings.FirstOrDefault();
        if (publishSettings is null)
            return Task.FromResult(Result.Failure(Error.Validation("No publish settings configured")));

        try
        {
            var payload = FormatPayload(value, publishSettings);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(publishSettings.Topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)publishSettings.QoS)
                .WithRetainFlag(publishSettings.Retain)
                .Build();

            _mqttClient.EnqueueAsync(message);

            _logger.LogDebug(
                "Published value {Value} to topic {Topic}",
                value,
                publishSettings.Topic);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing MQTT message");
            return Task.FromResult(Result.Failure(Error.Failure($"Publish failed: {ex.Message}")));
        }
    }

    private Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var message = new MqttMessageReceived
            {
                Topic = topic,
                Payload = args.ApplicationMessage.PayloadSegment.ToArray(),
                QoS = (MqttQualityOfService)args.ApplicationMessage.QualityOfServiceLevel,
                Retain = args.ApplicationMessage.Retain,
                Timestamp = DateTime.UtcNow
            };

            lock (_lock)
            {
                _lastMessages[topic] = message;
            }

            _logger.LogDebug(
                "Received MQTT message from topic {Topic}, size {Size} bytes",
                topic,
                message.Payload.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling MQTT message");
        }

        return Task.CompletedTask;
    }

    private object ParsePayload(byte[] payload, MqttSubscription subscription)
    {
        var payloadString = Encoding.UTF8.GetString(payload);

        return subscription.Format switch
        {
            PayloadFormat.PlainText => ParsePlainText(payloadString),
            PayloadFormat.Json => ParseJson(payloadString, subscription.JsonPath),
            PayloadFormat.Binary => payload,
            PayloadFormat.Csv => ParseCsv(payloadString),
            _ => payloadString
        };
    }

    private object ParsePlainText(string payload)
    {
        // Try to parse as number
        if (double.TryParse(payload, out var number))
            return number;

        // Try to parse as boolean
        if (bool.TryParse(payload, out var boolean))
            return boolean ? 1.0 : 0.0;

        return payload;
    }

    private object ParseJson(string payload, string? jsonPath)
    {
        var doc = JsonDocument.Parse(payload);

        if (string.IsNullOrEmpty(jsonPath))
        {
            // Return the whole JSON as string
            return payload;
        }

        // Simple JSONPath support (e.g., "$.temperature" or "data.value")
        var element = doc.RootElement;
        var parts = jsonPath.TrimStart('$', '.').Split('.');

        foreach (var part in parts)
        {
            if (element.TryGetProperty(part, out var property))
            {
                element = property;
            }
            else
            {
                throw new InvalidOperationException($"JSON path '{jsonPath}' not found");
            }
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => 1.0,
            JsonValueKind.False => 0.0,
            _ => element.ToString()
        };
    }

    private object ParseCsv(string payload)
    {
        var values = payload.Split(',');
        if (values.Length > 0 && double.TryParse(values[0], out var firstValue))
        {
            return firstValue;
        }
        return payload;
    }

    private byte[] FormatPayload(object value, MqttPublishSettings settings)
    {
        var formatted = settings.Format switch
        {
            PayloadFormat.PlainText => value.ToString() ?? string.Empty,
            PayloadFormat.Json => JsonSerializer.Serialize(new { value, timestamp = DateTime.UtcNow }),
            PayloadFormat.Binary => Encoding.UTF8.GetBytes(value.ToString() ?? string.Empty),
            _ => value.ToString() ?? string.Empty
        };

        return formatted is byte[] bytes
            ? bytes
            : Encoding.UTF8.GetBytes(formatted.ToString() ?? string.Empty);
    }
}
