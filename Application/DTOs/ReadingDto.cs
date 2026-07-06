using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public record ReadingDto
    {
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; init; } = default!;

        [JsonPropertyName("metric")]
        public string Metric { get; init; } = default!;

        [JsonPropertyName("ts")]
        public DateTime Timestamp { get; init; }

        [JsonPropertyName("value")]
        public double Value { get; init; }

        [JsonPropertyName("seq")]
        public int Sequence { get; init; }
    }
}
