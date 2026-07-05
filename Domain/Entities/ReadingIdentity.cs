namespace Domain.Entities
{
    public record ReadingIdentity(
    string DeviceId,
    string Metric,
    DateTime Timestamp,
    int Sequence);
}
