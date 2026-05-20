using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Platform.Models.SystemInfo
{
    /// <summary>
    /// Describes storage information in a reusable form.
    /// </summary>
    public sealed class SystemStorageInfo : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SystemStorageInfo other)
                return false;

            return AvailableSpaceMb.Is(other.AvailableSpaceMb)
                   && StorageType.Is(other.StorageType);
        }

        public override SystemStorageInfo Clone()
        {
            return new SystemStorageInfo
            {
                AvailableSpaceMb = AvailableSpaceMb,
                StorageType = StorageType
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Available free space in megabytes.
        /// </summary>
        public long AvailableSpaceMb { get; init; }

        /// <summary>
        /// Storage type.
        /// </summary>
        public SystemStorageType StorageType { get; init; } = SystemStorageType.Unknown;

        #endregion
    }
}
