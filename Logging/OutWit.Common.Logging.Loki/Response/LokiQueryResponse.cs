using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OutWit.Common.Logging.Loki.Response
{
    /// <summary>
    /// Root of a <c>/loki/api/v1/query_range</c> response.
    /// </summary>
    public sealed class LokiQueryResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("data")]
        public LokiQueryData? Data { get; set; }
    }

    public sealed class LokiQueryData
    {
        /// <summary>"streams" for raw lines; "matrix"/"vector" for range vector queries.</summary>
        [JsonPropertyName("resultType")]
        public string? ResultType { get; set; }

        [JsonPropertyName("result")]
        public List<LokiStream> Result { get; set; } = new();
    }

    public sealed class LokiStream
    {
        /// <summary>Stream labels — the discriminating set, e.g. service, level.</summary>
        [JsonPropertyName("stream")]
        public Dictionary<string, string>? Stream { get; set; }

        /// <summary>
        /// Pairs of <c>[unix-ns-string, line]</c> for "streams" responses;
        /// for matrix responses use <see cref="Values"/> as <c>[ts, sample]</c>.
        /// </summary>
        [JsonPropertyName("values")]
        public List<string[]>? Values { get; set; }

        /// <summary>
        /// Set on aggregating queries (count_over_time, sum by ...).
        /// Same shape as <see cref="Stream"/>: a key/value descriptor for the sample stream.
        /// </summary>
        [JsonPropertyName("metric")]
        public Dictionary<string, string>? Metric { get; set; }
    }
}
