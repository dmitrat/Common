using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OutWit.Common.NewRelic.Response
{
    internal sealed class NerdGraphError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("path")]
        public string[]? Path { get; set; }

        [JsonPropertyName("extensions")]
        public Dictionary<string, object>? Extensions { get; set; }

        [JsonPropertyName("locations")]
        public NerdGraphErrorLocation[]? Locations { get; set; }
    }
}
