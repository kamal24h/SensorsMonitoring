using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UnitTests;

public class DefaultAggregationStrategyTests
{
    private readonly DefaultAggregationStrategy _strategy = new();

    [Fact]
    public void Calculate_WithValidReadings_ShouldReturnCorrectResult()
    {
        // Arrange
        var bucketStart = DateTime.Parse("2025-06-01T08:30:00Z");
        var readings = new List<Reading>
        {
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:33:00Z"), 1, 10.5),
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:34:00Z"), 2, 20.3),
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:35:00Z"), 3, 15.7)
        };

        // Act
        var result = _strategy.Calculate(bucketStart, readings);

        // Assert
        result.BucketStart.Should().Be(bucketStart);
        result.Count.Should().Be(3);
        result.Average.Should().Be(Math.Round((10.5 + 20.3 + 15.7) / 3, 2));
        result.Minimum.Should().Be(10.5);
        result.Maximum.Should().Be(20.3);
    }

    [Fact]
    public void Calculate_WithEmptyReadings_ShouldThrowException()
    {
        // Arrange
        var bucketStart = DateTime.UtcNow;
        var emptyReadings = new List<Reading>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _strategy.Calculate(bucketStart, emptyReadings));
    }

    [Fact]
    public void Calculate_WithNullReadings_ShouldThrowException()
    {
        // Arrange
        var bucketStart = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _strategy.Calculate(bucketStart, null!));
    }
}