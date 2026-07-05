using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Interfaces
{

    public interface IReadSensorDataService
    {
        Task ProcessFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task<IEnumerable<AggregationResultDto>> GetAggregatedDataAsync(
            string deviceId,
            string metric,
            DateTime from,
            DateTime to,
            int bucketSizeSeconds,
            CancellationToken cancellationToken = default);
        Task<ProcessedStatsDto> GetProcessedStatsAsync(CancellationToken cancellationToken = default);
    }
}
