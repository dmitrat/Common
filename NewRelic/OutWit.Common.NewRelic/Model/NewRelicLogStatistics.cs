using System;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.NewRelic.Model
{
    /// <summary>
    /// Statistics about log data in New Relic for a given time period.
    /// Focuses on log counts and severity distribution.
    /// For actual storage/billing data, use NewRelicDataConsumption instead.
    /// </summary>
    [MemoryPackable]
    public partial class NewRelicLogStatistics : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not NewRelicLogStatistics stats)
                return false;

            return From == stats.From
                   && To == stats.To
                   && TotalCount == stats.TotalCount
                   && ErrorCount == stats.ErrorCount
                   && WarningCount == stats.WarningCount
                   && InfoCount == stats.InfoCount
                   && DebugCount == stats.DebugCount;
        }

        public override NewRelicLogStatistics Clone()
        {
            return new NewRelicLogStatistics
            {
                From = From,
                To = To,
                TotalCount = TotalCount,
                ErrorCount = ErrorCount,
                WarningCount = WarningCount,
                InfoCount = InfoCount,
                DebugCount = DebugCount
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Start of the statistics period.
        /// </summary>
        [MemoryPackOrder(0)]
        public DateTime From { get; init; }

        /// <summary>
        /// End of the statistics period.
        /// </summary>
        [MemoryPackOrder(1)]
        public DateTime To { get; init; }

        /// <summary>
        /// Total number of log entries in the period.
        /// </summary>
        [MemoryPackOrder(2)]
        public long TotalCount { get; init; }

        /// <summary>
        /// Number of Error and Critical level logs.
        /// </summary>
        [MemoryPackOrder(3)]
        public long ErrorCount { get; init; }

        /// <summary>
        /// Number of Warning level logs.
        /// </summary>
        [MemoryPackOrder(4)]
        public long WarningCount { get; init; }

        /// <summary>
        /// Number of Information level logs.
        /// </summary>
        [MemoryPackOrder(5)]
        public long InfoCount { get; init; }

        /// <summary>
        /// Number of Debug and Trace level logs.
        /// </summary>
        [MemoryPackOrder(6)]
        public long DebugCount { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Error rate (percentage of error/critical logs).
        /// </summary>
        [MemoryPackIgnore]
        public double ErrorRate => TotalCount > 0 ? (ErrorCount * 100.0) / TotalCount : 0;

        /// <summary>
        /// Warning rate (percentage of warning logs).
        /// </summary>
        [MemoryPackIgnore]
        public double WarningRate => TotalCount > 0 ? (WarningCount * 100.0) / TotalCount : 0;

        /// <summary>
        /// Info rate (percentage of info logs).
        /// </summary>
        [MemoryPackIgnore]
        public double InfoRate => TotalCount > 0 ? (InfoCount * 100.0) / TotalCount : 0;

        /// <summary>
        /// Debug rate (percentage of debug/trace logs).
        /// </summary>
        [MemoryPackIgnore]
        public double DebugRate => TotalCount > 0 ? (DebugCount * 100.0) / TotalCount : 0;

        /// <summary>
        /// Duration of the statistics period in days.
        /// </summary>
        [MemoryPackIgnore]
        public double DurationDays => (To - From).TotalDays;

        /// <summary>
        /// Average logs per day.
        /// </summary>
        [MemoryPackIgnore]
        public double AverageLogsPerDay => DurationDays > 0 ? TotalCount / DurationDays : 0;

        /// <summary>
        /// Average errors per day.
        /// </summary>
        [MemoryPackIgnore]
        public double AverageErrorsPerDay => DurationDays > 0 ? ErrorCount / DurationDays : 0;

        /// <summary>
        /// Average warnings per day.
        /// </summary>
        [MemoryPackIgnore]
        public double AverageWarningsPerDay => DurationDays > 0 ? WarningCount / DurationDays : 0;

        #endregion
    }
}
