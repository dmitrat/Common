using System;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.NewRelic.Model
{
    /// <summary>
    /// Data consumption statistics from New Relic billing/usage API.
    /// Shows actual storage usage and limits - what you see in the UI.
    /// </summary>
    [MemoryPackable]
    public partial class NewRelicDataConsumption : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not NewRelicDataConsumption consumption)
                return false;

            return StartDate == consumption.StartDate
                   && EndDate == consumption.EndDate
                   && TotalGigabytes.Is(consumption.TotalGigabytes, tolerance)
                   && DailyAverageGigabytes.Is(consumption.DailyAverageGigabytes, tolerance)
                   && MonthToDateGigabytes.Is(consumption.MonthToDateGigabytes, tolerance)
                   && ProjectedEndOfMonthGigabytes.Is(consumption.ProjectedEndOfMonthGigabytes, tolerance);
        }

        public override NewRelicDataConsumption Clone()
        {
            return new NewRelicDataConsumption
            {
                StartDate = StartDate,
                EndDate = EndDate,
                TotalGigabytes = TotalGigabytes,
                DailyAverageGigabytes = DailyAverageGigabytes,
                MonthToDateGigabytes = MonthToDateGigabytes,
                ProjectedEndOfMonthGigabytes = ProjectedEndOfMonthGigabytes,
                LogsGigabytes = LogsGigabytes,
                MetricsGigabytes = MetricsGigabytes,
                TracesGigabytes = TracesGigabytes,
                EventsGigabytes = EventsGigabytes
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Start date of the consumption period.
        /// </summary>
        [MemoryPackOrder(0)]
        public DateTime StartDate { get; init; }

        /// <summary>
        /// End date of the consumption period.
        /// </summary>
        [MemoryPackOrder(1)]
        public DateTime EndDate { get; init; }

        /// <summary>
        /// Total data ingested in gigabytes for the period.
        /// This is the ACTUAL storage metric from New Relic billing.
        /// </summary>
        [MemoryPackOrder(2)]
        public double TotalGigabytes { get; init; }

        /// <summary>
        /// Daily average ingestion in gigabytes.
        /// </summary>
        [MemoryPackOrder(3)]
        public double DailyAverageGigabytes { get; init; }

        /// <summary>
        /// Month-to-date consumption in gigabytes (for current month).
        /// </summary>
        [MemoryPackOrder(4)]
        public double MonthToDateGigabytes { get; init; }

        /// <summary>
        /// Projected end-of-month consumption in gigabytes.
        /// </summary>
        [MemoryPackOrder(5)]
        public double ProjectedEndOfMonthGigabytes { get; init; }

        /// <summary>
        /// Logs data consumption in gigabytes.
        /// </summary>
        [MemoryPackOrder(6)]
        public double LogsGigabytes { get; init; }

        /// <summary>
        /// Metrics data consumption in gigabytes.
        /// </summary>
        [MemoryPackOrder(7)]
        public double MetricsGigabytes { get; init; }

        /// <summary>
        /// Traces data consumption in gigabytes.
        /// </summary>
        [MemoryPackOrder(8)]
        public double TracesGigabytes { get; init; }

        /// <summary>
        /// Events data consumption in gigabytes.
        /// </summary>
        [MemoryPackOrder(9)]
        public double EventsGigabytes { get; init; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Days in the period.
        /// </summary>
        [MemoryPackIgnore]
        public double DurationDays => (EndDate - StartDate).TotalDays;

        /// <summary>
        /// Percentage of free tier limit (100 GB) used.
        /// </summary>
        [MemoryPackIgnore]
        public double FreeTierUsagePercent => (MonthToDateGigabytes / 100.0) * 100.0;

        /// <summary>
        /// Remaining gigabytes in free tier (100 GB limit).
        /// </summary>
        [MemoryPackIgnore]
        public double FreeTierRemainingGB => Math.Max(0, 100.0 - MonthToDateGigabytes);

        /// <summary>
        /// Will exceed free tier this month?
        /// </summary>
        [MemoryPackIgnore]
        public bool WillExceedFreeTier => ProjectedEndOfMonthGigabytes > 100.0;

        /// <summary>
        /// Projected overage in GB (if exceeding free tier).
        /// </summary>
        [MemoryPackIgnore]
        public double ProjectedOverageGB => Math.Max(0, ProjectedEndOfMonthGigabytes - 100.0);

        #endregion
    }
}
