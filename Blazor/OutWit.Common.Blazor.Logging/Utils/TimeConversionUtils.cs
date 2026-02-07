namespace OutWit.Common.Blazor.Logging.Utils
{
    /// <summary>
    /// Time conversion utilities for log toolbar.
    /// </summary>
    public static class TimeConversionUtils
    {
        /// <summary>
        /// Converts a nullable <see cref="TimeOnly"/> to a nullable <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan? AsTimeSpan(this TimeOnly? timeOnly)
        {
            return timeOnly.HasValue
                ? new TimeSpan(timeOnly.Value.Hour, timeOnly.Value.Minute, 0)
                : null;
        }

        /// <summary>
        /// Converts a nullable <see cref="TimeSpan"/> to a nullable <see cref="TimeOnly"/>.
        /// </summary>
        public static TimeOnly? AsTimeOnly(this TimeSpan? timeSpan)
        {
            return timeSpan.HasValue
                ? new TimeOnly(timeSpan.Value.Hours, timeSpan.Value.Minutes)
                : null;
        }
    }
}
