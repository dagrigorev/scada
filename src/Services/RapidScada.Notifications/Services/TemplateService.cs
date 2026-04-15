using HandlebarsDotNet;

namespace RapidScada.Notifications.Services;

public interface ITemplateService
{
    Task<string> RenderAsync(string templateName, Dictionary<string, object> data, CancellationToken cancellationToken = default);
    void RegisterTemplate(string name, string template);
}

public sealed class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates = new();
    private readonly string _templateDirectory;

    public TemplateService(ILogger<TemplateService> logger, string templateDirectory = "Templates")
    {
        _logger = logger;
        _templateDirectory = templateDirectory;
        LoadDefaultTemplates();
    }

    public Task<string> RenderAsync(
        string templateName,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default)
    {
        if (!_compiledTemplates.TryGetValue(templateName, out var template))
        {
            // Try to load from file
            var templatePath = Path.Combine(_templateDirectory, $"{templateName}.hbs");
            if (File.Exists(templatePath))
            {
                var templateContent = File.ReadAllText(templatePath);
                RegisterTemplate(templateName, templateContent);
                template = _compiledTemplates[templateName];
            }
            else
            {
                throw new FileNotFoundException($"Template '{templateName}' not found");
            }
        }

        var result = template(data);
        return Task.FromResult(result);
    }

    public void RegisterTemplate(string name, string template)
    {
        var compiled = Handlebars.Compile(template);
        _compiledTemplates[name] = compiled;
        _logger.LogInformation("Registered template: {TemplateName}", name);
    }

    private void LoadDefaultTemplates()
    {
        // Alarm notification template
        RegisterTemplate("alarm", @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .alarm { border-left: 4px solid #d32f2f; padding: 15px; background: #ffebee; }
        .alarm.critical { border-color: #b71c1c; background: #ffcdd2; }
        .alarm.high { border-color: #f57c00; background: #ffe0b2; }
        .info { color: #666; font-size: 12px; margin-top: 10px; }
    </style>
</head>
<body>
    <div class='alarm {{severity}}'>
        <h2>🚨 {{subject}}</h2>
        <p><strong>Message:</strong> {{message}}</p>
        <p><strong>Device:</strong> {{deviceName}}</p>
        <p><strong>Tag:</strong> {{tagName}} = {{value}}</p>
        <p><strong>Time:</strong> {{timestamp}}</p>
        <div class='info'>
            This is an automated notification from RapidScada.
        </div>
    </div>
</body>
</html>");

        // Device status change template
        RegisterTemplate("device_status", @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .status { padding: 15px; border-radius: 4px; }
        .status.online { background: #e8f5e9; border-left: 4px solid #4caf50; }
        .status.offline { background: #ffebee; border-left: 4px solid #f44336; }
    </style>
</head>
<body>
    <div class='status {{status}}'>
        <h2>{{subject}}</h2>
        <p>{{message}}</p>
        <p><strong>Device:</strong> {{deviceName}}</p>
        <p><strong>Status:</strong> {{status}}</p>
        <p><strong>Time:</strong> {{timestamp}}</p>
    </div>
</body>
</html>");

        // Daily report template
        RegisterTemplate("daily_report", @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #2196f3; color: white; }
    </style>
</head>
<body>
    <h1>Daily SCADA Report</h1>
    <p><strong>Date:</strong> {{date}}</p>
    
    <h2>Summary</h2>
    <table>
        <tr>
            <th>Metric</th>
            <th>Value</th>
        </tr>
        <tr>
            <td>Total Devices</td>
            <td>{{totalDevices}}</td>
        </tr>
        <tr>
            <td>Online Devices</td>
            <td>{{onlineDevices}}</td>
        </tr>
        <tr>
            <td>Total Alarms</td>
            <td>{{totalAlarms}}</td>
        </tr>
    </table>
</body>
</html>");
    }
}
