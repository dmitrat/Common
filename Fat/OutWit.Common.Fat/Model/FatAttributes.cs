using System;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// The attribute byte of a directory entry; the values are the on-disk bits.
    /// </summary>
    [Flags]
    public enum FatAttributes
    {
        /// <summary>
        /// No attribute.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Writes are not allowed.
        /// </summary>
        ReadOnly = 0x01,

        /// <summary>
        /// Left out of ordinary listings.
        /// </summary>
        Hidden = 0x02,

        /// <summary>
        /// Belongs to the operating system.
        /// </summary>
        System = 0x04,

        /// <summary>
        /// The entry is the volume label, not a file.
        /// </summary>
        VolumeLabel = 0x08,

        /// <summary>
        /// The entry is a directory.
        /// </summary>
        Directory = 0x10,

        /// <summary>
        /// Changed since it was last backed up.
        /// </summary>
        Archive = 0x20
    }
}
