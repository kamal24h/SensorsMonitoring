using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories
{
    public sealed class ReadSensorDataRepository(SensorDbContext context,
        ILogger<ReadSensorDataRepository> logger) : IReadSensorDataRepository
    {
        private readonly SensorDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        private readonly ILogger<ReadSensorDataRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task AddAsync( Reading reading, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(reading);
            await _context.Readings.AddAsync(reading, cancellationToken);
        }

        public async Task<IEnumerable<Reading>> GetByDeviceAndMetricAsync(string deviceId, string metric,
            DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogTrace("Querying readings: Device={DeviceId}, Metric={Metric}, From={From}, To={To}",
                    deviceId, metric, from, to);

                var result = await _context.Readings
                    .AsNoTracking()
                    .Where(r =>
                        r.DeviceId == deviceId &&
                        r.Metric == metric &&
                        r.Timestamp >= from.ToUniversalTime() &&
                        r.Timestamp < to.ToUniversalTime()) // [from,to)
                    .OrderBy(r => r.Timestamp)
                    .ToListAsync(cancellationToken);

                _logger.LogTrace("Query returned {Count} readings", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying readings for Device={DeviceId}, Metric={Metric}", deviceId, metric);
                throw;
            }
        }

        public async Task<ProcessedStats?> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            var result = await _context.ProcessedStats.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            return result;
        }

        public async Task SaveProcessingStatsAsync(ProcessedStats stats, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stats);

            var current = await _context.ProcessedStats.FirstOrDefaultAsync(cancellationToken);

            if (current is null)
            {
                await _context.ProcessedStats.AddAsync(stats, cancellationToken);
            }
            else
            {
                current.Update(stats.TotalLines, stats.ReadingsStored, stats.DuplicatesRemoved, stats.InvalidRecords);
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var changes = await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Changes saved successfully. {Changes} entries affected.", changes);
                return changes;
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
        }

        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            _context.Readings.RemoveRange(_context.Readings);

            var stats = await _context.ProcessedStats.FirstOrDefaultAsync(cancellationToken);

            if (stats is not null)
                _context.ProcessedStats.Remove(stats);

            await _context.SaveChangesAsync(cancellationToken);
        }
    
    }
}