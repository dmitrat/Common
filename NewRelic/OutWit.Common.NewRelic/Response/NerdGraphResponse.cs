using System.Text.Json.Serialization;

namespace OutWit.Common.NewRelic.Response
{
    internal sealed class NerdGraphResponse
    {
        [JsonPropertyName("data")]
        public NerdGraphData? Data { get; set; }

        [JsonPropertyName("errors")]
        public NerdGraphError[]? Errors { get; set; }
    }
}
