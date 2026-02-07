using System;
using System.Linq;

namespace OutWit.Common.NewRelic.Model
{
    /// <summary>
    /// Strongly-typed helpers for building log filters without magic strings.
    /// </summary>
    public static class NewRelicLogFilters
    {

        #region Level filters

        /// <summary>
        /// Creates a filter that matches a specific log severity level.
        /// </summary>
        /// <param name="level">The severity level to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter LevelEquals(NewRelicLogSeverity level)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.Level, level.Value);
        }

        /// <summary>
        /// Creates a filter that matches any of the specified severity levels.
        /// </summary>
        /// <param name="levels">The severity levels to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter LevelIn(params NewRelicLogSeverity[] levels)
        {
            return NewRelicLogFilter.In(NewRelicLogAttribute.Level, levels.Select(severity=>severity.Value).ToArray());
        }


        /// <summary>
        /// level IN (Warning, Error, Critical) etc.
        /// </summary>
        public static NewRelicLogFilter LevelAtLeast(NewRelicLogSeverity minLevel)
        {
            var values = NewRelicLogSeverity
                .LevelAtLeast(minLevel)
                .Select(severity => severity.Value)
                .ToArray();

            return NewRelicLogFilter.In(NewRelicLogAttribute.Level, values);
        }

        #endregion

        #region Message filters

        /// <summary>
        /// Creates a filter that matches messages containing the specified text.
        /// </summary>
        /// <param name="text">The text to search for in the message.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter MessageContains(string text)
        {
            return new NewRelicLogFilter
            {
                Attribute = NewRelicLogAttribute.Message,
                Operator = NewRelicLogFilterOperator.Contains,
                Values = [text]
            };
        }

        /// <summary>
        /// Creates a filter that excludes messages containing the specified text.
        /// </summary>
        /// <param name="text">The text to exclude from the message.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter MessageNotContains(string text)
        {
            return new NewRelicLogFilter
            {
                Attribute = NewRelicLogAttribute.Message,
                Operator = NewRelicLogFilterOperator.NotContains,
                Values = [text]
            };
        }

        #endregion

        #region Service / environment / context

        /// <summary>
        /// Creates a filter that matches a specific service name.
        /// </summary>
        /// <param name="serviceName">The service name to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter ServiceEquals(string serviceName)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.ServiceName, serviceName);
        }

        /// <summary>
        /// Creates a filter that matches any of the specified service names.
        /// </summary>
        /// <param name="services">The service names to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter ServiceIn(params string[] services)
        {
            return NewRelicLogFilter.In(NewRelicLogAttribute.ServiceName, services);
        }

        /// <summary>
        /// Creates a filter that matches a specific environment.
        /// </summary>
        /// <param name="environment">The environment name to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter EnvironmentEquals(string environment)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.Environment, environment);
        }

        /// <summary>
        /// Creates a filter that matches a specific source context (logger name).
        /// </summary>
        /// <param name="sourceContext">The source context to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter SourceContextEquals(string sourceContext)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.SourceContext, sourceContext);
        }

        /// <summary>
        /// Creates a filter that matches any of the specified source contexts.
        /// </summary>
        /// <param name="sourceContext">The source contexts to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter SourceContextIn(params string[] sourceContext)
        {
            return NewRelicLogFilter.In(NewRelicLogAttribute.SourceContext, sourceContext);
        }

        #endregion

        #region Tracing

        /// <summary>
        /// Creates a filter that matches a specific distributed trace identifier.
        /// </summary>
        /// <param name="traceId">The trace ID to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter TraceIdEquals(string traceId)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.TraceId, traceId);
        }

        /// <summary>
        /// Creates a filter that matches a specific span identifier.
        /// </summary>
        /// <param name="spanId">The span ID to match.</param>
        /// <returns>A new <see cref="NewRelicLogFilter"/> instance.</returns>
        public static NewRelicLogFilter SpanIdEquals(string spanId)
        {
            return NewRelicLogFilter.Eq(NewRelicLogAttribute.SpanId, spanId);
        }

        #endregion
    }
}
