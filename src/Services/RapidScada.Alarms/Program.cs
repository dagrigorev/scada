using Microsoft.EntityFrameworkCore;
using RapidScada.Alarms;
using RapidScada.Alarms.Engine;
using RapidScada.Alarms.Services;
using RapidScada.Application.Abstractions;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/alarms-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Configuration
builder.Services.Configure<AlarmOptions>(
    builder.Configuration.GetSection(AlarmOptions.Section));

// Database
builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("RapidScada.Persistence")));

// Repositories
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddSingleton<IAlarmRepository>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    var logger = sp.GetRequiredService<ILogger<AlarmRepository>>();
    return new AlarmRepository(connectionString, logger);
});

// Services
builder.Services.AddSingleton<AlarmEvaluationEngine>();

// Background service
builder.Services.AddHostedService<AlarmMonitoringWorker>();

var host = builder.Build();

// Ensure database tables exist
using (var scope = host.Services.CreateScope())
{
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create alarm tables if they don't exist
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS alarm_rules (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                tag_id INTEGER NOT NULL,
                enabled BOOLEAN NOT NULL DEFAULT true,
                severity INTEGER NOT NULL,
                priority INTEGER NOT NULL DEFAULT 5,
                condition_data JSONB NOT NULL,
                actions_data JSONB,
                escalation_data JSONB,
                metadata JSONB,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS alarms (
                id BIGSERIAL PRIMARY KEY,
                tag_id INTEGER NOT NULL,
                device_id INTEGER NOT NULL,
                rule_id TEXT NOT NULL,
                severity INTEGER NOT NULL,
                state TEXT NOT NULL,
                message TEXT NOT NULL,
                trigger_value DOUBLE PRECISION NOT NULL,
                triggered_at TIMESTAMPTZ NOT NULL,
                acknowledged_at TIMESTAMPTZ,
                acknowledged_by TEXT,
                cleared_at TIMESTAMPTZ,
                clear_reason TEXT,
                escalation_level INTEGER NOT NULL DEFAULT 0,
                last_escalated_at TIMESTAMPTZ,
                metadata JSONB,
                FOREIGN KEY (rule_id) REFERENCES alarm_rules(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_alarms_state ON alarms(state);
            CREATE INDEX IF NOT EXISTS idx_alarms_device ON alarms(device_id);
            CREATE INDEX IF NOT EXISTS idx_alarms_triggered ON alarms(triggered_at DESC);
            CREATE INDEX IF NOT EXISTS idx_alarms_severity ON alarms(severity DESC);
            CREATE INDEX IF NOT EXISTS idx_alarm_rules_tag ON alarm_rules(tag_id);
        ";

        await cmd.ExecuteNonQueryAsync();
        
        Log.Information("Alarm database tables verified");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error creating alarm tables");
    }
}

Log.Information("RapidScada Alarms Service starting");

await host.RunAsync();
