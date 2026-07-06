using Xunit;
using Domain.Entities;

namespace UnitTests;

public class DeduplicationTests
{
    [Fact]
    public void TryAddReading_WithDuplicate_ShouldNotAdd()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);

        // Act
        var result1 = processed.TryAddReading(reading1);
        var result2 = processed.TryAddReading(reading2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.Equal(2, processed.TotalLines);
        Assert.Equal(1, processed.DuplicatesRemoved);
        Assert.Single(processed.Readings);
    }

    [Fact]
    public void TryAddReading_WithDifferentSequence_ShouldAdd()
    {
        // Arrange
        var processed = new ProcessedReadings();
        var reading1 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1199, 67.21);
        var reading2 = new Reading("PUMP-01", "temperature",
            DateTime.Parse("2025-06-01T08:33:00Z"), 1200, 67.21);

        // Act
        var result1 = processed.TryAddReading(reading1);
        var result2 = processed.TryAddReading(reading2);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(2, processed.TotalLines);
        Assert.Equal(0, processed.DuplicatesRemoved);
        Assert.Equal(2, processed.Readings.Count);
    }
}