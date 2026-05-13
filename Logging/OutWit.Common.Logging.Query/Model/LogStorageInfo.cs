using System;
using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Logging.Query.Model
{
    /// <summary>
    /// Storage / quota state for a log backend. Each provider returns what it can
    /// observe; unknown fields stay <c>null</c>. Vendor-specific extras
    /// (NewRelic billing breakdown, Loki stream label sizes, etc.) live in
    /// <see cref="Breakdown"/>.
    /// </summary>
    [MemoryPackable]
    public partial class LogStorageInfo : ModelBase
    {
        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = 1E-07)
        {
            if (modelBase is not LogStorageInfo info)
                return false;

            return UsedBytes.Is(info.UsedBytes)
                   && LimitBytes.Is(info.LimitBytes)
                   && TotalEntries.Is(info.TotalEntries)
                   && PeriodFrom.Is(info.PeriodFrom)
                   && PeriodTo.Is(info.PeriodTo)
                   && DictEquals(Breakdown, info.Breakdown);
        }

        public override LogStorageInfo Clone()
        {
            return new LogStorageInfo
            {
                UsedBytes = UsedBytes,
                LimitBytes = LimitBytes,
                TotalEntries = TotalEntries,
                PeriodFrom = PeriodFrom,
                PeriodTo = PeriodTo,
                Breakdown = Breakdown?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        private static bool DictEquals(IReadOnlyDictionary<string, long>? a, IReadOnlyDictionary<string, long>? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out var bv)) return false;
                if (kvp.Value != bv) return false;
            }
            return true;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Bytes currently consumed by stored logs. <c>null</c> if the provider
        /// cannot determine it (e.g. Loki without admin metrics endpoint).
        /// </summary>
        [MemoryPackOrder(0)]
        public long? UsedBytes { get; set; }

        /// <summary>
        /// Storage quota in bytes. <c>null</c> if the backend has no fixed limit
        /// (e.g. self-hosted disk-bound stores) or the limit is not exposed.
        /// </summary>
        [MemoryPackOrder(1)]
        public long? LimitBytes { get; set; }

        /// <summary>
        /// Total number of log entries currently stored. <c>null</c> if not counted.
        /// </summary>
        [MemoryPackOrder(2)]
        public long? TotalEntries { get; set; }

        /// <summary>
        /// Start of the period these numbers apply to (for billing-based providers,
        /// typically the start of the current invoice period).
        /// </summary>
        [MemoryPackOrder(3)]
        public DateTime? PeriodFrom { get; set; }

        /// <summary>
        /// End of the period these numbers apply to.
        /// </summary>
        [MemoryPackOrder(4)]
        public DateTime? PeriodTo { get; set; }

        /// <summary>
        /// Vendor-specific breakdown — e.g. NewRelic
        /// <c>{ "Logs": 12, "Metrics": 7, "Traces": 3, "Events": 1 }</c>,
        /// File provider per-file sizes, Loki stream label volumes.
        /// </summary>
        [MemoryPackOrder(5)]
        public IReadOnlyDictionary<string, long>? Breakdown { get; set; }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Remaining bytes (<see cref="LimitBytes"/> &#8211; <see cref="UsedBytes"/>)
        /// when both are known; otherwise <c>null</c>.
        /// </summary>
        [MemoryPackIgnore]
        public long? RemainingBytes => LimitBytes.HasValue && UsedBytes.HasValue
            ? LimitBytes.Value - UsedBytes.Value
            : null;

        /// <summary>
        /// Used fraction of the quota in the range 0..1, when both numbers are known.
        /// </summary>
        [MemoryPackIgnore]
        public double? UsedFraction => LimitBytes.HasValue && UsedBytes.HasValue && LimitBytes.Value > 0
            ? (double)UsedBytes.Value / LimitBytes.Value
            : (double?)null;

        #endregion
    }
}
