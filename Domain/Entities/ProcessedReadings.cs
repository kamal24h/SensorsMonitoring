namespace Domain.Entities;

public class ProcessedReadings
{
    // برای تشخیص رکوردهای تکراری
    private readonly HashSet<ReadingIdentity> _identities = [];

    // برای نگهداری رکوردهای معتبر
    private readonly List<Reading> _readings = [];

    public IReadOnlyCollection<Reading> Readings => _readings.AsReadOnly();

    public int TotalLines { get; private set; }
    public int DuplicatesRemoved { get; private set; }
    public int InvalidRecords { get; private set; }

    public bool TryAddReading(Reading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        TotalLines++;

        // HashSet.Add اگر قبلاً وجود داشته باشد false برمی‌گرداند
        if (!_identities.Add(reading.Id))
        {
            DuplicatesRemoved++;
            return false;
        }

        _readings.Add(reading);
        return true;
    }

    public void IncrementInvalidRecords()
    {
        InvalidRecords++;
        TotalLines++;
    }
}
