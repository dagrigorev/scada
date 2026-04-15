using Hangfire;
using Hangfire.PostgreSql;
using RapidScada.Notifications;
using RapidScada.Notifications.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/notifications-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Configuration
builder.Services.Configure<NotificationOptions>(
    builder.Configuration.GetSection(NotificationOptions.Section));
builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection(EmailOptions.Section));
builder.Services.Configure<SmsOptions>(
    builder.Configuration.GetSection(SmsOptions.Section));

// Services
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<ISmsNotificationService, SmsNotificationService>();
builder.Services.AddScoped<NotificationJobs>();

// Hangfire for background job processing
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.ServerName = "RapidScada.Notifications";
});

// Background service
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();

// Configure recurring jobs (examples)
var recurringJobManager = host.Services.GetRequiredService<IRecurringJobManager>();

// Example: Daily report at 8 AM
recurringJobManager.AddOrUpdate<NotificationJobs>(
    "daily-report",
    job => job.SendDailyReportAsync(
        new List<string> { "admin@example.com" },
        0, 0, 0),
    "0 8 * * *", // Cron expression: 8 AM daily
    new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Local
    });

Log.Information("RapidScada Notifications Service starting");

await host.RunAsync();
