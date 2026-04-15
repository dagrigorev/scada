using Microsoft.Extensions.Options;
using RapidScada.Notifications.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace RapidScada.Notifications.Services;

public interface ISmsNotificationService
{
    Task SendAsync(SmsNotificationRequest request, CancellationToken cancellationToken = default);
}

public sealed class SmsNotificationService : ISmsNotificationService
{
    private readonly SmsOptions _options;
    private readonly ILogger<SmsNotificationService> _logger;

    public SmsNotificationService(
        IOptions<SmsOptions> options,
        ILogger<SmsNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrEmpty(_options.AccountSid) && !string.IsNullOrEmpty(_options.AuthToken))
        {
            TwilioClient.Init(_options.AccountSid, _options.AuthToken);
        }
    }

    public async Task SendAsync(
        SmsNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("SMS notifications are disabled");
            return;
        }

        if (string.IsNullOrEmpty(_options.AccountSid) || string.IsNullOrEmpty(_options.AuthToken))
        {
            throw new InvalidOperationException("Twilio credentials not configured");
        }

        try
        {
            foreach (var phoneNumber in request.PhoneNumbers)
            {
                var message = await MessageResource.CreateAsync(
                    to: new PhoneNumber(phoneNumber),
                    from: new PhoneNumber(_options.FromPhoneNumber),
                    body: request.Message);

                _logger.LogInformation(
                    "SMS sent to {PhoneNumber}, SID: {MessageSid}",
                    phoneNumber,
                    message.Sid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS");
            throw;
        }
    }
}

public sealed class SmsOptions
{
    public const string Section = "Sms";

    public bool Enabled { get; set; } = false;
    public string? AccountSid { get; set; }
    public string? AuthToken { get; set; }
    public string FromPhoneNumber { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
}
