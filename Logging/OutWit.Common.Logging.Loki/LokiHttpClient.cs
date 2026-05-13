using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Logging.Loki.Response;

namespace OutWit.Common.Logging.Loki
{
    /// <summary>
    /// Typed wrapper over Loki's <c>/loki/api/v1/*</c> HTTP endpoints. Owns base
    /// URL, optional basic auth and the <c>X-Scope-OrgID</c> multi-tenancy header.
    /// </summary>
    public class LokiHttpClient
    {
        #region Constants

        private const string PATH_QUERY_RANGE = "/loki/api/v1/query_range";

        private const string PATH_LABEL_VALUES = "/loki/api/v1/label/{0}/values";

        #endregion

        #region Constructors

        public LokiHttpClient(HttpClient http, LokiOptions options)
        {
            m_http = http ?? throw new ArgumentNullException(nameof(http));
            Options = options ?? throw new ArgumentNullException(nameof(options));

            ConfigureHttp();
        }

        #endregion

        #region Fields

        private readonly HttpClient m_http;

        #endregion

        #region Initialization

        private void ConfigureHttp()
        {
            if (string.IsNullOrWhiteSpace(Options.BaseUrl))
                throw new InvalidOperationException("LokiOptions.BaseUrl is required.");

            // Allow callers to inject HttpClient with a pre-set base address (typed
            // client DI scenario); otherwise apply ours.
            if (m_http.BaseAddress is null)
                m_http.BaseAddress = new Uri(Options.BaseUrl);

            if (!string.IsNullOrEmpty(Options.TenantId)
                && !m_http.DefaultRequestHeaders.Contains("X-Scope-OrgID"))
            {
                m_http.DefaultRequestHeaders.Add("X-Scope-OrgID", Options.TenantId);
            }

            if (!string.IsNullOrEmpty(Options.Username)
                && m_http.DefaultRequestHeaders.Authorization is null)
            {
                var raw = $"{Options.Username}:{Options.Password ?? string.Empty}";
                var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                m_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", b64);
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Runs a <c>/loki/api/v1/query_range</c> request and deserializes the
        /// streams response.
        /// </summary>
        public async Task<LokiQueryResponse> QueryRangeAsync(string logql,
            DateTime startUtc,
            DateTime endUtc,
            int limit,
            bool ascending,
            CancellationToken ct = default)
        {
            var url = BuildQueryRangeUrl(logql, startUtc, endUtc, limit, ascending);
            using var resp = await m_http.GetAsync(url, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<LokiQueryResponse>(body, JsonOptions)
                   ?? new LokiQueryResponse();
        }

        /// <summary>
        /// Returns all known values for the named label via
        /// <c>/loki/api/v1/label/{name}/values</c>.
        /// </summary>
        public async Task<LokiLabelValuesResponse> GetLabelValuesAsync(string labelName,
            DateTime? startUtc = null,
            DateTime? endUtc = null,
            CancellationToken ct = default)
        {
            var url = BuildLabelValuesUrl(labelName, startUtc, endUtc);
            using var resp = await m_http.GetAsync(url, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<LokiLabelValuesResponse>(body, JsonOptions)
                   ?? new LokiLabelValuesResponse();
        }

        #endregion

        #region Tools

        private static string BuildQueryRangeUrl(string logql, DateTime startUtc, DateTime endUtc,
            int limit, bool ascending)
        {
            // Loki expects nanoseconds since the Unix epoch.
            var startNs = ToUnixNanoseconds(startUtc);
            var endNs = ToUnixNanoseconds(endUtc);
            var direction = ascending ? "forward" : "backward";

            var sb = new StringBuilder(PATH_QUERY_RANGE);
            sb.Append('?').Append("query=").Append(Uri.EscapeDataString(logql));
            sb.Append('&').Append("start=").Append(startNs);
            sb.Append('&').Append("end=").Append(endNs);
            sb.Append('&').Append("limit=").Append(limit);
            sb.Append('&').Append("direction=").Append(direction);
            return sb.ToString();
        }

        private static string BuildLabelValuesUrl(string labelName, DateTime? startUtc, DateTime? endUtc)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(PATH_LABEL_VALUES, Uri.EscapeDataString(labelName));
            if (startUtc.HasValue || endUtc.HasValue)
            {
                sb.Append('?');
                var sep = false;
                if (startUtc.HasValue)
                {
                    sb.Append("start=").Append(ToUnixNanoseconds(startUtc.Value));
                    sep = true;
                }
                if (endUtc.HasValue)
                {
                    if (sep) sb.Append('&');
                    sb.Append("end=").Append(ToUnixNanoseconds(endUtc.Value));
                }
            }
            return sb.ToString();
        }

        public static long ToUnixNanoseconds(DateTime utc)
        {
            // DateTime.Ticks → 100ns intervals from 0001-01-01.
            // Unix epoch in DateTime.Ticks: 621355968000000000.
            const long UNIX_EPOCH_TICKS = 621355968000000000L;
            var ticks = (utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime()).Ticks - UNIX_EPOCH_TICKS;
            return ticks * 100L;
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Properties

        public LokiOptions Options { get; }

        #endregion
    }
}
