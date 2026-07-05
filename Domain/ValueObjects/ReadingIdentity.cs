namespace Domain.ValueObjects;

public record ReadingIdentity
{
    public string DeviceId { get; }
    public string Metric { get; }
    public DateTime Timestamp { get; }
    public int Sequence { get; }

    public ReadingIdentity(string deviceId, string metric, DateTime timestamp, int sequence)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        Metric = metric ?? throw new ArgumentNullException(nameof(metric));
        Timestamp = timestamp;
        Sequence = sequence;
    }

    public override string ToString()
        => $"{DeviceId}-{Metric}-{Timestamp:yyyyMMddHHmmss}-{Sequence}";
}