using Microsoft.EntityFrameworkCore;
using RapidScada.Application.Abstractions;
using RapidScada.Archiver;
using RapidScada.Archiver.Repositories;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/scada-archiver-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Configuration
builder.Services.Configure<ArchiverOptions>(
    builder.Configuration.GetSection(ArchiverOptions.Section));

// Database
builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("RapidScada.Persistence")));

// Repositories
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddSingleton<IHistoricalDataRepository>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    var logger = sp.GetRequiredService<ILogger<HistoricalDataRepository>>();
    return new HistoricalDataRepository(connectionString, logger);
});

// Background service
builder.Services.AddHostedService<ArchiverWorker>();

var host = builder.Build();

// Ensure TimescaleDB hypertables are created
using (var scope = host.Services.CreateScope())
{
    try
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
        await using var connection = new Npgsql.NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Create TimescaleDB extension if not exists
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE EXTENSION IF NOT EXISTS timescaledb";
        await cmd.ExecuteNonQueryAsync();

        Log.Information("TimescaleDB extension verified");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not enable TimescaleDB extension. Historical data will still work but without time-series optimizations.");
    }
}

await host.RunAsync();
