using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using RapidScada.Notifications.Services;

namespace RapidScada.Notifications;

public sealed class NotificationWorker : BackgroundService
{
    private readonly ILogger<NotificationWorker> _logger;
    private readonly NotificationOptions _options;

    public NotificationWorker(
        ILogger<NotificationWorker> logger,
        IOptions<NotificationOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Worker starting");

        // Hangfire handles background jobs automatically
        // This worker just keeps the service running

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Notification Worker stopped");
    }
}

public sealed class NotificationOptions
{
    public const string Section = "Notifications";

    public bool EnableEmail { get; set; } = true;
    public bool EnableSms { get; set; } = false;
    public bool EnablePush { get; set; } = false;
    public bool EnableWebhooks { get; set; } = true;
    public int DefaultRetryAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 5;
}

/// <summary>
/// Hangfire job for sending notifications
/// </summary>
public sealed class NotificationJobs
{
    private readonly IEmailNotificationService _emailService;
    private readonly ISmsNotificationService _smsService;
    private readonly ILogger<NotificationJobs> _logger;

    public NotificationJobs(
        IEmailNotificationService emailService,
        ISmsNotificationService smsService,
        ILogger<NotificationJobs> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
    }

    public async Task SendAlarmNotificationAsync(
        string deviceName,
        string tagName,
        double value,
        string severity,
        List<string> emailRecipients,
        List<string> smsRecipients)
    {
        _logger.LogInformation(
            "Sending alarm notification for {DeviceName}.{TagName}",
            deviceName,
            tagName);

        // Send email
        if (emailRecipients.Any())
        {
            var emailRequest = new Models.EmailNotificationRequest
            {
                To = emailRecipients,
                Subject = $"ALARM: {deviceName} - {tagName}",
                TemplateName = "alarm",
                Data = new Dictionary<string, object>
                {
                    ["subject"] = $"ALARM: {deviceName}",
                    ["message"] = $"Tag {tagName} triggered an alarm",
                    ["deviceName"] = deviceName,
                    ["tagName"] = tagName,
                    ["value"] = value,
                    ["severity"] = severity.ToLowerInvariant(),
                    ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                },
                Priority = severity.ToLower() == "critical"
                    ? Models.NotificationPriority.Critical
                    : Models.NotificationPriority.High
            };

            await _emailService.SendAsync(emailRequest);
        }

        // Send SMS
        if (smsRecipients.Any())
        {
            var smsRequest = new Models.SmsNotificationRequest
            {
                PhoneNumbers = smsRecipients,
                Message = $"ALARM: {deviceName} - {tagName} = {value}",
                Priority = Models.NotificationPriority.High
            };

            await _smsService.SendAsync(smsRequest);
        }
    }

    public async Task SendDailyReportAsync(
        List<string> recipients,
        int totalDevices,
        int onlineDevices,
        int totalAlarms)
    {
        _logger.LogInformation("Sending daily report to {Count} recipients", recipients.Count);

        var request = new Models.EmailNotificationRequest
        {
            To = recipients,
            Subject = $"Daily SCADA Report - {DateTime.UtcNow:yyyy-MM-dd}",
            TemplateName = "daily_report",
            Data = new Dictionary<string, object>
            {
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["totalDevices"] = totalDevices,
                ["onlineDevices"] = onlineDevices,
                ["totalAlarms"] = totalAlarms
            }
        };

        await _emailService.SendAsync(request);
    }
}
