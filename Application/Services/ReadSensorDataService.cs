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
        private readonly IReadSensorDataRepository _repository;
        private readonly ILogger<ReadSensorDataService> _logger;

        public ReadSensorDataService(IReadSensorDataRepository repository, ILogger<ReadSensorDataService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ProcessedStatsDto> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting file processing: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var processed = new ProcessedReadings();
            var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);

            _logger.LogInformation("Total lines to process: {LineCount}", lines.Length);
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogWarning("Line {LineNumber}: Empty or whitespace line", i + 1);
                    continue;
                }

                try
                {
                    var dto = JsonSerializer.Deserialize<ReadingDto>(line, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (dto == null)
                    {
                        processed.IncrementInvalidRecords();
                        _logger.LogWarning("Line {LineNumber}: Deserialization returned null", i + 1);
                        continue;
                    }

                    // اعتبارسنجی مقادیر
                    if (string.IsNullOrEmpty(dto.DeviceId) || string.IsNullOrEmpty(dto.Metric))
                    {
                        processed.IncrementInvalidRecords();
                        _logger.LogWarning("Line {LineNumber}: Missing DeviceId or Metric", i + 1);
                        continue;
                    }

                    var reading = new Reading(
                        dto.DeviceId,
                        dto.Metric,
                        dto.Timestamp,
                        dto.Sequence,
                        dto.Value
                    );

                    var added = processed.TryAddReading(reading);

                    if (added)
                    {
                        await _repository.AddAsync(reading, cancellationToken);
                        if (processed.Readings.Count % 100 == 0)
                        {
                            _logger.LogDebug("Processed {Count} readings so far", processed.Readings.Count);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Line {LineNumber}: Duplicate reading detected: {ReadingId}",
                            i + 1, reading.Id);
                    }
                }
                catch (JsonException ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogWarning(ex, "Line {LineNumber}: JSON parsing error: {Line}", i + 1, line);
                }
                catch (Domain.Exceptions.InvalidReadingException ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogWarning(ex, "Line {LineNumber}: Invalid reading data", i + 1);
                }
                catch (Exception ex)
                {
                    processed.IncrementInvalidRecords();
                    _logger.LogError(ex, "Line {LineNumber}: Unexpected error processing line", i + 1);
                }
            }

            // ذخیره تغییرات در دیتابیس
            await _repository.SaveChangesAsync(cancellationToken);

            // به‌روزرسانی آمار
            var updateResult = await _repository.UpdateProcessingStats(processed);
            var result = new ProcessedStatsDto()
            {
                TotalLines = (updateResult.TotalLines != 0)? updateResult.TotalLines : processed.TotalLines,
                ReadingsStored = (updateResult.ReadingsStored != 0) ? updateResult.ReadingsStored : processed.Readings.Count,
                DuplicatesRemoved = (updateResult.DuplicatesRemoved != 0) ? updateResult.DuplicatesRemoved : processed.DuplicatesRemoved,
                InvalidRecords = (updateResult.InvalidRecords != 0) ? updateResult.InvalidRecords : processed.InvalidRecords
            };
            
            return result;
        }

        public async Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            int bucketSizeSeconds,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting aggregated data for Device: {DeviceId}, Metric: {Metric}",
                deviceId, metric);
            _logger.LogDebug("Time range: {From} to {To}, Bucket size: {BucketSize}s",
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

            var readings = await _repository.GetByDeviceAndMetricAsync(
                deviceId, metric, from, to, cancellationToken);

            if (!readings.Any())
            {
                _logger.LogInformation("No readings found for the specified criteria");
                return [];
            }

            _logger.LogInformation("Found {Count} readings for aggregation", readings.Count());

            // گروه‌بندی بر اساس بازه زمانی
            var grouped = readings
                .GroupBy(r => GetBucketStart(r.Id.Timestamp, from, bucketSizeSeconds))
                .Select(g => new AggregationResultDto
                {
                    BucketStart = g.Key,
                    Count = g.Count(),
                    Average = Math.Round(g.Average(r => r.Value), 2),
                    Minimum = Math.Round(g.Min(r => r.Value), 2),
                    Maximum = Math.Round(g.Max(r => r.Value), 2)
                })
                .OrderBy(r => r.BucketStart)
                .ToList();

            _logger.LogInformation("Aggregation completed with {BucketCount} buckets", grouped.Count);
            return grouped;
        }

        public async Task<ProcessedStatsDto> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting processing statistics");
            var stats = await _repository.GetProcessedStatsAsync(cancellationToken);

            if (stats == null)
            {
                _logger.LogWarning("No statistics available");
                return new ProcessedStatsDto
                {
                    TotalLines = 0,
                    ReadingsStored = 0,
                    DuplicatesRemoved = 0,
                    InvalidRecords = 0
                };
            }

            return new ProcessedStatsDto
            {
                TotalLines = stats.TotalLines,
                ReadingsStored = stats.ReadingsStored,
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

        public async Task ResetStatsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Resetting statistics");
            await _repository.ResetStatsAsync(cancellationToken);
            _logger.LogInformation("Statistics reset completed");
        }

    }
}

