using Microsoft.EntityFrameworkCore;
using RapidScada.Application.Abstractions;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using RapidScada.Server;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/scada-server-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddSerilog();

// Database
builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("RapidScada.Persistence")));

// Repositories
builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<ICommunicationLineRepository, CommunicationLineRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Device Driver Factory (would be implemented)
// builder.Services.AddSingleton<IDeviceDriverFactory, DeviceDriverFactory>();

// Background service
builder.Services.AddHostedService<ServerWorker>();

var host = builder.Build();

// Apply migrations on startup
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
