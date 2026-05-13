using System.Collections.Generic;
using System.Linq;
using System.Text;
using OutWit.Common.Logging.Query.Model;

namespace OutWit.Common.Logging.Loki.LogQL
{
    /// <summary>
    /// Translates neutral <see cref="LogQuery"/>/<see cref="LogFilter"/> into LogQL.
    ///
    /// <para>
    /// Output shape: <c>{labelSelectors} | json [| filterClauses] [|~ "freeText"]</c>.
    /// Filters on known stream labels (e.g. <c>service.name</c>) are folded into the
    /// stream selector; everything else lands behind <c>| json</c> as label filters,
    /// so the log line is parsed as JSON before the filter is applied.
    /// </para>
    /// </summary>
    public static class LogQlBuilder
    {
        #region Constants

        /// <summary>
        /// Stream-label names that Loki indexes natively. Filters targeting these
        /// move into the stream selector (cheap); others live behind <c>| json</c>.
        /// </summary>
        private static readonly HashSet<string> STREAM_LABELS = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "service_name", "service.name", "level", "env", "environment", "host", "hostname"
        };

        #endregion

        #region Functions

        /// <summary>
        /// Builds the <c>query_range</c> LogQL expression for the given query plus
        /// default labels from <see cref="LokiOptions.DefaultLabels"/>.
        /// </summary>
        public static string BuildRangeQuery(LogQuery query,
            IReadOnlyDictionary<string, string>? defaultLabels)
        {
            var labelSelectors = new List<(string Key, string Op, string Value)>();
            var jsonFilters = new List<(string Key, string Op, string Value)>();

            if (defaultLabels != null)
            {
                foreach (var kvp in defaultLabels)
                    labelSelectors.Add((NormalizeLabel(kvp.Key), "=", kvp.Value));
            }

            if (query.Filters != null)
            {
                foreach (var f in query.Filters)
                {
                    var bucket = STREAM_LABELS.Contains(f.Attribute) ? labelSelectors : jsonFilters;
                    var op = TranslateOp(f.Operator);
                    foreach (var v in f.Values)
                    {
                        if (f.Operator == LogFilterOperator.In)
                        {
                            // OR-style "in" — model each value separately. Loki's
                            // stream selector supports regex with "=~", which we use here.
                            bucket = STREAM_LABELS.Contains(f.Attribute) ? labelSelectors : jsonFilters;
                            // fall through to the loop body for simple equality on each value
                            bucket.Add((NormalizeLabel(f.Attribute), "=", v));
                        }
                        else
                        {
                            bucket.Add((NormalizeLabel(f.Attribute), op, v));
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            AppendStreamSelector(sb, labelSelectors);

            if (jsonFilters.Count > 0 || !string.IsNullOrEmpty(query.FullTextSearch))
                sb.Append(" | json");

            foreach (var f in jsonFilters)
                sb.Append(" | ").Append(f.Key).Append(' ').Append(f.Op).Append(' ').Append(Quote(f.Value));

            if (!string.IsNullOrEmpty(query.FullTextSearch))
                sb.Append(" |~ ").Append(Quote(query.FullTextSearch!));

            return sb.ToString();
        }

        /// <summary>
        /// Builds a <c>sum by (level) (count_over_time({selectors} | json [range]))</c>
        /// expression. Used by <c>GetStatisticsAsync</c>.
        /// </summary>
        public static string BuildLevelHistogram(IReadOnlyList<LogFilter>? filters,
            IReadOnlyDictionary<string, string>? defaultLabels,
            System.TimeSpan range)
        {
            var labelSelectors = new List<(string Key, string Op, string Value)>();
            if (defaultLabels != null)
            {
                foreach (var kvp in defaultLabels)
                    labelSelectors.Add((NormalizeLabel(kvp.Key), "=", kvp.Value));
            }

            if (filters != null)
            {
                foreach (var f in filters)
                {
                    if (!STREAM_LABELS.Contains(f.Attribute)) continue;
                    foreach (var v in f.Values)
                        labelSelectors.Add((NormalizeLabel(f.Attribute), "=", v));
                }
            }

            var sb = new StringBuilder("sum by (level) (count_over_time(");
            AppendStreamSelector(sb, labelSelectors);
            sb.Append(" | json [").Append(FormatRange(range)).Append("]))");
            return sb.ToString();
        }

        #endregion

        #region Tools

        private static void AppendStreamSelector(StringBuilder sb,
            List<(string Key, string Op, string Value)> selectors)
        {
            sb.Append('{');
            for (int i = 0; i < selectors.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var s = selectors[i];
                sb.Append(s.Key).Append(s.Op).Append(Quote(s.Value));
            }
            sb.Append('}');
        }

        private static string TranslateOp(LogFilterOperator op) => op switch
        {
            LogFilterOperator.Equals          => "=",
            LogFilterOperator.NotEquals       => "!=",
            LogFilterOperator.Contains        => "=~",
            LogFilterOperator.NotContains     => "!~",
            LogFilterOperator.In              => "=",   // handled per-value above
            LogFilterOperator.GreaterThan     => ">",
            LogFilterOperator.GreaterOrEqual  => ">=",
            LogFilterOperator.LessThan        => "<",
            LogFilterOperator.LessOrEqual     => "<=",
            _                                 => "="
        };

        /// <summary>
        /// LogQL label names cannot contain a dot — translate "service.name" → "service_name".
        /// </summary>
        private static string NormalizeLabel(string name) => name.Replace('.', '_');

        private static string Quote(string value)
        {
            // LogQL strings are Go-style; escape \" and \\
            var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "\"" + escaped + "\"";
        }

        private static string FormatRange(System.TimeSpan range)
        {
            if (range.TotalDays >= 1)    return ((long)range.TotalDays) + "d";
            if (range.TotalHours >= 1)   return ((long)range.TotalHours) + "h";
            if (range.TotalMinutes >= 1) return ((long)range.TotalMinutes) + "m";
            return ((long)range.TotalSeconds) + "s";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Exposed for the provider/tests — names of labels Loki indexes as stream selectors.
        /// </summary>
        public static IReadOnlyCollection<string> StreamLabels => STREAM_LABELS.ToList();

        #endregion
    }
}
