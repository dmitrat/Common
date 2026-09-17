using System;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// How a volume is mounted.
    /// </summary>
    public sealed class FatVolumeOptions
    {
        #region Constants

        /// <summary>
        /// The options used when none are given.
        /// </summary>
        public static readonly FatVolumeOptions DEFAULT = new();

        #endregion

        #region Properties

        /// <summary>
        /// Whether the device stays open when the volume is disposed. Off by default: the
        /// volume owns the device.
        /// </summary>
        public bool LeaveOpen { get; init; }

        /// <summary>
        /// Where the times of new and written entries come from; the system clock by
        /// default. FAT records the local time the provider gives.
        /// </summary>
        public TimeProvider Clock { get; init; } = TimeProvider.System;

        #endregion
    }
}
