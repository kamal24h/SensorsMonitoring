
namespace Domain.Entities
{
    public sealed class AggregatedBucket
    {
        public DateTime BucketStart { get; init; }
        public int Count { get; init; }
        public double Average { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
    }
}
