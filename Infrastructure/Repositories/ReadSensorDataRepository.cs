using System.Diagnostics;
using Application.DTOs;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public class ReadSensorDataRepository(
        SensorDbContext context,
        ILogger<ReadSensorDataRepository> logger
        ) : IReadSensorDataRepository
    {
        private readonly SensorDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<ReadSensorDataRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ProcessedReadings _stats = new();
        private readonly SemaphoreSlim _statsLock = new(1, 1);
        private bool _isStatsUpdated = false;

        public async Task AddAsync(Reading reading, CancellationToken cancellationToken = default)
        {
            if (reading == null)
                throw new ArgumentNullException(nameof(reading));

            try
            {
                // بررسی وجود رکورد تکراری در دیتابیس
                var existing = await _context.Readings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                        r.DeviceId == reading.DeviceId &&
                        r.Metric == reading.Metric &&
                        r.Timestamp == reading.Timestamp &&
                        r.Sequence == reading.Sequence,
                        cancellationToken);

                if (existing != null)
                {
                    // به‌روزرسانی آمار
                    await _statsLock.WaitAsync(cancellationToken);
                    try
                    {
                        _stats.TryAddReading(reading);
                    }
                    finally
                    {
                        _statsLock.Release();
                    }

                    _logger.LogTrace("Duplicate reading: {DeviceId}/{Metric} at {Timestamp} (Seq: {Seq})",
                        reading.DeviceId, reading.Metric, reading.Timestamp, reading.Sequence);
                    return;
                }

                // افزودن رکورد جدید
                await _context.Readings.AddAsync(reading, cancellationToken);

                // به‌روزرسانی آمار
                await _statsLock.WaitAsync(cancellationToken);
                try
                {
                    _stats.TryAddReading(reading);
                    _isStatsUpdated = false;
                }
                finally
                {
                    _statsLock.Release();
                }

                _logger.LogTrace("Added reading: {DeviceId}/{Metric} at {Timestamp} (Seq: {Seq})",
                    reading.DeviceId, reading.Metric, reading.Timestamp, reading.Sequence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reading: {ReadingId}", reading.Id);
                throw;
            }
        }

        public async Task<IEnumerable<Reading>> GetByDeviceAndMetricAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogTrace("Querying readings: Device={DeviceId}, Metric={Metric}, From={From}, To={To}",
                    deviceId, metric, from, to);

                var query = _context.Readings
                    .AsNoTracking()
                    .Where(r =>
                        r.DeviceId == deviceId &&
                        r.Metric == metric &&
                        r.Timestamp >= from &&
                        r.Timestamp <= to);

                var result = await query
                    .OrderBy(r => r.Timestamp)
                    .ToListAsync(cancellationToken);

                _logger.LogTrace("Query returned {Count} readings", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying readings for Device={DeviceId}, Metric={Metric}",
                    deviceId, metric);
                throw;
            }
        }

        public async Task<ProcessedStats> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new ProcessedStats(0, 0, 0, 0);
            try
            {
                await _statsLock.WaitAsync(cancellationToken);
                try
                {
                    // اگر آمار به‌روز نیست یا اولین بار است که فراخوانی می‌شود
                    if (!_isStatsUpdated)
                    {
                        var statsDb = await _context.ProcessedStats.FirstOrDefaultAsync(cancellationToken: cancellationToken);
                        if (statsDb != null)
                            stats.Update(statsDb.TotalLines, statsDb.ReadingsStored, statsDb.DuplicatesRemoved, statsDb.InvalidRecords);
                        _isStatsUpdated = true;
                    }
                    return new ProcessedStats(stats.TotalLines, stats.ReadingsStored, stats.DuplicatesRemoved, stats.InvalidRecords);
                }
                finally
                {
                    _statsLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                throw;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int changes = 0;
            try
            {
                changes = await _context.SaveChangesAsync(cancellationToken);
                _isStatsUpdated = false;

                _logger.LogInformation("Changes saved successfully. {Changes} entries affected.", changes);

                // به‌روزرسانی آمار بعد از ذخیره‌سازی
                await GetProcessedStatsAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database update error");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving changes");
                throw;
            }
            
            return changes;
        }

        public async Task ResetStatsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _statsLock.WaitAsync(cancellationToken);
                try
                {
                    // پاک کردن تمام داده‌ها از دیتابیس

                    var allReadings = await _context.Readings.ToListAsync(cancellationToken);
                    _context.Readings.RemoveRange(allReadings);

                    var stats = await _context.ProcessedStats.FirstOrDefaultAsync();
                    if(stats != null)
                        _context.ProcessedStats.Remove(stats);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("All statistics and data have been reset");
                }
                finally
                {
                    _statsLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting stats");
                throw;
            }
        }

        public async Task<ProcessedStats> UpdateProcessingStats(ProcessedReadings processed)
        {

            var currentStats = await _context.ProcessedStats.FirstOrDefaultAsync();

            try{            
                if (currentStats != null)
                {
                    currentStats.Update(processed.TotalLines, processed.Readings.Count, processed.DuplicatesRemoved, processed.InvalidRecords);
                }
                else
                {
                    await _context.AddAsync(new ProcessedStats(processed.TotalLines, processed.Readings.Count, processed.DuplicatesRemoved, processed.InvalidRecords));
                }
                await _context.SaveChangesAsync();
                return new ProcessedStats(processed.TotalLines, processed.Readings.Count, processed.DuplicatesRemoved, processed.InvalidRecords);
            } catch (Exception)
            {
                _logger.LogInformation("ProcessingStats update was not successful!");
                return new ProcessedStats(currentStats?.TotalLines?? 0, 
                                            currentStats?.ReadingsStored ?? 0,
                                            currentStats?.DuplicatesRemoved ?? 0,
                                            currentStats?.InvalidRecords ?? 0);
            }
        }
    }
}