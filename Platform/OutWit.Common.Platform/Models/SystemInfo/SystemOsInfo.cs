using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes operating-system information in a reusable form.
    /// </summary>
    public sealed class SystemOsInfo : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemOsInfo other)
                return false;

            return Platform.Is(other.Platform)
                   && Version.Is(other.Version);
        }

        public override SystemOsInfo Clone()
        {
            return new SystemOsInfo
            {
                Platform = Platform,
                Version = Version
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Operating-system platform kind.
        /// </summary>
        public PlatformKind Platform { get; init; } = PlatformKind.Unknown;

        /// <summary>
        /// Operating-system version text.
        /// </summary>
        public string Version { get; init; } = string.Empty;

        #endregion
    }
}
