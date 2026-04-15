using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RapidScada.Application.DTOs;
using RapidScada.Persistence;
using Xunit;

namespace RapidScada.Integration.Tests.Api;

public sealed class DeviceEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DeviceEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database
                services.RemoveAll(typeof(DbContextOptions<ScadaDbContext>));

                // Add in-memory database for testing
                services.AddDbContext<ScadaDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Ensure database is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ScadaDbContext>();
                db.Database.EnsureCreated();
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllDevices_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/devices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>();
        devices.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateDevice_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createDto = new CreateDeviceDto(
            Name: "Test Device",
            DeviceTypeId: 1,
            Address: 1,
            CommunicationLineId: 1,
            CallSign: null,
            Description: "Integration test device");

        // Act
        var response = await _client.PostAsJsonAsync("/api/devices", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var device = await response.Content.ReadFromJsonAsync<DeviceDto>();
        device.Should().NotBeNull();
        device!.Name.Should().Be("Test Device");
    }

    [Fact]
    public async Task GetDeviceById_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/devices/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateDevice_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateDeviceDto(
            Name: "", // Invalid empty name
            DeviceTypeId: 1,
            Address: 1,
            CommunicationLineId: 1);

        // Act
        var response = await _client.PostAsJsonAsync("/api/devices", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
