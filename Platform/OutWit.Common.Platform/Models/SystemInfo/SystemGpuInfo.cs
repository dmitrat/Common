using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes GPU information in a reusable form.
    /// </summary>
    public sealed class SystemGpuInfo : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemGpuInfo other)
                return false;

            return GpuType.Is(other.GpuType)
                   && ModelName.Is(other.ModelName)
                   && VRamMb.Is(other.VRamMb)
                   && SupportedFeatures.Is(other.SupportedFeatures);
        }

        public override SystemGpuInfo Clone()
        {
            return new SystemGpuInfo
            {
                GpuType = GpuType,
                ModelName = ModelName,
                VRamMb = VRamMb,
                SupportedFeatures = SupportedFeatures
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// GPU type.
        /// </summary>
        public SystemGpuType GpuType { get; init; } = SystemGpuType.Unknown;

        /// <summary>
        /// GPU model name.
        /// </summary>
        public string ModelName { get; init; } = string.Empty;

        /// <summary>
        /// Dedicated video memory in megabytes.
        /// </summary>
        public long VRamMb { get; init; }

        /// <summary>
        /// Supported GPU compute/display features.
        /// </summary>
        public SystemGpuFeatures SupportedFeatures { get; init; } = SystemGpuFeatures.None;

        #endregion
    }
}
