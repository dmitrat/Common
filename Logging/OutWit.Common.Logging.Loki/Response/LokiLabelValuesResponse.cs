using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OutWit.Common.Logging.Loki.Response
{
    /// <summary>
    /// Root of a <c>/loki/api/v1/label/{name}/values</c> response.
    /// </summary>
    public sealed class LokiLabelValuesResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public List<string> Data { get; set; } = new();
    }
}
