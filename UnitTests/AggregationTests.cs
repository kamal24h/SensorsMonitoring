using Domain.Entities;
using Application.Services;
using Moq;
using Domain.Interfaces;

namespace UnitTests;

public class AggregationTests
{
    [Fact]
    public async Task GetAggregatedDataAsync_WithReadings_ShouldReturnCorrectBuckets()
    {
        // Arrange
        var mockRepo = new Mock<IReadSensorDataRepository>();
        var readings = new List<Reading>
        {
            new Reading("PUMP-01", "temperature",
                DateTime.Parse("2025-06-01T08:33:00Z"), 1, 67.21),
            new Reading("PUMP-01", "temperature",
                DateTime.Parse("2025-06-01T08:34:30Z"), 2, 68.5),
            new Reading("PUMP-01", "temperature",
                DateTime.Parse("2025-06-01T08:35:00Z"), 3, 67.8)
        };

        mockRepo.Setup(r => r.GetByDeviceAndMetricAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        var service = new ReadSensorDataService(mockRepo.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<ReadSensorDataService>>());

        var from = DateTime.Parse("2025-06-01T08:30:00Z");
        var to = DateTime.Parse("2025-06-01T09:00:00Z");
        var bucketSize = 60; // 1 minute

        // Act
        var result = await service.GetAggregatedDataAsync("PUMP-01", "temperature", from, to, bucketSize);

        // Assert
        Assert.Equal(3, result.Count());

        var first = result.First();
        Assert.Equal(DateTime.Parse("2025-06-01T08:33:00Z"), first.BucketStart);
        Assert.Equal(1, first.Count);
        Assert.Equal(67.21, first.Average);
        Assert.Equal(67.21, first.Minimum);
        Assert.Equal(67.21, first.Maximum);
    }
}