using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.Logging.NewRelic.Interfaces;
using OutWit.Common.Logging.NewRelic.Model;
using OutWit.Common.Logging.NewRelic.Nrql;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.NewRelic
{
    public sealed class NewRelicProvider : INewRelicProvider
    {
        #region Constructors

        public NewRelicProvider(NewRelicHttpClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        #endregion

        #region Functions

        public async Task<LogPage> QueryAsync(LogQuery query, CancellationToken cancellationToken = default)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            query = Client.Validate(query);
            var nrql = query.BuildNrql();
            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            LogEntry[] entries = result.ToLogEntries();

            return new LogPage
            {
                Items = entries,
                Offset = query.Offset,
                PageSize = query.PageSize!.Value,
                HasMore = entries.Length == query.PageSize.Value
            };
        }

        public async Task<LogPage> GetLogsAsync(DateTime from, DateTime to, IReadOnlyList<LogFilter>? filters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                From = from,
                To = to,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<LogPage> GetRecentLogsAsync(TimeSpan lookback, IReadOnlyList<LogFilter>? filters = null, int? pageSize = null, int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                Lookback = lookback,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<LogPage> SearchAsync(string text, TimeSpan lookback, IReadOnlyList<LogFilter>? extraFilters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new LogQuery
            {
                Lookback = lookback,
                FullTextSearch = text,
                Filters = extraFilters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = LogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetDistinctValuesAsync(DateTime from, DateTime to, LogAttribute attribute, IReadOnlyList<LogFilter>? filters = null,
            int limit = 1000, CancellationToken cancellationToken = default)
        {
            var nrql = NrqlQueryBuilder.BuildDistinctNrql(attribute, from, to, filters, limit);

            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            return result.ToDistinctValues();
        }

        public async Task<long> FindOffsetAsync(LogQuery query, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            var nrql = NrqlQueryBuilder.BuildCountNrql(query, timestamp);

            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            return result.ToCount();
        }

        public async Task<LogStatistics> GetStatisticsAsync(
            DateTime from, DateTime to,
            IReadOnlyList<LogFilter>? filters = null,
            CancellationToken cancellationToken = default)
        {
            // Execute statistics query (counts by severity level)
            var statsNrql = NrqlQueryBuilder.BuildStatisticsNrql(from, to, filters);
            var statsResult = await Client.PostNrqlAsync(statsNrql, cancellationToken);

            return statsResult.ToStatistics(from, to);
        }

        public async Task<NewRelicDataConsumption> GetDataConsumptionAsync(
            DateTime from, DateTime to,
            CancellationToken cancellationToken = default)
        {
            var nrql = NrqlQueryBuilder.BuildConsumptionNrql(from, to);
            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            return result.ToDataConsumption(from, to);
        }

        public async Task<LogStorageInfo> GetStorageInfoAsync(CancellationToken cancellationToken = default)
        {
            // Use the NR-specific billing query but project the result into the
            // neutral LogStorageInfo shape so generic consumers can read it
            // without depending on NewRelicDataConsumption directly.
            var now = DateTime.UtcNow;
            var periodFrom = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodTo = now;

            var consumption = await GetDataConsumptionAsync(periodFrom, periodTo, cancellationToken);

            const long BYTES_PER_GIGABYTE = 1024L * 1024L * 1024L;
            const long FREE_TIER_GB = 100L;

            return new LogStorageInfo
            {
                UsedBytes = (long)(consumption.MonthToDateGigabytes * BYTES_PER_GIGABYTE),
                LimitBytes = FREE_TIER_GB * BYTES_PER_GIGABYTE,
                TotalEntries = null,
                PeriodFrom = consumption.StartDate,
                PeriodTo = consumption.EndDate,
                Breakdown = new Dictionary<string, long>
                {
                    ["Logs"] = (long)(consumption.LogsGigabytes * BYTES_PER_GIGABYTE),
                    ["Metrics"] = (long)(consumption.MetricsGigabytes * BYTES_PER_GIGABYTE),
                    ["Traces"] = (long)(consumption.TracesGigabytes * BYTES_PER_GIGABYTE),
                    ["Events"] = (long)(consumption.EventsGigabytes * BYTES_PER_GIGABYTE)
                }
            };
        }

        #endregion

        #region Properties

        private NewRelicHttpClient Client { get; }

        #endregion
    }
}
