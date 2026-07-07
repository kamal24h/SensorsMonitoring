using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SensorMonitoring.UnitTests;

public class DeduplicationTests
{
    [Fact]
    public void Register_WithNewReading_ShouldAddAndReturnTrue()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);

        // Act
        var result = processed.Register(reading);

        // Assert
        result.Should().BeTrue();
        processed.StoredReadings.Should().Be(1);
        processed.TotalLines.Should().Be(0); // Register خطوط را شمارش نمی‌کند
        processed.DuplicatesRemoved.Should().Be(0);
    }

    [Fact]
    public void Register_WithDuplicateReading_ShouldNotAddAndReturnFalse()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 68.50);

        // Act
        var result1 = processed.Register(reading1);
        var result2 = processed.Register(reading2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
        processed.StoredReadings.Should().Be(1);
        processed.DuplicatesRemoved.Should().Be(1);
    }

    [Fact]
    public void Register_WithDifferentSequence_ShouldAddBoth()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1200, 68.50);

        // Act
        var result1 = processed.Register(reading1);
        var result2 = processed.Register(reading2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        processed.StoredReadings.Should().Be(2);
        processed.DuplicatesRemoved.Should().Be(0);
    }

    [Fact]
    public void Register_WithDifferentDevice_ShouldAddBoth()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-02", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 68.50);

        // Act
        var result1 = processed.Register(reading1);
        var result2 = processed.Register(reading2);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        processed.StoredReadings.Should().Be(2);
        processed.DuplicatesRemoved.Should().Be(0);
    }

    [Fact]
    public void IncrementLine_ShouldIncreaseTotalLines()
    {
        // Arrange
        var processed = new ProcessedReadings();

        // Act
        processed.IncrementLine();
        processed.IncrementLine();
        processed.IncrementLine();

        // Assert
        processed.TotalLines.Should().Be(3);
    }

    [Fact]
    public void IncrementInvalid_ShouldIncreaseInvalidRecords()
    {
        // Arrange
        var processed = new ProcessedReadings();

        // Act
        processed.IncrementInvalid();
        processed.IncrementInvalid();

        // Assert
        processed.InvalidRecords.Should().Be(2);
    }
}