using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record AggregationResultDto
    {
        public DateTime BucketStart { get; init; }
        public int Count { get; init; }
        public double Average { get; init; }
        public double Minimum { get; init; }
        public double Maximum { get; init; }
    }
}
