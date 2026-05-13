using System.Linq;

namespace OutWit.Common.Logging.Query.Model
{
    /// <summary>
    /// Strongly-typed helpers for building common <see cref="LogFilter"/>s without magic strings.
    /// </summary>
    public static class LogFilters
    {
        #region Level filters

        /// <summary>
        /// Creates a filter that matches a specific log severity level.
        /// </summary>
        public static LogFilter LevelEquals(LogSeverity level)
        {
            return LogFilter.Eq(LogAttribute.Level, level.Value);
        }

        /// <summary>
        /// Creates a filter that matches any of the specified severity levels.
        /// </summary>
        public static LogFilter LevelIn(params LogSeverity[] levels)
        {
            return LogFilter.In(LogAttribute.Level, levels.Select(severity => severity.Value).ToArray());
        }

        /// <summary>
        /// level IN (minLevel, ..., Fatal) — convenience.
        /// </summary>
        public static LogFilter LevelAtLeast(LogSeverity minLevel)
        {
            var values = LogSeverity
                .LevelAtLeast(minLevel)
                .Select(severity => severity.Value)
                .ToArray();

            return LogFilter.In(LogAttribute.Level, values);
        }

        #endregion

        #region Message filters

        public static LogFilter MessageContains(string text)
        {
            return new LogFilter
            {
                Attribute = LogAttribute.Message,
                Operator = LogFilterOperator.Contains,
                Values = [text]
            };
        }

        public static LogFilter MessageNotContains(string text)
        {
            return new LogFilter
            {
                Attribute = LogAttribute.Message,
                Operator = LogFilterOperator.NotContains,
                Values = [text]
            };
        }

        #endregion

        #region Service / environment / context

        public static LogFilter ServiceEquals(string serviceName)
        {
            return LogFilter.Eq(LogAttribute.ServiceName, serviceName);
        }

        public static LogFilter ServiceIn(params string[] services)
        {
            return LogFilter.In(LogAttribute.ServiceName, services);
        }

        public static LogFilter EnvironmentEquals(string environment)
        {
            return LogFilter.Eq(LogAttribute.Environment, environment);
        }

        public static LogFilter SourceContextEquals(string sourceContext)
        {
            return LogFilter.Eq(LogAttribute.SourceContext, sourceContext);
        }

        public static LogFilter SourceContextIn(params string[] sourceContext)
        {
            return LogFilter.In(LogAttribute.SourceContext, sourceContext);
        }

        #endregion

        #region Tracing

        public static LogFilter TraceIdEquals(string traceId)
        {
            return LogFilter.Eq(LogAttribute.TraceId, traceId);
        }

        public static LogFilter SpanIdEquals(string spanId)
        {
            return LogFilter.Eq(LogAttribute.SpanId, spanId);
        }

        #endregion
    }
}
