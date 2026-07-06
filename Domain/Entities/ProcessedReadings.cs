namespace Domain.Entities;

public class ProcessedReadings
{
    // برای تشخیص رکوردهای تکراری
    private readonly HashSet<ReadingIdentity> _identities = [];

    // برای نگهداری رکوردهای معتبر
    private readonly List<Reading> _readings = [];

    public IReadOnlyCollection<Reading> Readings => _readings.AsReadOnly();

    public int TotalLines { get; set; }
    public int DuplicatesRemoved { get; set; }
    public int InvalidRecords { get; set; }

    public bool TryAddReading(Reading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        TotalLines++;

        // دستور HashSet.Add اگر قبلاً وجود داشته باشد false برمی‌گرداند 
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
        //TotalLines++;
    }
        
}