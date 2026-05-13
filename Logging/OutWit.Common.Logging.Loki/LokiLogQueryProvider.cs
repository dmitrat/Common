using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Logging.Loki.LogQL;
using OutWit.Common.Logging.Loki.Response;
using OutWit.Common.Logging.Query;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.Loki
{
    /// <summary>
    /// <see cref="ILogQueryProvider"/> backed by a Grafana Loki server.
    /// Translates the neutral query model into LogQL and parses the
    /// streams / matrix responses from <c>/loki/api/v1/*</c> endpoints.
    /// </summary>
    public sealed class LokiLogQueryProvider : ILogQueryProvider
    {
        #region Constants

        private const int DEFAULT_PAGE_SIZE = 200;

        #endregion

        #region Constructors

        public LokiLogQueryProvider(LokiHttpClient client)
        {
            m_client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #endregion

        #region Fields

        private readonly LokiHttpClient m_client;

        #endregion

        #region ILogQueryProvider

        public async Task<LogPage> QueryAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var (start, end) = ResolveRange(query);
            EnforceRangeCap(start, end);

            var logql = LogQlBuilder.BuildRangeQuery(query, m_client.Options.DefaultLabels);
            var limit = ClampLimit(query.PageSize ?? DEFAULT_PAGE_SIZE);

            var response = await m_client.QueryRangeAsync(logql, start, end, limit,
                ascending: query.SortOrder == LogSortOrder.Ascending,
                ct: cancellationToken).ConfigureAwait(false);

            var entries = ExtractEntries(response).ToArray();

            return new LogPage
            {
                Items = entries,
                Offset = query.Offset,
                PageSize = limit,
                HasMore = entries.Length == limit
            };
        }

        public Task<LogPage> GetLogsAsync(DateTime from, DateTime to,
            IReadOnlyList<LogFilter>? filters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new LogQuery
            {
                From = from,
                To = to,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            }, cancellationToken);
        }

        public Task<LogPage> GetRecentLogsAsync(TimeSpan lookback,
            IReadOnlyList<LogFilter>? filters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new LogQuery
            {
                Lookback = lookback,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            }, cancellationToken);
        }

        public Task<LogPage> SearchAsync(string text, TimeSpan lookback,
            IReadOnlyList<LogFilter>? extraFilters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default)
        {
            return QueryAsync(new LogQuery
            {
                Lookback = lookback,
                FullTextSearch = text,
                Filters = extraFilters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetDistinctValuesAsync(
            DateTime from, DateTime to,
            LogAttribute attribute,
            IReadOnlyList<LogFilter>? filters = null,
            int limit = 1000,
            CancellationToken cancellationToken = default)
        {
            // Loki exposes label-values cheaply only for stream labels. For
            // attributes that live in the parsed JSON body, the only way is to
            // scan results — out of scope here. Callers can fall back to picking
            // values from a recent page.
            var labelName = NormalizeLabel(attribute.Value);
            var response = await m_client.GetLabelValuesAsync(labelName, from, to, cancellationToken)
                .ConfigureAwait(false);

            return response.Data.Take(limit).ToList();
        }

        public Task<long> FindOffsetAsync(LogQuery query, DateTime timestamp,
            CancellationToken cancellationToken = default)
        {
            // Loki has no row-offset concept; pagination is by time window. UI
            // callers should narrow the window via <c>start</c>/<c>end</c> instead.
            return Task.FromResult(-1L);
        }

        public async Task<LogStatistics> GetStatisticsAsync(
            DateTime from, DateTime to,
            IReadOnlyList<LogFilter>? filters = null,
            CancellationToken cancellationToken = default)
        {
            var range = to - from;
            if (range <= TimeSpan.Zero)
                return new LogStatistics { From = from, To = to };

            var logql = LogQlBuilder.BuildLevelHistogram(filters, m_client.Options.DefaultLabels, range);
            var response = await m_client.QueryRangeAsync(logql, from, to,
                limit: ClampLimit(DEFAULT_PAGE_SIZE),
                ascending: true,
                ct: cancellationToken).ConfigureAwait(false);

            var counts = AggregateLevelCounts(response);

            return new LogStatistics
            {
                From = from,
                To = to,
                TotalCount = counts.Values.Sum(),
                ErrorCount = counts.GetValueOrDefault("Error") + counts.GetValueOrDefault("Critical") + counts.GetValueOrDefault("Fatal"),
                WarningCount = counts.GetValueOrDefault("Warning"),
                InfoCount = counts.GetValueOrDefault("Information"),
                DebugCount = counts.GetValueOrDefault("Debug") + counts.GetValueOrDefault("Trace")
            };
        }

        public Task<LogStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            // Loki's HTTP API doesn't expose ingestion volume directly. Operators
            // who need it integrate with the Loki admin metrics endpoint or
            // Prometheus, which is out of scope for the first cut. We return an
            // all-null shape so the UI can show "Storage info unavailable".
            return Task.FromResult(new LogStorageInfo());
        }

        #endregion

        #region Tools

        private (DateTime start, DateTime end) ResolveRange(LogQuery query)
        {
            if (query.From.HasValue && query.To.HasValue)
                return (Normalize(query.From.Value), Normalize(query.To.Value));

            if (query.Lookback.HasValue)
            {
                var end = DateTime.UtcNow;
                return (end - query.Lookback.Value, end);
            }

            // Default — last hour.
            var endDefault = DateTime.UtcNow;
            return (endDefault - TimeSpan.FromHours(1), endDefault);
        }

        private void EnforceRangeCap(DateTime start, DateTime end)
        {
            var max = m_client.Options.MaxRange;
            if (max > TimeSpan.Zero && (end - start) > max)
                throw new ArgumentOutOfRangeException(nameof(end),
                    $"Requested range exceeds LokiOptions.MaxRange ({max}).");
        }

        private int ClampLimit(int requested)
        {
            var max = m_client.Options.MaxResultLimit;
            if (max > 0 && requested > max) return max;
            if (requested <= 0) return DEFAULT_PAGE_SIZE;
            return requested;
        }

        private static IEnumerable<LogEntry> ExtractEntries(LokiQueryResponse response)
        {
            if (response.Data?.Result is null) yield break;

            foreach (var stream in response.Data.Result)
            {
                if (stream.Values is null) continue;
                var labels = stream.Stream ?? new Dictionary<string, string>();
                foreach (var pair in stream.Values)
                {
                    if (pair is null || pair.Length < 2) continue;

                    var timestamp = ParseUnixNanoseconds(pair[0]);
                    var line = pair[1];

                    yield return new LogEntry
                    {
                        Timestamp = timestamp,
                        Message = line,
                        Level = ResolveLevel(labels),
                        ServiceName = labels.GetValueOrDefault("service_name") ?? labels.GetValueOrDefault("service.name"),
                        Host = labels.GetValueOrDefault("hostname") ?? labels.GetValueOrDefault("host"),
                        Environment = labels.GetValueOrDefault("env") ?? labels.GetValueOrDefault("environment"),
                        SourceContext = labels.GetValueOrDefault("logger") ?? labels.GetValueOrDefault("SourceContext"),
                        TraceId = labels.GetValueOrDefault("trace_id") ?? labels.GetValueOrDefault("traceId"),
                        SpanId = labels.GetValueOrDefault("span_id") ?? labels.GetValueOrDefault("spanId")
                    };
                }
            }
        }

        private static LogSeverity? ResolveLevel(IReadOnlyDictionary<string, string> labels)
        {
            if (!labels.TryGetValue("level", out var raw) || string.IsNullOrWhiteSpace(raw))
                return null;
            return LogSeverity.GetAll().FirstOrDefault(s => string.Equals(s.Value, raw, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, long> AggregateLevelCounts(LokiQueryResponse response)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (response.Data?.Result is null) return result;

            foreach (var entry in response.Data.Result)
            {
                var level = entry.Metric?.GetValueOrDefault("level");
                if (string.IsNullOrEmpty(level) || entry.Values is null) continue;

                long sum = 0;
                foreach (var pair in entry.Values)
                {
                    if (pair is null || pair.Length < 2) continue;
                    if (double.TryParse(pair[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out var value))
                    {
                        sum += (long)value;
                    }
                }
                result[level!] = result.GetValueOrDefault(level!) + sum;
            }
            return result;
        }

        private static DateTime ParseUnixNanoseconds(string ns)
        {
            if (!long.TryParse(ns, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                return default;

            const long UNIX_EPOCH_TICKS = 621355968000000000L;
            var ticks = value / 100L + UNIX_EPOCH_TICKS;
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static DateTime Normalize(DateTime t)
            => t.Kind == DateTimeKind.Utc ? t : t.ToUniversalTime();

        private static string NormalizeLabel(string name) => name.Replace('.', '_');

        #endregion
    }
}
