using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class ReadSensorDataService : IReadSensorDataService
    {
        private readonly IReadingRepository _repository;
        private readonly ILogger<ReadSensorDataService> _logger;

        public ReadSensorDataService(IReadingRepository repository, ILogger<ReadSensorDataService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting file processing: {FilePath}", filePath);

            var processed = new ProcessedReadings();
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            foreach (var line in lines)
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<ReadingDto>(line);
                    if (dto == null)
                    {
                        processed.IncrementInvalidRecords();
                        _logger.LogWarning("Invalid JSON format: {Line}", line);
                        continue;
                    }

                    var reading = new Reading(
                        dto.DeviceId,
                        dto.Metric,
                        dto.Timestamp,
                        dto.Sequence,
                        dto.Value
                    );

                    if (processed.TryAddReading(reading))
                    {
                        await _repository.AddAsync(reading, cancellationToken);
                    }
                    else
                    {
                        _logger.LogDebug("Duplicate reading detected: {ReadingId}", reading.Id);
                    }
                }
                catch (JsonException ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogWarning(ex, "JSON parsing error for line: {Line}", line);
                }
                catch (Domain.Exceptions.InvalidReadingException ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogWarning(ex, "Invalid reading data for line: {Line}", line);
                }
                catch (Exception ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogError(ex, "Unexpected error processing line: {Line}", line);
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("File processing completed. Total: {Total}, Stored: {Stored}, Duplicates: {Duplicates}, Invalid: {Invalid}",
                processed.TotalLines, processed.Readings.Count, processed.DuplicatesRemoved, processed.InvalidRecords);
        }

        public async Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            int bucketSizeSeconds,
            CancellationToken cancellationToken = default)
        {
            var readings = await _repository.GetByDeviceAndMetricAsync(
                deviceId, metric, from, to, cancellationToken);

            if (!readings.Any())
                return Enumerable.Empty<AggregationResultDto>();

            // گروه‌بندی بر اساس بازه زمانی
            var grouped = readings
                .GroupBy(r => GetBucketStart(r.Id.Timestamp, from, bucketSizeSeconds))
                .Select(g => new AggregationResultDto
                {
                    BucketStart = g.Key,
                    Count = g.Count(),
                    Average = g.Average(r => r.Value),
                    Minimum = g.Min(r => r.Value),
                    Maximum = g.Max(r => r.Value)
                })
                .OrderBy(r => r.BucketStart)
                .ToList();

            return grouped;
        }

        public async Task<ProcessedStatsDto> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            var stats = await _repository.GetProcessedStatsAsync(cancellationToken);
            return new ProcessedStatsDto()
            {
                TotalLines = stats.TotalLines,
                ReadingsStored = stats.Readings.Count,
                DuplicatesRemoved = stats.DuplicatesRemoved,
                InvalidRecords = stats.InvalidRecords
            };
        }

        private DateTime GetBucketStart(DateTime timestamp, DateTime rangeStart, int bucketSizeSeconds)
        {
            var elapsed = (timestamp - rangeStart).TotalSeconds;
            var bucketIndex = (int)(elapsed / bucketSizeSeconds);
            return rangeStart.AddSeconds(bucketIndex * bucketSizeSeconds);
        }
    }
}
