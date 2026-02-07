using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using OutWit.Common.NewRelic.Model;

namespace OutWit.Common.NewRelic.Nrql
{
    public static class NrqlResponseParser
    {
        /// <summary>
        /// Parses NerdGraph query results into an array of <see cref="NewRelicLogEntry"/> instances.
        /// </summary>
        /// <param name="me">The raw NerdGraph result rows.</param>
        /// <returns>An array of parsed log entries.</returns>
        public static NewRelicLogEntry[] ToLogEntries(this List<Dictionary<string, JsonElement>> me)
        {
             return me.Select(ParseLogEntry).ToArray();
        }

        /// <summary>
        /// Parses NerdGraph query results into an array of distinct string values.
        /// </summary>
        /// <param name="me">The raw NerdGraph result rows.</param>
        /// <returns>An array of distinct values sorted alphabetically.</returns>
        public static string[] ToDistinctValues(this List<Dictionary<string, JsonElement>> me)
        {
            return me.SelectMany(ParseDistinctResult).ToArray();
        }

        /// <summary>
        /// Extracts a count value from NerdGraph query results.
        /// </summary>
        /// <param name="me">The raw NerdGraph result rows.</param>
        /// <returns>The count value, or 0 if not found.</returns>
        public static long ToCount(this List<Dictionary<string, JsonElement>> me)
        {
            if (me.Count == 0)
                return 0;

            var row = me[0];
            if (row.TryGetValue("count", out var jsonElement) && jsonElement.ValueKind == JsonValueKind.Number)
            {
                if (jsonElement.TryGetInt64(out long count))
                {
                    return count;
                }
            }

            return 0;
        }

        /// <summary>
        /// Parses NerdGraph query results into log statistics with severity distribution.
        /// </summary>
        /// <param name="me">The raw NerdGraph result rows.</param>
        /// <param name="from">The start of the statistics period.</param>
        /// <param name="to">The end of the statistics period.</param>
        /// <returns>A <see cref="NewRelicLogStatistics"/> instance.</returns>
        public static NewRelicLogStatistics ToStatistics(this List<Dictionary<string, JsonElement>> me, DateTime from, DateTime to)
        {
            if (me.Count == 0)
            {
                return new NewRelicLogStatistics
                {
                    From = from,
                    To = to
                };
            }

            var row = me[0];

            return new NewRelicLogStatistics
            {
                From = from,
                To = to,
                TotalCount = GetInt64Value(row, "totalCount"),
                ErrorCount = GetInt64Value(row, "errorCount"),
                WarningCount = GetInt64Value(row, "warningCount"),
                InfoCount = GetInt64Value(row, "infoCount"),
                DebugCount = GetInt64Value(row, "debugCount")
            };
        }

        /// <summary>
        /// Parses NerdGraph query results into data consumption metrics with product-line breakdown.
        /// </summary>
        /// <param name="me">The raw NerdGraph result rows from NrConsumption query.</param>
        /// <param name="from">The start of the consumption period.</param>
        /// <param name="to">The end of the consumption period.</param>
        /// <returns>A <see cref="NewRelicDataConsumption"/> instance.</returns>
        public static NewRelicDataConsumption ToDataConsumption(this List<Dictionary<string, JsonElement>> me, DateTime from, DateTime to)
        {
            if (me == null || me.Count == 0)
            {
                return new NewRelicDataConsumption
                {
                    StartDate = from,
                    EndDate = to
                };
            }

            // Parse faceted results and accumulate totals
            double totalGB = 0;
            double logsGB = 0, metricsGB = 0, tracesGB = 0, infrastructureGB = 0, eventsGB = 0;

            foreach (var result in me)
            {
                // Get product line from facet or productLine field
                string? productLine = null;
                if (result.TryGetValue("facet", out var facetElement))
                {
                    productLine = facetElement.ValueKind == JsonValueKind.String 
                        ? facetElement.GetString() 
                        : facetElement.ValueKind == JsonValueKind.Array && facetElement.GetArrayLength() > 0
                            ? facetElement[0].GetString()
                            : null;
                }
                
                if (string.IsNullOrEmpty(productLine) && result.TryGetValue("productLine", out var productLineElement))
                {
                    productLine = productLineElement.ValueKind == JsonValueKind.String 
                        ? productLineElement.GetString() 
                        : null;
                }

                // Get usage value - try different field names
                double usage = 0;
                if (result.TryGetValue("sum.GigabytesIngested", out var usageElement) && usageElement.ValueKind == JsonValueKind.Number)
                {
                    usage = usageElement.GetDouble();
                }
                else if (result.TryGetValue("sum", out usageElement) && usageElement.ValueKind == JsonValueKind.Number)
                {
                    usage = usageElement.GetDouble();
                }
                else if (result.TryGetValue("GigabytesIngested", out usageElement) && usageElement.ValueKind == JsonValueKind.Number)
                {
                    usage = usageElement.GetDouble();
                }

                // Add to total
                totalGB += usage;

                // Map to our categories
                if (!string.IsNullOrEmpty(productLine))
                {
                    var productLower = productLine.ToLowerInvariant();
                    switch (productLower)
                    {
                        case "logs":
                        case "logging":
                            logsGB += usage;
                            break;
                        case "metrics":
                        case "metric":
                            metricsGB += usage;
                            break;
                        case "apm":
                        case "tracing":
                        case "traces":
                            tracesGB += usage;
                            break;
                        case "infrastructure":
                        case "infra":
                            infrastructureGB += usage;
                            break;
                        case "browser":
                        case "mobile":
                        case "serverless":
                        case "synthetics":
                        case "events":
                        case "insights":
                            eventsGB += usage;
                            break;
                        default:
                            // Unknown products go to events
                            eventsGB += usage;
                            break;
                    }
                }
            }

            // Calculate daily average and projections
            var duration = (to - from).TotalDays;
            if (duration <= 0) duration = 1;

            double dailyAverage = totalGB / duration;

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            double monthToDate = totalGB;
            double projected = totalGB;

            // If querying current month, calculate projection
            if (from <= monthStart && to >= now.Date)
            {
                var daysElapsed = (now.Date - monthStart).TotalDays + 1;
                var totalDaysInMonth = (monthEnd - monthStart).TotalDays + 1;
                
                if (daysElapsed > 0 && totalDaysInMonth > 0)
                {
                    projected = (totalGB / daysElapsed) * totalDaysInMonth;
                }
            }

            return new NewRelicDataConsumption
            {
                StartDate = from,
                EndDate = to,
                TotalGigabytes = totalGB,
                DailyAverageGigabytes = dailyAverage,
                MonthToDateGigabytes = monthToDate,
                ProjectedEndOfMonthGigabytes = projected,
                LogsGigabytes = logsGB,
                MetricsGigabytes = metricsGB,
                TracesGigabytes = tracesGB + infrastructureGB, // Combine APM and Infrastructure
                EventsGigabytes = eventsGB
            };
        }

        private static long GetInt64Value(Dictionary<string, JsonElement> row, string key)
        {
            if (row.TryGetValue(key, out var element) &&
                element.ValueKind == JsonValueKind.Number &&
                element.TryGetInt64(out var value))
            {
                return value;
            }

            return 0;
        }

        private static NewRelicLogEntry ParseLogEntry(Dictionary<string, JsonElement> row)
        {
            var entry = new NewRelicLogEntry();

            foreach (var kvp in row)
                AssignField(entry, kvp.Key, kvp.Value);

            if (entry.Timestamp == default)
                entry.Timestamp = DateTime.UtcNow;

            return entry;
        }

        private static List<string> ParseDistinctResult(Dictionary<string, JsonElement> row)
        {
            var list = new List<string>();

            foreach (var rowValue in row.Values)
            {
                if(rowValue.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var value in rowValue.EnumerateArray())
                {
                    var stringValue = value.GetString();
                    if(string.IsNullOrEmpty(stringValue))
                        continue;
                    list.Add(stringValue);
                }
            }

            return list.Distinct().OrderBy(x => x).ToList();
        }

        private static void AssignField(NewRelicLogEntry entry, string key, JsonElement value)
        {
            switch (key)
            {
                case var _ when NewRelicLogAttribute.Timestamp.Is(key):
                    entry.Timestamp = value.AsTimestamp();
                    break;
                case var _ when NewRelicLogAttribute.Message.Is(key):
                     entry.Message = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.Level.Is(key):
                    entry.Level = value.AsSeverity();
                    break;
                case var _ when NewRelicLogAttribute.SourceContext.Is(key):
                     entry.SourceContext = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.Exception.StartsWith(key):
                    entry.Exception = value.AsException(entry);
                    break;
                case var _ when NewRelicLogAttribute.ServiceName.Is(key):
                    entry.ServiceName = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.Host.Is(key):
                    entry.Host = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.Environment.Is(key):
                     entry.Environment = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.TraceId.Is(key):
                     entry.TraceId = value.AsString();
                    break;
                case var _ when NewRelicLogAttribute.SpanId.Is(key):
                     entry.SpanId = value.AsString();
                    break;
            }
        }

        private static NewRelicLogSeverity? AsSeverity(this JsonElement value)
        {
            return NewRelicLogSeverity.TryParse(value.AsString(), out var severity) ? severity : null;
        }

        private static DateTime AsTimestamp(this JsonElement value)
        {
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var unixMs)
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime
                : DateTime.UtcNow;
        }

        private static string? AsException(this JsonElement value, NewRelicLogEntry entry)
        {
            var exString = value.AsString();
            if (string.IsNullOrEmpty(exString))
                return null;

            return string.IsNullOrEmpty(entry.Exception)
                ? exString
                : entry.Exception + Environment.NewLine + exString;
        }
    }
}
