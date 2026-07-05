using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record ReadingDto
    {
        public string DeviceId { get; init; }
        public string Metric { get; init; }
        public DateTime Timestamp { get; init; }
        public double Value { get; init; }
        public int Sequence { get; init; }
    }
}