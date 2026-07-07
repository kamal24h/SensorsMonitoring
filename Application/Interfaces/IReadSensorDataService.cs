using Application.DTOs;

namespace Application.Interfaces
{

    public interface IReadSensorDataService
    {
        Task<ProcessedStatsDto> ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            int bucketSizeSeconds,
            CancellationToken cancellationToken = default);
        Task<ProcessedStatsDto> GetProcessedStatsAsync(CancellationToken cancellationToken = default);
        Task ResetStatsAsync(CancellationToken cancellationToken = default);
    }
}
