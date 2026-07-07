
namespace Domain.Entities
{
    public sealed class ProcessedReadings
    {
        private readonly HashSet<ReadingIdentity> _identities = [];

        public int TotalLines { get; private set; }

        public int StoredReadings { get; private set; }

        public int InvalidRecords { get; private set; }

        public int DuplicatesRemoved { get; private set; }

        public void IncrementLine()
            => TotalLines++;

        public void IncrementInvalid()
            => InvalidRecords++;

        public bool Register(Reading reading)
        {
            if (!_identities.Add(reading.Id))
            {
                DuplicatesRemoved++;
                return false;
            }

            StoredReadings++;

            return true;
        }
    }
}