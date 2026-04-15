namespace RapidScada.Notifications.Models;

/// <summary>
/// Base notification request
/// </summary>
public abstract record NotificationRequest
{
    public string Subject { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Email notification request
/// </summary>
public sealed record EmailNotificationRequest : NotificationRequest
{
    public List<string> To { get; init; } = new();
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
    public List<EmailAttachment> Attachments { get; init; } = new();
    public bool IsHtml { get; init; } = true;
    public string? TemplateName { get; init; }
}

/// <summary>
/// SMS notification request
/// </summary>
public sealed record SmsNotificationRequest : NotificationRequest
{
    public List<string> PhoneNumbers { get; init; } = new();
}

/// <summary>
/// Push notification request
/// </summary>
public sealed record PushNotificationRequest : NotificationRequest
{
    public List<string> DeviceTokens { get; init; } = new();
    public string? Icon { get; init; }
    public string? Action { get; init; }
}

/// <summary>
/// Webhook notification request
/// </summary>
public sealed record WebhookNotificationRequest : NotificationRequest
{
    public string Url { get; init; } = string.Empty;
    public HttpMethod Method { get; init; } = HttpMethod.Post;
    public Dictionary<string, string> Headers { get; init; } = new();
}

/// <summary>
/// Email attachment
/// </summary>
public sealed record EmailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "application/octet-stream";
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Notification status
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Failed,
    Retry
}

/// <summary>
/// Notification record for tracking
/// </summary>
public sealed record NotificationRecord
{
    public long Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Recipient { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public NotificationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? SentAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
}
