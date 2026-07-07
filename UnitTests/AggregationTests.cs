using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests;

public class AggregationTests
{
    private readonly Mock<IReadSensorDataRepository> _mockRepository;
    private readonly Mock<IAggregationStrategy> _mockStrategy;
    private readonly Mock<ILogger<ReadSensorDataService>> _mockLogger;
    private readonly ReadSensorDataService _service;

    public AggregationTests()
    {
        _mockRepository = new Mock<IReadSensorDataRepository>();
        _mockStrategy = new Mock<IAggregationStrategy>();
        _mockLogger = new Mock<ILogger<ReadSensorDataService>>();

        _service = new ReadSensorDataService(
            _mockRepository.Object,
            _mockStrategy.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithValidReadings_ShouldReturnAggregatedResults()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var from = DateTime.Parse("2025-06-01T08:00:00Z");
        var to = DateTime.Parse("2025-06-01T09:00:00Z");
        var bucketSize = 60;

        var readings = new List<Reading>
        {
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:33:00Z"), 1, 67.21),
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:34:30Z"), 2, 68.50),
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:35:00Z"), 3, 67.80),
            new("PUMP-01", "temperature", DateTime.Parse("2025-06-01T08:36:00Z"), 4, 69.10)
        };

        _mockRepository.Setup(r => r.GetByDeviceAndMetricAsync(deviceId, metric, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _service.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);

        // بررسی اینکه استراتژی برای هر گروه فراخوانی شده است
        _mockStrategy.Verify(
            s => s.Calculate(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Reading>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithNoReadings_ShouldReturnEmpty()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;
        var bucketSize = 60;

        _mockRepository.Setup(r => r.GetByDeviceAndMetricAsync(deviceId, metric, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Reading>());

        // Act
        var result = await _service.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSize);

        // Assert
        result.Should().BeEmpty();
        _mockStrategy.Verify(
            s => s.Calculate(It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Reading>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithInvalidParameters_ShouldThrowException()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow.AddHours(-1); // from > to
        var bucketSize = 60;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSize));
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithZeroBucketSize_ShouldThrowException()
    {
        // Arrange
        var deviceId = "PUMP-01";
        var metric = "temperature";
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var bucketSize = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSize));
    }

    [Fact]
    public async Task GetAggregatedDataAsync_WithEmptyDeviceId_ShouldThrowException()
    {
        // Arrange
        var deviceId = "";
        var metric = "temperature";
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var bucketSize = 60;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetAggregatedDataAsync(deviceId, metric, from, to, bucketSize));
    }
}