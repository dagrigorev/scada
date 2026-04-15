using FluentAssertions;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using Xunit;

namespace RapidScada.Domain.Tests.Entities;

public sealed class TagTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var id = TagId.New();
        var number = 1;
        var name = "Temperature";
        var deviceId = DeviceId.Create(1);

        // Act
        var result = Tag.Create(id, number, name, deviceId, TagType.Real);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be(number);
        result.Value.Name.Should().Be(name);
        result.Value.TagType.Should().Be(TagType.Real);
        result.Value.Status.Should().Be(TagStatus.Undefined);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Act
        var result = Tag.Create(TagId.New(), 1, "", DeviceId.Create(1), TagType.Real);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNegativeNumber_ShouldFail()
    {
        // Act
        var result = Tag.Create(TagId.New(), -1, "Test", DeviceId.Create(1), TagType.Real);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void UpdateValue_WithValidValue_ShouldSucceed()
    {
        // Arrange
        var tag = CreateTestTag();
        var value = TagValue.Create(25.5, quality: 1.0);

        // Act
        var result = tag.UpdateValue(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        tag.CurrentValue.Should().Be(value);
        tag.Status.Should().Be(TagStatus.Valid);
        tag.LastUpdateAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateValue_BelowLowLimit_ShouldSetAlarmStatus()
    {
        // Arrange
        var tag = Tag.Create(
            TagId.New(),
            1,
            "Temperature",
            DeviceId.Create(1),
            TagType.Real,
            lowLimit: 10.0,
            highLimit: 50.0).Value;

        var value = TagValue.Create(5.0);

        // Act
        tag.UpdateValue(value);

        // Assert
        tag.Status.Should().Be(TagStatus.BelowLowLimit);
    }

    [Fact]
    public void UpdateValue_AboveHighLimit_ShouldSetAlarmStatus()
    {
        // Arrange
        var tag = Tag.Create(
            TagId.New(),
            1,
            "Temperature",
            DeviceId.Create(1),
            TagType.Real,
            lowLimit: 10.0,
            highLimit: 50.0).Value;

        var value = TagValue.Create(60.0);

        // Act
        tag.UpdateValue(value);

        // Assert
        tag.Status.Should().Be(TagStatus.AboveHighLimit);
    }

    [Fact]
    public void UpdateValue_WithinLimits_ShouldBeValid()
    {
        // Arrange
        var tag = Tag.Create(
            TagId.New(),
            1,
            "Temperature",
            DeviceId.Create(1),
            TagType.Real,
            lowLimit: 10.0,
            highLimit: 50.0).Value;

        var value = TagValue.Create(25.0);

        // Act
        tag.UpdateValue(value);

        // Assert
        tag.Status.Should().Be(TagStatus.Valid);
    }

    [Fact]
    public void MarkAsInvalid_ShouldUpdateStatus()
    {
        // Arrange
        var tag = CreateTestTag();
        tag.UpdateValue(TagValue.Create(25.0));

        // Act
        tag.MarkAsInvalid();

        // Assert
        tag.Status.Should().Be(TagStatus.Invalid);
        tag.LastUpdateAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsUnreliable_ShouldUpdateStatus()
    {
        // Arrange
        var tag = CreateTestTag();

        // Act
        tag.MarkAsUnreliable();

        // Assert
        tag.Status.Should().Be(TagStatus.Unreliable);
    }

    private static Tag CreateTestTag()
    {
        return Tag.Create(
            TagId.New(),
            1,
            "Test Tag",
            DeviceId.Create(1),
            TagType.Real).Value;
    }
}
