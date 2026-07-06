using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ReadSensorDataRepository(SensorDbContext context) : IReadSensorDataRepository
    {
        private readonly SensorDbContext _context = context;
        private readonly ProcessedReadings _stats = new();

        public async Task AddAsync(Reading reading, CancellationToken cancellationToken = default)
        {
            await _context.Readings.AddAsync(reading, cancellationToken);
            _stats.TryAddReading(reading);
        }

        public async Task<IEnumerable<Reading>> GetByDeviceAndMetricAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var result = await _context.Readings
                .Where(r =>
                    r.DeviceId == deviceId &&
                    r.Metric == metric &&
                    r.Timestamp >= from &&
                    r.Timestamp <= to)
                .OrderBy(r => r.Timestamp)
                .ToListAsync(cancellationToken);

            return result;
        }

        public async Task<ProcessedReadings> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            // بخاطر استفاده از بانک اطلاعاتی InMemory 
            // todo: not implemented yet
            return _stats;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var res = await _context.SaveChangesAsync(cancellationToken);
            return res;
        }
    }
}

