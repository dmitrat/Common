using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OutWit.Common.NewRelic.Interfaces;
using OutWit.Common.NewRelic.Model;
using OutWit.Common.NewRelic.Nrql;

namespace OutWit.Common.NewRelic
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

        public async Task<NewRelicLogPage> QueryAsync(NewRelicLogQuery query, CancellationToken cancellationToken = default)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));

            query = Client.Validate(query);
            var nrql = query.BuildNrql();
            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            NewRelicLogEntry[] entries = result.ToLogEntries();

            return new NewRelicLogPage
            {
                Items = entries,
                Offset = query.Offset,
                PageSize = query.PageSize!.Value,
                HasMore = entries.Length == query.PageSize.Value
            };
        }

        public async Task<NewRelicLogPage> GetLogsAsync(DateTime from, DateTime to, IReadOnlyList<NewRelicLogFilter>? filters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new NewRelicLogQuery
            {
                From = from,
                To = to,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<NewRelicLogPage> GetRecentLogsAsync(TimeSpan lookback, IReadOnlyList<NewRelicLogFilter>? filters = null, int? pageSize = null, int offset = 0,
            CancellationToken cancellationToken = default)
        {
            var query = new NewRelicLogQuery
            {
                Lookback = lookback,
                Filters = filters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<NewRelicLogPage> SearchAsync(string text, TimeSpan lookback, IReadOnlyList<NewRelicLogFilter>? extraFilters = null, int? pageSize = null,
            int offset = 0, CancellationToken cancellationToken = default)
        {
            var query = new NewRelicLogQuery
            {
                Lookback = lookback,
                FullTextSearch = text,
                Filters = extraFilters?.ToArray(),
                PageSize = pageSize,
                Offset = offset,
                SortOrder = NewRelicLogSortOrder.Descending
            };

            return await QueryAsync(query, cancellationToken);
        }

        public async Task<IReadOnlyList<string>> GetDistinctValuesAsync(DateTime from, DateTime to, NewRelicLogAttribute attribute, IReadOnlyList<NewRelicLogFilter>? filters = null,
            int limit = 1000, CancellationToken cancellationToken = default)
        {
            var nrql = NrqlQueryBuilder.BuildDistinctNrql(attribute, from, to, filters, limit);

            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            return result.ToDistinctValues();
        }

        public async Task<long> FindOffsetAsync(NewRelicLogQuery query, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            var nrql = NrqlQueryBuilder.BuildCountNrql(query, timestamp);

            var result = await Client.PostNrqlAsync(nrql, cancellationToken);

            return result.ToCount();
        }

        public async Task<NewRelicLogStatistics> GetStatisticsAsync(
            DateTime from, DateTime to,
            IReadOnlyList<NewRelicLogFilter>? filters = null,
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

        #endregion

        #region Properties

        private NewRelicHttpClient Client { get; }

        #endregion
    }
}
