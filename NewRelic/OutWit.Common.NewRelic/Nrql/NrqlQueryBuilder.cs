using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.NewRelic.Nrql
{
    public static class NrqlQueryBuilder
    {
        private static readonly Dictionary<NewRelicLogFilterOperator, Func<NewRelicLogFilter, string>> FILTER_CLAUSE_GENERATORS =
            new()
            {
                { NewRelicLogFilterOperator.Equals, f => BuildSimpleOperator(f, "=") },
                { NewRelicLogFilterOperator.NotEquals, f => BuildSimpleOperator(f, "!=") },
                { NewRelicLogFilterOperator.GreaterThan, f => BuildSimpleOperator(f, ">") },
                { NewRelicLogFilterOperator.GreaterOrEqual, f => BuildSimpleOperator(f, ">=") },
                { NewRelicLogFilterOperator.LessThan, f => BuildSimpleOperator(f, "<") },
                { NewRelicLogFilterOperator.LessOrEqual, f => BuildSimpleOperator(f, "<=") },
                { NewRelicLogFilterOperator.Contains, f => $"{f.Attribute} LIKE '%{NrqlStringUtils.EscapeSingleQuoted(f.Values[0])}%'" },
                { NewRelicLogFilterOperator.NotContains, f => $"{f.Attribute} NOT LIKE '%{NrqlStringUtils.EscapeSingleQuoted(f.Values[0])}%'" },
                { NewRelicLogFilterOperator.In, f => $"{f.Attribute} IN ({string.Join(", ", f.Values.Select(NrqlStringUtils.ToNrqlLiteralFromString))})" }
            };

        /// <summary>
        /// Builds a NRQL query string from the specified log query parameters.
        /// </summary>
        /// <param name="me">The log query containing filters, time range, pagination and sort order.</param>
        /// <returns>A NRQL query string ready to be sent to the NerdGraph API.</returns>
        public static string BuildNrql(this NewRelicLogQuery me)
        {
            var sb = new StringBuilder();
             sb.Append("SELECT * FROM Log");

            var where = BuildWhereClause(me);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" WHERE ");
                 sb.Append(where);
            }

            var (since, until) = BuildTimeWindow(me);
            if (!string.IsNullOrEmpty(since))
            {
                sb.Append(" SINCE ");
                 sb.Append(since);
            }
            if (!string.IsNullOrEmpty(until))
            {
                sb.Append(" UNTIL ");
                 sb.Append(until);
            }

            sb.Append(" ORDER BY timestamp ");
             sb.Append(me.SortOrder == NewRelicLogSortOrder.Ascending ? "ASC" : "DESC");

            sb.Append(" LIMIT ");
            sb.Append(me.PageSize);
            if (me.Offset > 0)
            {
                sb.Append(" OFFSET ");
                 sb.Append(me.Offset);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds a NRQL query for fetching distinct values (uniques).
        /// Example: SELECT uniques(SourceContext) FROM Log SINCE ... UNTIL ... WHERE ... LIMIT 1000
        /// </summary>
        public static string BuildDistinctNrql(NewRelicLogAttribute attribute, DateTime from, DateTime to, IEnumerable<NewRelicLogFilter>? filters, int limit)
        {
            var sb = new StringBuilder();

            // Construct SELECT uniques(Attr)
            sb.Append($"SELECT uniques({attribute}) FROM Log");

            // Build WHERE clause (reuse existing logic if possible, or build manually using existing helpers)
            // Need to wrap filters in a temporary Query object or extract BuildWhereClause logic.
            // Let's refactor BuildWhereClause to be static and accept filters/search directly or create a dummy query object.

            // Reusing existing logic by creating a lightweight DTO:
            var dummyQuery = new NewRelicLogQuery
            {
                Filters = filters?.ToArray(),
                From = from,
                To = to
            };

            var where = BuildWhereClause(dummyQuery);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" WHERE ");
                sb.Append(where);
            }

            var (since, until) = BuildTimeWindow(dummyQuery);
            if (!string.IsNullOrEmpty(since))
            {
                sb.Append(" SINCE ");
                sb.Append(since);
            }
            if (!string.IsNullOrEmpty(until))
            {
                sb.Append(" UNTIL ");
                sb.Append(until);
            }

            sb.Append(" LIMIT ");
            sb.Append(limit);

            return sb.ToString();
        }

        public static string BuildCountNrql(NewRelicLogQuery me, DateTime targetTimestamp)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT count(*) AS 'count' FROM Log");

            var where = BuildWhereClause(me);

            sb.Append(" WHERE ");
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(where);
                sb.Append(" AND ");
            }
            var op = me.SortOrder == NewRelicLogSortOrder.Descending ? ">" : "<";

            var timeFilterValue = NrqlStringUtils.ToNrqlEpoch(targetTimestamp);

            sb.Append($"timestamp {op} {timeFilterValue}");

            var (since, until) = BuildTimeWindow(me);
            if (!string.IsNullOrEmpty(since))
            {
                sb.Append(" SINCE ");
                sb.Append(since);
            }
            if (!string.IsNullOrEmpty(until))
            {
                sb.Append(" UNTIL ");
                sb.Append(until);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds NRQL query for collecting log statistics.
        /// Returns counts by severity level (no size estimation - use GetDataConsumptionAsync for storage metrics).
        /// </summary>
        public static string BuildStatisticsNrql(DateTime from, DateTime to, IEnumerable<NewRelicLogFilter>? filters = null)
        {
            var sb = new StringBuilder();

            // Select count aggregates by severity level
            sb.Append("SELECT ");
            sb.Append("count(*) AS 'totalCount', ");

            // Count by level using filter()
            sb.Append("filter(count(*), WHERE level IN ('Error', 'Critical', 'Fatal')) AS 'errorCount', ");
            sb.Append("filter(count(*), WHERE level = 'Warning') AS 'warningCount', ");
            sb.Append("filter(count(*), WHERE level = 'Information') AS 'infoCount', ");
            sb.Append("filter(count(*), WHERE level IN ('Debug', 'Trace')) AS 'debugCount' ");

            sb.Append("FROM Log");

            // Build WHERE clause
            var dummyQuery = new NewRelicLogQuery
            {
                Filters = filters?.ToArray(),
                From = from,
                To = to
            };

            var where = BuildWhereClause(dummyQuery);
            if (!string.IsNullOrWhiteSpace(where))
            {
                sb.Append(" WHERE ");
                sb.Append(where);
            }

            var (since, until) = BuildTimeWindow(dummyQuery);
            if (!string.IsNullOrEmpty(since))
            {
                sb.Append(" SINCE ");
                sb.Append(since);
            }
            if (!string.IsNullOrEmpty(until))
            {
                sb.Append(" UNTIL ");
                sb.Append(until);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Builds NRQL query for getting data consumption from NrConsumption event.
        /// Uses FACET productLine to get breakdown by product type.
        /// Example: FROM NrConsumption SELECT sum(GigabytesIngested) SINCE '...' UNTIL '...' FACET productLine LIMIT 100
        /// </summary>
        public static string BuildConsumptionNrql(DateTime from, DateTime to)
        {
            var fromStr = from.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            return $"FROM NrConsumption SELECT sum(GigabytesIngested) SINCE '{fromStr}' UNTIL '{toStr}' FACET productLine LIMIT 100";
        }

        private static string BuildWhereClause(NewRelicLogQuery request)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.FullTextSearch))
            {
                var value = NrqlStringUtils.EscapeSingleQuoted(request.FullTextSearch);
                parts.Add($"message LIKE '%{value}%'");
            }

            if (request.Filters is { Length: > 0 })
            {
                foreach (var f in request.Filters)
                    parts.Add(BuildFilterClause(f));
            }

            return string.Join(" AND ", parts);
        }

        private static string BuildFilterClause(NewRelicLogFilter filter)
        {
            if (filter.Values is null || filter.Values.Length == 0)
                throw new InvalidOperationException($"Filter for '{filter.Attribute}' has no values.");

            if (FILTER_CLAUSE_GENERATORS.TryGetValue(filter.Operator, out var generator))
                return generator(filter);

            throw new NotSupportedException($"Unsupported operator: {filter.Operator}"); 
        }

        private static string BuildSimpleOperator(NewRelicLogFilter filter, string op)
        {
            return $"{filter.Attribute} {op} {NrqlStringUtils.ToNrqlLiteralFromString(filter.Values[0])}";
        }

        private static (string since, string until) BuildTimeWindow(NewRelicLogQuery request)
        {
            if (request.From is { } f && request.To is { } t)
            {
                var since = NrqlStringUtils.ToNrqlTimestamp(f);
                var until = NrqlStringUtils.ToNrqlTimestamp(t);
                return (since, until);
            }

            if (request.Lookback is { } lookback)
            {
                var to = DateTime.UtcNow;
                var from = to - lookback;
                return (NrqlStringUtils.ToNrqlTimestamp(from), NrqlStringUtils.ToNrqlTimestamp(to));
            }

            return (string.Empty, string.Empty);
        }
    }
}
