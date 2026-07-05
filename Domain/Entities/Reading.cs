namespace Domain.Entities
{
    public class Reading
    {
        public string DeviceId { get; }

        public string Metric { get; }

        public DateTime Timestamp { get; }

        public double Value { get; }

        public int Sequence { get; }

        // Reading identity (for deduplication)
        public ReadingIdentity Identity =>
            new(DeviceId, Metric, Timestamp, Sequence);
    }
}
