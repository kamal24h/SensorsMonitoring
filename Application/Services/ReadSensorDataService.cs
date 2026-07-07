using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public sealed class ReadSensorDataService : IReadSensorDataService
    {
        private readonly IReadSensorDataRepository repository;
        private readonly IAggregationStrategy aggregationStrategy;
        private readonly ILogger<ReadSensorDataService> logger;

        public ReadSensorDataService(IReadSensorDataRepository repository,
            IAggregationStrategy aggregationStrategy,
            ILogger<ReadSensorDataService> logger)
        {
            this.repository = repository;
            this.aggregationStrategy = aggregationStrategy;
            this.logger = logger;
        }

        public async Task<ProcessedStatsDto> ProcessFileAsync( string filePath, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("========== STARTING FILE PROCESSING ==========");

            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            logger.LogInformation( "Processing file {File}", filePath);

            var session = new ProcessedReadings();

            try
            {
                var lineIndex = 0;
                foreach (var line in File.ReadLines(filePath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    lineIndex++;
                    session.IncrementLine();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        session.IncrementInvalid();
                        continue;
                    }

                    await ProcessLineAsync(line, session, lineIndex, cancellationToken);
                }

                await repository.SaveProcessingStatsAsync(
                    new ProcessedStats(
                        session.TotalLines,
                        session.StoredReadings,
                        session.DuplicatesRemoved,
                        session.InvalidRecords),
                    cancellationToken);

                await repository.SaveChangesAsync(cancellationToken);
                                
                logger.LogInformation("========== FILE PROCESSING COMPLETED ==========");
                logger.LogInformation("Statistics:");
                logger.LogInformation("  Total Lines: {TotalLines}", session.TotalLines);
                logger.LogInformation("  Readings Stored: {Stored}", session.StoredReadings);
                logger.LogInformation("  Duplicates Removed: {Duplicates}", session.DuplicatesRemoved);
                logger.LogInformation("  Invalid Records: {Invalid}", session.InvalidRecords);
                logger.LogInformation("===============================================");

                return new ProcessedStatsDto
                {
                    TotalLines = session.TotalLines,
                    ReadingsStored = session.StoredReadings,
                    DuplicatesRemoved = session.DuplicatesRemoved,
                    InvalidRecords = session.InvalidRecords
                };

            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Import canceled.");
                throw;
            }
        }

        private async Task ProcessLineAsync( string line, ProcessedReadings session, int lineIndex, CancellationToken cancellationToken)
        {
            try
            {
                var dto = JsonSerializer.Deserialize<ReadingDto>(line);

                if (dto is null)
                {
                    session.IncrementInvalid();
                    return;
                }

                var reading = new Reading( dto.DeviceId, dto.Metric, dto.Timestamp, dto.Sequence, dto.Value);

                if (!session.Register(reading))
                    return;

                await repository.AddAsync(reading, cancellationToken);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Line {lineIndex}: Invalid json!", lineIndex);
                session.IncrementInvalid();
            }
            catch (InvalidReadingException ex)
            {
                logger.LogWarning( ex, "Line {lineIndex}: Invalid reading!", lineIndex);
                session.IncrementInvalid();
            }
        }

        public async Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync(string deviceId, string metric,
            DateTime from, DateTime to, int bucketSizeSeconds, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);
            ArgumentException.ThrowIfNullOrWhiteSpace(metric);

            if (from >= to)
                throw new ArgumentException("'from' must be before 'to'.");

            if (bucketSizeSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(bucketSizeSeconds), "Bucket size must be greater than zero.");

            logger.LogInformation("Aggregating readings for Device:{DeviceId}, Metric:{Metric}", deviceId, metric);

            var readings = (await repository.GetByDeviceAndMetricAsync(deviceId, metric, from, to, cancellationToken)).ToList();

            if (readings.Count == 0)
            {
                logger.LogInformation("No readings found.");
                return Enumerable.Empty<AggregationResultDto>();
            }

            var result = readings
                .GroupBy(r => GetBucketStart(r.Timestamp, from, bucketSizeSeconds))
                .OrderBy(g => g.Key)
                .Select(g =>
                    aggregationStrategy.Calculate(
                        g.Key,
                        g.ToList()))
                .ToList();

            logger.LogInformation("Aggregation completed. Buckets:{BucketCount}", result.Count);

            return result;
        }

        public async Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync1(string deviceId, string metric, DateTime from, DateTime to,
            int bucketSizeSeconds, CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Getting aggregated data for Device: {DeviceId}, Metric: {Metric}",
                deviceId, metric);
            logger.LogDebug("Time range: {From} to {To}, Bucket size: {BucketSize}s",
                from, to, bucketSizeSeconds);

            // اعتبارسنجی
            if (string.IsNullOrEmpty(deviceId))
                throw new ArgumentException("DeviceId cannot be null or empty", nameof(deviceId));

            if (string.IsNullOrEmpty(metric))
                throw new ArgumentException("Metric cannot be null or empty", nameof(metric));

            if (from >= to)
                throw new ArgumentException("'from' must be before 'to'");

            if (bucketSizeSeconds <= 0)
                throw new ArgumentException("Bucket size must be greater than 0");

            var readings = await repository.GetByDeviceAndMetricAsync(deviceId, metric, from, to, cancellationToken);

            if (!readings.Any())
            {
                logger.LogInformation("No readings found for the specified criteria");
                return [];
            }

            logger.LogInformation("Found {Count} readings for aggregation", readings.Count());

            // گروه‌بندی بر اساس بازه زمانی
            var grouped = readings
                .GroupBy(r => GetBucketStart(r.Timestamp, from, bucketSizeSeconds))
                .OrderBy(g => g.Key)
                .Select(g => AggregationCalculator.Calculate(g.Key, g))
                .ToList();

            logger.LogInformation("Aggregation completed with {BucketCount} buckets", grouped.Count);
            return grouped;
        }

        private static DateTime GetBucketStart(DateTime timestamp, DateTime from, int bucketSizeSeconds)
        {
            var bucket = TimeSpan.FromSeconds(bucketSizeSeconds);
            var bucketIndex = (timestamp - from).Ticks / bucket.Ticks;
            return from.AddTicks(bucketIndex * bucket.Ticks);
        }

        public async Task<ProcessedStatsDto> GetProcessedStatsAsync( CancellationToken cancellationToken = default)
        {
            var stats = await repository.GetProcessedStatsAsync(cancellationToken);

            if (stats is null)
                return new ProcessedStatsDto();

            return new ProcessedStatsDto
            {
                TotalLines = stats.TotalLines,
                ReadingsStored = stats.ReadingsStored,
                DuplicatesRemoved = stats.DuplicatesRemoved,
                InvalidRecords = stats.InvalidRecords
            };
        }

        public Task ResetStatsAsync( CancellationToken cancellationToken = default)
        {
            return repository.ResetAsync( cancellationToken);
        }

        internal static class AggregationCalculator
        {
            public static AggregationResultDto Calculate(DateTime bucketStart, IEnumerable<Reading> readings)
            {
                var count = 0;
                var sum = 0d;
                var min = double.MaxValue;
                var max = double.MinValue;

                foreach (var reading in readings)
                {
                    count++;

                    sum += reading.Value;

                    if (reading.Value < min)
                        min = reading.Value;

                    if (reading.Value > max)
                        max = reading.Value;
                }

                return new AggregationResultDto
                {
                    BucketStart = bucketStart,
                    Count = count,
                    Average = Math.Round(sum / count, 2),
                    Minimum = Math.Round(min, 2),
                    Maximum = Math.Round(max, 2)
                };
            }
        }

    }
}
