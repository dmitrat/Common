using System;
using System.Collections.Generic;
using OutWit.Common.Enums;

namespace OutWit.Common.NewRelic.Model
{
    public sealed class NewRelicLogAttribute : StringEnum<NewRelicLogAttribute>
    {
        #region Static Constants

        public static readonly NewRelicLogAttribute Timestamp = new("timestamp");

        public static readonly NewRelicLogAttribute Level = new("level", "log.level");

        public static readonly NewRelicLogAttribute Message = new("message");

        public static readonly NewRelicLogAttribute Host = new("hostname", "host", "host.name");

        public static readonly NewRelicLogAttribute ServiceName = new("service.name", "serviceName");

        public static readonly NewRelicLogAttribute SourceContext = new("Message.Properties.SourceContext", "SourceContext", "logger", "logger.name");

        public static readonly NewRelicLogAttribute Environment = new("environment", "env");

        public static readonly NewRelicLogAttribute Exception = new("exception");

        public static readonly NewRelicLogAttribute TraceId = new("trace.id", "traceId");

        public static readonly NewRelicLogAttribute SpanId = new("span.id", "spanId");

        #endregion

        #region Constructors

        private NewRelicLogAttribute(string value, params string[] variations) 
            : base(value)
        {
            Variations = variations;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Determines whether the specified value matches this attribute's primary name or any of its variations (case-insensitive).
        /// </summary>
        /// <param name="value">The attribute name to compare against.</param>
        /// <returns><c>true</c> if the value matches; otherwise <c>false</c>.</returns>
        public bool Is(string? value)
        {
            if(string.IsNullOrWhiteSpace(value))
                return false;

            if (StringComparer.OrdinalIgnoreCase.Equals(Value, value))
                return true;

            foreach (var variation in Variations)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(variation, value))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified value starts with this attribute's primary name or any of its variations (case-insensitive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value starts with a matching prefix; otherwise <c>false</c>.</returns>
        public bool StartsWith(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.StartsWith(Value, StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var variation in Variations)
            {
                if (value.StartsWith(variation, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified value ends with this attribute's primary name or any of its variations (case-insensitive).
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns><c>true</c> if the value ends with a matching suffix; otherwise <c>false</c>.</returns>
        public bool EndsWith(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            if (value.EndsWith(Value, StringComparison.OrdinalIgnoreCase))
                return true;
            foreach (var variation in Variations)
            {
                if (value.EndsWith(variation, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection of alias names recognized as this attribute.
        /// </summary>
        public IReadOnlyCollection<string> Variations { get; }

        #endregion
    }
}
