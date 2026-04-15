using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RapidScada.Notifications.Models;

namespace RapidScada.Notifications.Services;

public interface IEmailNotificationService
{
    Task SendAsync(EmailNotificationRequest request, CancellationToken cancellationToken = default);
}

public sealed class EmailNotificationService : IEmailNotificationService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly ITemplateService _templateService;

    public EmailNotificationService(
        IOptions<EmailOptions> options,
        ILogger<EmailNotificationService> logger,
        ITemplateService templateService)
    {
        _options = options.Value;
        _logger = logger;
        _templateService = templateService;
    }

    public async Task SendAsync(
        EmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));

            // Add recipients
            foreach (var to in request.To)
            {
                message.To.Add(MailboxAddress.Parse(to));
            }

            foreach (var cc in request.Cc)
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }

            foreach (var bcc in request.Bcc)
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            message.Subject = request.Subject;

            // Build body
            var bodyBuilder = new BodyBuilder();

            if (!string.IsNullOrEmpty(request.TemplateName))
            {
                // Use template
                var renderedBody = await _templateService.RenderAsync(
                    request.TemplateName,
                    request.Data,
                    cancellationToken);

                if (request.IsHtml)
                {
                    bodyBuilder.HtmlBody = renderedBody;
                }
                else
                {
                    bodyBuilder.TextBody = renderedBody;
                }
            }
            else
            {
                // Use plain message
                if (request.IsHtml)
                {
                    bodyBuilder.HtmlBody = request.Message;
                }
                else
                {
                    bodyBuilder.TextBody = request.Message;
                }
            }

            // Add attachments
            foreach (var attachment in request.Attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send email
            using var client = new SmtpClient();
            
            await client.ConnectAsync(
                _options.SmtpServer,
                _options.SmtpPort,
                _options.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);

            if (!string.IsNullOrEmpty(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {Count} recipients: {Subject}",
                request.To.Count,
                request.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email: {Subject}", request.Subject);
            throw;
        }
    }
}

public sealed class EmailOptions
{
    public const string Section = "Email";

    public string SmtpServer { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@rapidscada.com";
    public string FromName { get; set; } = "RapidScada";
    public int MaxRetries { get; set; } = 3;
}
