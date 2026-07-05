using Domain.Entities;

namespace Application.DTOs
{
    public class ProcessedStatsDto
    {
        public int TotalLines { get; init; }
        public int ReadingsStored { get; init; }
        public int DuplicatesRemoved { get; init; }
        public int InvalidRecords { get; init; }
    }
}
