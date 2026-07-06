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
            return await _context.Readings
                .Where(r =>
                    r.Id.DeviceId == deviceId &&
                    r.Id.Metric == metric &&
                    r.Id.Timestamp >= from &&
                    r.Id.Timestamp <= to)
                .OrderBy(r => r.Id.Timestamp)
                .ToListAsync(cancellationToken);
        }

        public async Task<ProcessedReadings> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            // بخاطر استفاده از بانک اطلاعاتی InMemory 
            return _stats;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var res = await _context.SaveChangesAsync(cancellationToken);
            return res;
        }
    }
}

