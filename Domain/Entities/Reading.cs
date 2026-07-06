using System.Numerics;
using Domain.Exceptions;

namespace Domain.Entities;
public class Reading
{
    public string DeviceId { get; private set; } = null!;
    public string Metric { get; private set; } = null!;
    public DateTime Timestamp { get; private set; }
    public int Sequence { get; private set; }

    public ReadingIdentity Id => new(DeviceId, Metric, Timestamp, Sequence);

    public double Value { get; private set; }

    public DateTime ProcessedAt { get; private set; }

    private Reading() { }

    public Reading(string deviceId, string metric, DateTime timestamp, int sequence, double value)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new InvalidReadingException("DeviceId is required.");

        if (string.IsNullOrWhiteSpace(metric))
            throw new InvalidReadingException("Metric is required.");

        if (sequence < 0)
            throw new InvalidReadingException("Sequence must be non-negative.");

        Validate(value);

        DeviceId = deviceId;
        Metric = metric;
        Timestamp = timestamp;
        Sequence = sequence;

        Value = value;
        ProcessedAt = DateTime.UtcNow;
    }

    private static void Validate(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidReadingException("Invalid value.");

    }
}
