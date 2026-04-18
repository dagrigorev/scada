using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RapidScada.Application.Abstractions;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using RapidScada.Realtime.Hubs;
using RapidScada.Realtime.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/realtime-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
builder.Services.Configure<RealtimeOptions>(
    builder.Configuration.GetSection(RealtimeOptions.Section));

var realtimeOptions = builder.Configuration.GetSection(RealtimeOptions.Section).Get<RealtimeOptions>()!;

// Add authentication (same JWT as Identity service)
var jwtKey = builder.Configuration["Jwt:SecretKey"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Allow JWT tokens in SignalR connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/scadahub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Database
builder.Services.AddDbContext<ScadaDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("RapidScada.Persistence")));

// Repositories
builder.Services.AddScoped<ITagRepository, TagRepository>();

// SignalR
var signalrBuilder = builder.Services.AddSignalR();

// Add Redis backplane if enabled
if (realtimeOptions.UseRedisBackplane && !string.IsNullOrEmpty(realtimeOptions.RedisConnectionString))
{
    signalrBuilder.AddStackExchangeRedis(realtimeOptions.RedisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = "RapidScada";
    });
    
    Log.Information("SignalR configured with Redis backplane");
}

// Background service for broadcasting
builder.Services.AddHostedService<RealtimeBroadcastService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // React/frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map SignalR hub
app.MapHub<ScadaHub>("/scadahub");

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "realtime",
    connections = ScadaHub.GetActiveSubscriptionsCount().Count
}));

// Metrics endpoint
app.MapGet("/metrics", () => Results.Ok(new
{
    activeConnections = ScadaHub.GetActiveSubscriptionsCount().Count,
    subscriptions = ScadaHub.GetActiveSubscriptionsCount()
}));

Log.Information("RapidScada Realtime Service starting on {Url}", app.Urls.FirstOrDefault() ?? "http://localhost:5005");

await app.RunAsync();
