using FluentAssertions;
using RapidScada.Domain.ValueObjects;
using Xunit;

namespace RapidScada.Domain.Tests.ValueObjects;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("Valid Device Name")]
    [InlineData("A")]
    [InlineData("Device-123")]
    public void DeviceName_Create_WithValidName_ShouldSucceed(string name)
    {
        // Act
        var result = DeviceName.Create(name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void DeviceName_Create_WithInvalidName_ShouldFail(string? name)
    {
        // Act
        var result = DeviceName.Create(name!);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DeviceName_Create_WithTooLongName_ShouldFail()
    {
        // Arrange
        var longName = new string('A', DeviceName.MaxLength + 1);

        // Act
        var result = DeviceName.Create(longName);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(65535)]
    public void DeviceAddress_Create_WithValidAddress_ShouldSucceed(int address)
    {
        // Act
        var result = DeviceAddress.Create(address);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(address);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(100000)]
    public void DeviceAddress_Create_WithInvalidAddress_ShouldFail(int address)
    {
        // Act
        var result = DeviceAddress.Create(address);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void CallSign_Create_WithValidValue_ShouldSucceed()
    {
        // Act
        var result = CallSign.Create("ALPHA-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("ALPHA-1");
    }

    [Fact]
    public void CallSign_Create_WithEmptyValue_ShouldFail()
    {
        // Act
        var result = CallSign.Create("");

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void TcpClientSettings_Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = TcpClientSettings.Create("192.168.1.100", 502, 5000, true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Host.Should().Be("192.168.1.100");
        result.Value.Port.Should().Be(502);
        result.Value.TimeoutMs.Should().Be(5000);
        result.Value.UseKeepAlive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", 502)]
    [InlineData("host", 0)]
    [InlineData("host", 65536)]
    [InlineData("host", -1)]
    public void TcpClientSettings_Create_WithInvalidParameters_ShouldFail(string host, int port)
    {
        // Act
        var result = TcpClientSettings.Create(host, port);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SerialPortSettings_Create_WithValidParameters_ShouldSucceed()
    {
        // Act
        var result = SerialPortSettings.Create("COM1", 9600, 8, "None", "One", 1000);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PortName.Should().Be("COM1");
        result.Value.BaudRate.Should().Be(9600);
    }

    [Fact]
    public void TagValue_Create_ShouldSetTimestampAndQuality()
    {
        // Arrange
        var value = 42.5;
        var quality = 0.95;

        // Act
        var tagValue = TagValue.Create(value, quality);

        // Assert
        tagValue.Value.Should().Be(value);
        tagValue.Quality.Should().Be(quality);
        tagValue.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TagValue_GetNumericValue_ShouldConvertSuccessfully()
    {
        // Arrange
        var tagValue = TagValue.Create(25.5);

        // Act
        var success = tagValue.TryGetNumericValue(out var numericValue);

        // Assert
        success.Should().BeTrue();
        numericValue.Should().Be(25.5);
    }

    [Fact]
    public void TagValue_Equality_SameCatalogValues_ShouldBeEqual()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var value1 = TagValue.Create(42, timestamp, 1.0);
        var value2 = TagValue.Create(42, timestamp, 1.0);

        // Act & Assert
        value1.Should().Be(value2);
    }
}
