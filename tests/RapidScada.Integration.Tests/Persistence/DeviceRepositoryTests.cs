using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using RapidScada.Persistence;
using RapidScada.Persistence.Repositories;
using Xunit;

namespace RapidScada.Integration.Tests.Persistence;

public sealed class DeviceRepositoryTests : IAsyncLifetime
{
    private ScadaDbContext _context = null!;
    private DeviceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ScadaDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ScadaDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _repository = new DeviceRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistDevice()
    {
        // Arrange
        var device = CreateTestDevice();

        // Act
        await _repository.AddAsync(device);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _repository.GetByIdAsync(device.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Value.Should().Be(device.Name.Value);
    }

    [Fact]
    public async Task GetByCommunicationLineAsync_ShouldReturnMatchingDevices()
    {
        // Arrange
        var lineId = CommunicationLineId.Create(1);
        var device1 = CreateTestDevice(lineId: lineId);
        var device2 = CreateTestDevice(lineId: lineId);
        var device3 = CreateTestDevice(lineId: CommunicationLineId.Create(2));

        await _repository.AddAsync(device1);
        await _repository.AddAsync(device2);
        await _repository.AddAsync(device3);
        await _context.SaveChangesAsync();

        // Act
        var devices = await _repository.GetByCommunicationLineAsync(lineId);

        // Assert
        devices.Should().HaveCount(2);
        devices.Should().AllSatisfy(d => d.CommunicationLineId.Should().Be(lineId));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnMatchingDevices()
    {
        // Arrange
        var device1 = CreateTestDevice();
        device1.UpdateCommunicationStatus(true);

        var device2 = CreateTestDevice();
        device2.UpdateCommunicationStatus(false);

        await _repository.AddAsync(device1);
        await _repository.AddAsync(device2);
        await _context.SaveChangesAsync();

        // Act
        var onlineDevices = await _repository.GetByStatusAsync(DeviceStatus.Online);

        // Assert
        onlineDevices.Should().HaveCount(1);
        onlineDevices.First().Status.Should().Be(DeviceStatus.Online);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        // Arrange
        var device = CreateTestDevice();
        await _repository.AddAsync(device);
        await _context.SaveChangesAsync();

        var newName = DeviceName.Create("Updated Name").Value;
        var newAddress = DeviceAddress.Create(99).Value;

        // Act
        device.UpdateConfiguration(newName, newAddress, null, "Updated");
        await _repository.UpdateAsync(device);
        await _context.SaveChangesAsync();

        // Clear tracking
        _context.ChangeTracker.Clear();

        // Assert
        var updated = await _repository.GetByIdAsync(device.Id);
        updated!.Name.Value.Should().Be("Updated Name");
        updated.Address.Value.Should().Be(99);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDevice()
    {
        // Arrange
        var device = CreateTestDevice();
        await _repository.AddAsync(device);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(device);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(device.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForExistingDevice()
    {
        // Arrange
        var device = CreateTestDevice();
        await _repository.AddAsync(device);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(device.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalseForNonExistentDevice()
    {
        // Act
        var exists = await _repository.ExistsAsync(DeviceId.Create(99999));

        // Assert
        exists.Should().BeFalse();
    }

    private static int _deviceCounter = 0;

    private static Device CreateTestDevice(CommunicationLineId? lineId = null)
    {
        var counter = Interlocked.Increment(ref _deviceCounter);
        return Device.Create(
            DeviceId.New(),
            DeviceName.Create($"Test Device {counter}").Value,
            DeviceTypeId.Create(1),
            DeviceAddress.Create(counter).Value,
            lineId ?? CommunicationLineId.Create(1)).Value;
    }
}
