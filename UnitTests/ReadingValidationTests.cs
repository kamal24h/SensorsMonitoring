using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class ReadingValidationTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateReading()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;
        var value = 67.21;

        // Act
        var reading = new Reading(deviceId, metric, timestamp, sequence, value);

        // Assert
        reading.DeviceId.Should().Be(deviceId);
        reading.Metric.Should().Be(metric);
        reading.Timestamp.Should().Be(timestamp);
        reading.Sequence.Should().Be(sequence);
        reading.Value.Should().Be(value);
        reading.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_WithEmptyDeviceId_ShouldThrowException()
    {
        // Arrange
        var deviceId = "";
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;
        var value = 67.21;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId, metric, timestamp, sequence, value));
    }

    [Fact]
    public void Constructor_WithNullDeviceId_ShouldThrowException()
    {
        // Arrange
        string? deviceId = null;
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;
        var value = 67.21;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId!, metric, timestamp, sequence, value));
    }

    [Fact]
    public void Constructor_WithEmptyMetric_ShouldThrowException()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;
        var value = 67.21;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId, metric, timestamp, sequence, value));
    }

    [Fact]
    public void Constructor_WithNegativeSequence_ShouldThrowException()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = -1;
        var value = 67.21;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId, metric, timestamp, sequence, value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WithInvalidValue_ShouldThrowException(double invalidValue)
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId, metric, timestamp, sequence, invalidValue));
    }

    [Theory]
    [InlineData(-1001)]
    [InlineData(1001)]
    public void Constructor_WithOutOfRangeValue_ShouldThrowException(double outOfRangeValue)
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var timestamp = DateTime.UtcNow;
        var sequence = 1199;

        // Act & Assert
        Assert.Throws<InvalidReadingException>(() =>
            new Reading(deviceId, metric, timestamp, sequence, outOfRangeValue));
    }

    [Fact]
    public void ReadingIdentity_ShouldBeCorrect()
    {
        // Arrange
        var reading = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);

        // Act
        var identity = reading.Id;

        // Assert
        identity.DeviceId.Should().Be("PUMP-01");
        identity.Metric.Should().Be("temperature");
        identity.Timestamp.Should().Be(DateTime.Parse("2025-06-01T08:33:00Z"));
        identity.Sequence.Should().Be(1199);
    }

    [Fact]
    public void TwoReadingsWithSameIdentity_ShouldBeEqual()
    {
        // Arrange
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 68.50);

        // Act & Assert
        reading1.Id.Should().Be(reading2.Id);
        reading1.Value.Should().NotBe(reading2.Value);
    }
}