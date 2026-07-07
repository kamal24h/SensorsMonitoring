using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public sealed class DefaultAggregationStrategy : IAggregationStrategy
    {
        public AggregationResultDto Calculate(DateTime bucketStart, IReadOnlyCollection<Reading> readings)
        {
            ArgumentNullException.ThrowIfNull(readings);

            if (readings.Count == 0)
                throw new ArgumentException("Bucket is empty.", nameof(readings));

            double sum = 0;
            double min = double.MaxValue;
            double max = double.MinValue;

            foreach (var reading in readings)
            {
                sum += reading.Value;

                if (reading.Value < min)
                    min = reading.Value;

                if (reading.Value > max)
                    max = reading.Value;
            }

            return new AggregationResultDto
            {
                BucketStart = bucketStart,
                Count = readings.Count,
                Average = Math.Round(sum / readings.Count, 2),
                Minimum = Math.Round(min, 2),
                Maximum = Math.Round(max, 2)
            };
        }
    }
}
