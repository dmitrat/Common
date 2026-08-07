using System;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// Detects a system clock that has been moved backwards.
    /// <para>
    /// With no network to ask what time it is, an expiry date is only as good as
    /// the clock the product reads. Winding it back is the obvious and easiest
    /// way to keep an expired licence alive, so the runtime remembers the
    /// furthest instant it has already seen and refuses to be taken further back
    /// than that.
    /// </para>
    /// </summary>
    public sealed class ClockGuard
    {
        #region Constants

        /// <summary>
        /// How far backwards is still considered ordinary. NTP corrections,
        /// a virtual machine resuming from a snapshot and a laptop returning
        /// from a badly-configured timezone all move a clock backwards by
        /// legitimate amounts; a guard that fired on those would generate far
        /// more support than the tampering it prevents.
        /// </summary>
        public const int DEFAULT_TOLERANCE_HOURS = 24;

        #endregion

        #region Fields

        private readonly TimeSpan m_tolerance;

        #endregion

        #region Constructors

        public ClockGuard()
            : this(TimeSpan.FromHours(DEFAULT_TOLERANCE_HOURS))
        {
        }

        public ClockGuard(TimeSpan tolerance)
        {
            m_tolerance = tolerance;
        }

        #endregion

        #region Functions

        /// <summary>
        /// True when <paramref name="utcNow"/> is further back than the tolerance
        /// allows, given what has already been observed.
        /// </summary>
        public bool IsTampered(LicenseStoreState state, DateTime utcNow)
        {
            if (state.HighWaterMarkUtc == default)
                return false;

            return utcNow < state.HighWaterMarkUtc - m_tolerance;
        }

        /// <summary>
        /// Advances the high-water mark, and seeds the first-run stamp on a
        /// fresh installation. Never moves the mark backwards.
        /// </summary>
        public LicenseStoreState Observe(LicenseStoreState state, DateTime utcNow)
        {
            if (state.FirstRunUtc == default)
                state.FirstRunUtc = utcNow;

            if (utcNow > state.HighWaterMarkUtc)
                state.HighWaterMarkUtc = utcNow;

            return state;
        }

        #endregion
    }
}
