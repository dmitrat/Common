using System;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// How a volume is formatted.
    /// </summary>
    /// <remarks>
    /// Everything left unset is chosen from the size of the volume, the way card makers and
    /// Windows choose it: FAT12 up to 8 MiB, FAT16 up to 512 MiB, FAT32 up to 32 GiB and exFAT
    /// above, each with the cluster size usual for its size.
    /// </remarks>
    public sealed class FatFormatOptions
    {
        #region Constants

        /// <summary>
        /// The options used when none are given.
        /// </summary>
        public static readonly FatFormatOptions DEFAULT = new();

        #endregion

        #region Properties

        /// <summary>
        /// The kind of volume, or <c>null</c> to choose it by size.
        /// </summary>
        public FatKind? Kind { get; init; }

        /// <summary>
        /// The cluster size in bytes — a power of two, at least the sector size — or
        /// <c>null</c> for the usual size.
        /// </summary>
        public int? ClusterSize { get; init; }

        /// <summary>
        /// The volume label, or <c>null</c> for none: up to 11 characters, on FAT12/16/32 in
        /// ASCII and stored in upper case.
        /// </summary>
        public string? Label { get; init; }

        /// <summary>
        /// The volume serial number, or <c>null</c> to make one from the clock.
        /// </summary>
        public uint? VolumeSerial { get; init; }

        /// <summary>
        /// The number of allocation tables on FAT12/16/32, 1 or 2; exFAT has one.
        /// </summary>
        public int FatCount { get; init; } = 2;

        /// <summary>
        /// The size of the fixed root directory of FAT12 and FAT16, in entries; rounded up to
        /// whole sectors.
        /// </summary>
        public int RootEntries { get; init; } = 512;

        /// <summary>
        /// Where the times of the label come from, and the serial number when none is given.
        /// </summary>
        public TimeProvider Clock { get; init; } = TimeProvider.System;

        #endregion
    }
}
