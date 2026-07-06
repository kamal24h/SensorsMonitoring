using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IReadSensorDataRepository
    {
        Task AddAsync(Reading reading, CancellationToken cancellationToken = default);
        Task<IEnumerable<Reading>> GetByDeviceAndMetricAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);
        Task<ProcessedStats> GetProcessedStatsAsync(CancellationToken cancellationToken = default);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task ResetStatsAsync(CancellationToken cancellationToken = default);
        Task<ProcessedStats> UpdateProcessingStats(ProcessedReadings processed);
    }
}