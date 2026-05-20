using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes memory information in a reusable form.
    /// </summary>
    public sealed class SystemMemoryInfo : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemMemoryInfo other)
                return false;

            return TotalRamMb.Is(other.TotalRamMb);
        }

        public override SystemMemoryInfo Clone()
        {
            return new SystemMemoryInfo
            {
                TotalRamMb = TotalRamMb
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Total available RAM in megabytes.
        /// </summary>
        public long TotalRamMb { get; init; }

        #endregion
    }
}
