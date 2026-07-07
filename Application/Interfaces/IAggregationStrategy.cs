
using Application.DTOs;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAggregationStrategy
    {
        AggregationResultDto Calculate(DateTime bucketStart, IReadOnlyCollection<Reading> readings);
    }
}
