namespace Domain.Entities;

public sealed record ReadingIdentity(
    string DeviceId,
    string Metric,
    DateTime Timestamp,
    int Sequence);

