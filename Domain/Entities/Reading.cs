using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Reading
{
    public ReadingIdentity Id { get; private set; }
    public double Value { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    //private Reading() { } // برای EF Core

    public Reading(string deviceId, string metric, DateTime timestamp, int sequence, double value)
    {
        Validate(value);
        Id = new ReadingIdentity(deviceId, metric, timestamp, sequence);
        Value = value;
        ProcessedAt = DateTime.UtcNow;
    }

    private static void Validate(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new InvalidReadingException($"Invalid value: {value}");

        if (value < -1000 || value > 1000) // محدوده منطقی برای سنسورها
            throw new InvalidReadingException($"Value out of range: {value}");
    }

    public bool IsDuplicate(Reading other)
        => Id == other?.Id;
}