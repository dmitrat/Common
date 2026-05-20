using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemHealth
{
    /// <summary>
    /// Describes a point-in-time machine health snapshot in a reusable form.
    /// </summary>
    public sealed class SystemHealthSnapshot : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemHealthSnapshot other)
                return false;

            return TimestampUtc.Is(other.TimestampUtc)
                   && CpuLoadPercent.Is(other.CpuLoadPercent, tolerance)
                   && AvailableRamMb.Is(other.AvailableRamMb)
                   && GpuLoadPercent.Is(other.GpuLoadPercent, tolerance)
                   && IsUserActive.Is(other.IsUserActive);
        }

        public override SystemHealthSnapshot Clone()
        {
            return new SystemHealthSnapshot
            {
                TimestampUtc = TimestampUtc,
                CpuLoadPercent = CpuLoadPercent,
                AvailableRamMb = AvailableRamMb,
                GpuLoadPercent = GpuLoadPercent,
                IsUserActive = IsUserActive
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// When the snapshot was taken.
        /// </summary>
        public DateTime TimestampUtc { get; init; }

        /// <summary>
        /// Current CPU load percentage.
        /// </summary>
        public double CpuLoadPercent { get; init; }

        /// <summary>
        /// Currently available physical memory in megabytes.
        /// </summary>
        public long AvailableRamMb { get; init; }

        /// <summary>
        /// Current GPU load percentage if measurable.
        /// </summary>
        public double? GpuLoadPercent { get; init; }

        /// <summary>
        /// Whether the user appears to be active on the current machine.
        /// </summary>
        public bool IsUserActive { get; init; }

        #endregion
    }
}
