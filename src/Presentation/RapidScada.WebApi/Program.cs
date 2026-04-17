using Carter;
using Microsoft.EntityFrameworkCore;
using RapidScada.Application.Abstractions;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/scada-api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Rapid SCADA API",
        Version = "v8.0",
        Description = "Modern SCADA system REST API"
    });
});

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

// MediatR - Register all handlers from Application assembly
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RapidScada.Application.Commands.CreateDeviceCommand>();
    cfg.RegisterServicesFromAssemblyContaining<RapidScada.Application.Queries.GetAllDevicesQuery>();
});

// Carter for minimal APIs
builder.Services.AddCarter();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseHttpsRedirection();

// Map Carter endpoints
app.MapCarter();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "8.0.0"
}))
.WithTags("Health")
.WithOpenApi();

app.Run();
