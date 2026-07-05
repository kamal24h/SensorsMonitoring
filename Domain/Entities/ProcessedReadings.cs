namespace Domain.Entities;

public class ProcessedReadings
{
    private readonly HashSet<Reading> _readings = [];

    public IReadOnlyCollection<Reading> Readings => _readings.ToList().AsReadOnly();
    public int TotalLines { get; private set; }
    public int DuplicatesRemoved { get; private set; }
    public int InvalidRecords { get; private set; }

    public ProcessedReadings()
    {
        TotalLines = 0;
        DuplicatesRemoved = 0;
        InvalidRecords = 0;
    }

    public bool TryAddReading(Reading reading)
    {
        TotalLines++;

        if (_readings.Contains(reading))
        {
            DuplicatesRemoved++;
            return false;
        }

        _readings.Add(reading);
        return true;
    }

    public void IncrementInvalidRecords()
        => InvalidRecords++;
}