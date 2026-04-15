using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RapidScada.Application.Abstractions;

namespace RapidScada.Realtime.Hubs;

/// <summary>
/// SignalR Hub for real-time SCADA data updates
/// </summary>
[Authorize]
public sealed class ScadaHub : Hub
{
    private readonly ILogger<ScadaHub> _logger;
    private readonly ITagRepository _tagRepository;
    private static readonly Dictionary<string, HashSet<int>> _userSubscriptions = new();
    private static readonly object _lock = new();

    public ScadaHub(
        ILogger<ScadaHub> logger,
        ITagRepository tagRepository)
    {
        _logger = logger;
        _tagRepository = tagRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("Client connected: {ConnectionId}, User: {UserId}", Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        
        lock (_lock)
        {
            _userSubscriptions.Remove(connectionId);
        }

        _logger.LogInformation(
            "Client disconnected: {ConnectionId}, Reason: {Reason}",
            connectionId,
            exception?.Message ?? "Normal disconnect");

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to specific tag updates
    /// </summary>
    public async Task SubscribeToTags(int[] tagIds)
    {
        var connectionId = Context.ConnectionId;

        lock (_lock)
        {
            if (!_userSubscriptions.ContainsKey(connectionId))
            {
                _userSubscriptions[connectionId] = new HashSet<int>();
            }

            foreach (var tagId in tagIds)
            {
                _userSubscriptions[connectionId].Add(tagId);
            }
        }

        _logger.LogInformation(
            "Client {ConnectionId} subscribed to {Count} tags",
            connectionId,
            tagIds.Length);

        await Clients.Caller.SendAsync("SubscriptionConfirmed", tagIds);
    }

    /// <summary>
    /// Unsubscribe from specific tag updates
    /// </summary>
    public async Task UnsubscribeFromTags(int[] tagIds)
    {
        var connectionId = Context.ConnectionId;

        lock (_lock)
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                foreach (var tagId in tagIds)
                {
                    subscriptions.Remove(tagId);
                }
            }
        }

        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from {Count} tags",
            connectionId,
            tagIds.Length);

        await Clients.Caller.SendAsync("UnsubscriptionConfirmed", tagIds);
    }

    /// <summary>
    /// Get current subscriptions
    /// </summary>
    public Task<int[]> GetSubscriptions()
    {
        var connectionId = Context.ConnectionId;

        lock (_lock)
        {
            if (_userSubscriptions.TryGetValue(connectionId, out var subscriptions))
            {
                return Task.FromResult(subscriptions.ToArray());
            }
        }

        return Task.FromResult(Array.Empty<int>());
    }

    /// <summary>
    /// Subscribe to all device updates
    /// </summary>
    public async Task SubscribeToDevice(int deviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        
        _logger.LogInformation(
            "Client {ConnectionId} subscribed to device {DeviceId}",
            Context.ConnectionId,
            deviceId);

        await Clients.Caller.SendAsync("DeviceSubscriptionConfirmed", deviceId);
    }

    /// <summary>
    /// Unsubscribe from device updates
    /// </summary>
    public async Task UnsubscribeFromDevice(int deviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device_{deviceId}");
        
        _logger.LogInformation(
            "Client {ConnectionId} unsubscribed from device {DeviceId}",
            Context.ConnectionId,
            deviceId);

        await Clients.Caller.SendAsync("DeviceUnsubscriptionConfirmed", deviceId);
    }

    /// <summary>
    /// Send message to all connected clients (admin only)
    /// </summary>
    [Authorize(Roles = "Administrator")]
    public async Task BroadcastMessage(string message)
    {
        _logger.LogInformation(
            "Broadcasting message from {ConnectionId}: {Message}",
            Context.ConnectionId,
            message);

        await Clients.All.SendAsync("SystemMessage", new
        {
            timestamp = DateTime.UtcNow,
            message
        });
    }

    /// <summary>
    /// Get active subscriptions (for monitoring)
    /// </summary>
    public static Dictionary<string, int> GetActiveSubscriptionsCount()
    {
        lock (_lock)
        {
            return _userSubscriptions.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Count);
        }
    }
}

/// <summary>
/// DTO for tag value updates
/// </summary>
public sealed record TagValueUpdate
{
    public int TagId { get; init; }
    public double Value { get; init; }
    public double Quality { get; init; }
    public DateTime Timestamp { get; init; }
    public int DeviceId { get; init; }
}

/// <summary>
/// DTO for device status updates
/// </summary>
public sealed record DeviceStatusUpdate
{
    public int DeviceId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? Message { get; init; }
}

/// <summary>
/// DTO for alarm notifications
/// </summary>
public sealed record AlarmNotification
{
    public long AlarmId { get; init; }
    public int TagId { get; init; }
    public int DeviceId { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool Acknowledged { get; init; }
}
