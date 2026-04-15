using FluentAssertions;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using Xunit;

namespace RapidScada.Domain.Tests.Entities;

public sealed class DeviceTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var id = DeviceId.New();
        var name = DeviceName.Create("Temperature Sensor").Value;
        var typeId = DeviceTypeId.Create(1);
        var address = DeviceAddress.Create(1).Value;
        var lineId = CommunicationLineId.Create(1);

        // Act
        var result = Device.Create(id, name, typeId, address, lineId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Address.Should().Be(address);
        result.Value.Status.Should().Be(DeviceStatus.Offline);
        result.Value.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdateConfiguration_ShouldRaiseDomainEvent()
    {
        // Arrange
        var device = CreateTestDevice();
        var newName = DeviceName.Create("Updated Sensor").Value;
        var newAddress = DeviceAddress.Create(2).Value;

        // Act
        device.UpdateConfiguration(newName, newAddress, null, "Updated");

        // Assert
        device.Name.Should().Be(newName);
        device.Address.Should().Be(newAddress);
        device.DomainEvents.Should().HaveCount(2); // Created + Updated events
    }

    [Fact]
    public void UpdateCommunicationStatus_FromOfflineToOnline_ShouldRaiseEvent()
    {
        // Arrange
        var device = CreateTestDevice();
        device.ClearDomainEvents();

        // Act
        device.UpdateCommunicationStatus(success: true);

        // Assert
        device.Status.Should().Be(DeviceStatus.Online);
        device.LastCommunicationAt.Should().NotBeNull();
        device.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void SetErrorState_ShouldUpdateStatusAndRaiseEvent()
    {
        // Arrange
        var device = CreateTestDevice();
        device.ClearDomainEvents();

        // Act
        device.SetErrorState("Connection timeout");

        // Assert
        device.Status.Should().Be(DeviceStatus.Error);
        device.LastCommunicationAt.Should().NotBeNull();
        device.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void AddTag_WithUniqueNumber_ShouldSucceed()
    {
        // Arrange
        var device = CreateTestDevice();
        var tag = CreateTestTag(device.Id, tagNumber: 1);

        // Act
        var result = device.AddTag(tag);

        // Assert
        result.IsSuccess.Should().BeTrue();
        device.Tags.Should().ContainSingle();
    }

    [Fact]
    public void AddTag_WithDuplicateNumber_ShouldFail()
    {
        // Arrange
        var device = CreateTestDevice();
        var tag1 = CreateTestTag(device.Id, tagNumber: 1);
        var tag2 = CreateTestTag(device.Id, tagNumber: 1);
        device.AddTag(tag1);

        // Act
        var result = device.AddTag(tag2);

        // Assert
        result.IsFailure.Should().BeTrue();
        device.Tags.Should().ContainSingle();
    }

    private static Device CreateTestDevice()
    {
        return Device.Create(
            DeviceId.New(),
            DeviceName.Create("Test Device").Value,
            DeviceTypeId.Create(1),
            DeviceAddress.Create(1).Value,
            CommunicationLineId.Create(1)).Value;
    }

    private static Tag CreateTestTag(DeviceId deviceId, int tagNumber)
    {
        return Tag.Create(
            TagId.New(),
            tagNumber,
            $"Tag {tagNumber}",
            deviceId,
            TagType.Real).Value;
    }
}
