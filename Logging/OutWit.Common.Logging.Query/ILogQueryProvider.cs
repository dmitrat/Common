using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.Query
{
    /// <summary>
    /// Abstracts a queryable log backend (NewRelic NerdGraph, Grafana Loki, a local
    /// JSON-file tail, etc.). Concrete implementations live in vendor packages
    /// (e.g. <c>OutWit.Common.Logging.NewRelic</c>, <c>OutWit.Common.Logging.Loki</c>).
    /// </summary>
    public interface ILogQueryProvider
    {
        /// <summary>
        /// Universal method: query logs with filters, time range / lookback and pagination.
        /// </summary>
        Task<LogPage> QueryAsync(LogQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convenience shortcut: logs in a fixed time interval.
        /// </summary>
        Task<LogPage> GetLogsAsync(DateTime from, DateTime to,
            IReadOnlyList<LogFilter>? filters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Convenience shortcut: logs for the last <paramref name="lookback"/>
        /// (e.g. 15 minutes).
        /// </summary>
        Task<LogPage> GetRecentLogsAsync(TimeSpan lookback,
            IReadOnlyList<LogFilter>? filters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Free-text search across the message field.
        /// </summary>
        Task<LogPage> SearchAsync(string text, TimeSpan lookback,
            IReadOnlyList<LogFilter>? extraFilters = null,
            int? pageSize = null,
            int offset = 0,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves distinct values (facets) for an attribute within a time range —
        /// e.g. all SourceContext or service.name values seen. Useful for populating
        /// filter dropdowns.
        /// </summary>
        Task<IReadOnlyList<string>> GetDistinctValuesAsync(
            DateTime from, DateTime to,
            LogAttribute attribute,
            IReadOnlyList<LogFilter>? filters = null,
            int limit = 1000,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the offset of a log entry with the given timestamp inside the query
        /// result — used by "scroll to timestamp" UI affordances.
        /// </summary>
        Task<long> FindOffsetAsync(LogQuery query, DateTime timestamp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Per-level / total counts for a time interval. For storage / billing data,
        /// see <see cref="GetStorageInfoAsync"/>.
        /// </summary>
        Task<LogStatistics> GetStatisticsAsync(
            DateTime from, DateTime to,
            IReadOnlyList<LogFilter>? filters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Storage / quota state — used bytes, limit, total entries, optional
        /// vendor-specific breakdown. Each provider fills in what it can observe;
        /// unknown fields are returned as <c>null</c>.
        /// </summary>
        Task<LogStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default);
    }
}
