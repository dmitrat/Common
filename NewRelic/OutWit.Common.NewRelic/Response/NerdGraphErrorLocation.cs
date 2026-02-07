using System.Text.Json.Serialization;

namespace OutWit.Common.NewRelic.Response
{
    internal sealed class NerdGraphErrorLocation
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }

        [JsonPropertyName("column")]
        public int Column { get; set; }
    }
}
