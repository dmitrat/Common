using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.NewRelic.Interfaces
{
    public interface INewRelicProvider
    {
        /// <summary>
        /// Universal method: query logs with filters, interval, and pagination.
        /// </summary>
        Task<NewRelicLogPage> QueryAsync(NewRelicLogQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convenience shortcut: logs for a time interval.
        /// </summary>
        Task<NewRelicLogPage> GetLogsAsync(DateTime from, DateTime to,
            IReadOnlyList<NewRelicLogFilter>? filters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Convenience shortcut: recent logs for the last N amount of time (e.g., 15 minutes).
        /// </summary>
        Task<NewRelicLogPage> GetRecentLogsAsync(TimeSpan lookback,
            IReadOnlyList<NewRelicLogFilter>? filters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search by text in the message.
        /// </summary>
        Task<NewRelicLogPage> SearchAsync(string text, TimeSpan lookback,
            IReadOnlyList<NewRelicLogFilter>? extraFilters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves distinct values (facets) for a specific attribute within a time range.
        /// Useful for populating filter dropdowns (e.g. SourceContext, service.name).
        /// Corresponds to NRQL: SELECT uniques(attribute) ...
        /// </summary>
        Task<IReadOnlyList<string>> GetDistinctValuesAsync(
            DateTime from, DateTime to, NewRelicLogAttribute attribute,
            IReadOnlyList<NewRelicLogFilter>? filters = null,
            int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds the offset (index) of a log entry with the given timestamp.
        /// Useful for "scroll to timestamp" functionality.
        /// </summary>
        Task<long> FindOffsetAsync(NewRelicLogQuery query, DateTime timestamp, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves comprehensive statistics about log data for a given time period.
        /// Includes: total count and counts by severity level.
        /// 
        /// Primary use case: analyzing log patterns and severity distribution.
        /// For actual storage/billing metrics, use GetDataConsumptionAsync instead.
        /// </summary>
        /// <param name="from">Start of the statistics period</param>
        /// <param name="to">End of the statistics period</param>
        /// <param name="filters">Optional filters to apply (e.g., specific services or environments)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Statistics including counts and severity distribution</returns>
        Task<NewRelicLogStatistics> GetStatisticsAsync(
            DateTime from, DateTime to,
            IReadOnlyList<NewRelicLogFilter>? filters = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves ACTUAL data consumption from New Relic billing data.
        /// This is what you see in the Data Management UI - real storage usage in GB.
        /// 
        /// Use this to:
        /// - Monitor free tier usage (100 GB limit)
        /// - Track month-to-date consumption
        /// - Project end-of-month usage
        /// - Get breakdown by data type (Logs, Metrics, Traces, Events)
        /// 
        /// Note: This queries NrConsumption event via NRQL, which contains GigabytesIngested metric.
        /// API: NRQL query "FROM NrConsumption SELECT sum(GigabytesIngested) ..."
        /// </summary>
        /// <param name="from">Start date for consumption period (typically beginning of month)</param>
        /// <param name="to">End date for consumption period (typically today)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Data consumption including actual GB used and projections</returns>
        Task<NewRelicDataConsumption> GetDataConsumptionAsync(
            DateTime from, DateTime to,
            CancellationToken cancellationToken = default);
    }
}
