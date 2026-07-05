using Domain.Entities;
using Domain.Interfaces;

namespace Infrastructure.Repositories
{
    public class ReadSensorDataRepository : IReadSensorDataRepository
    {
        public Task AddAsync(Reading reading, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Reading>> GetByDeviceAndMetricAsync(string deviceId, string metric, DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ProcessedReadings> GetProcessedStatsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
