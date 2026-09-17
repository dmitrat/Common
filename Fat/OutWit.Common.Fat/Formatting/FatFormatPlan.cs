using System;
using System.Numerics;
using OutWit.Common.Fat.Directories;

namespace OutWit.Common.Fat.Formatting
{
    /// <summary>
    /// The choices made before a volume is laid out: its kind, its cluster size, its label
    /// and its serial number.
    /// </summary>
    /// <remarks>
    /// The kind follows the SD card rules — FAT12 up to 8 MiB, FAT16 up to 512 MiB, FAT32 up
    /// to 32 GiB, exFAT above — with FAT16 stopped at 512 MiB, where Windows moves to FAT32.
    /// The cluster sizes follow Microsoft's defaults for FAT16 and FAT32 and mkfs.exfat's for
    /// exFAT; FAT12 takes the smallest that keeps it below 4085 clusters.
    /// </remarks>
    internal static class FatFormatPlan
    {
        #region Constants

        public const int MAX_LABEL_LENGTH = 11;

        private const long MIB = 1L << 20;

        private const long GIB = 1L << 30;

        private const long MAX_FAT12_BYTES = 8 * MIB;

        private const long MAX_FAT16_BYTES = 512 * MIB;

        private const long MAX_FAT32_BYTES = 32 * GIB;

        private const int MAX_FAT_CLUSTER_SECTORS = 128;

        private const int MAX_EXFAT_CLUSTER_BYTES = 32 * 1024 * 1024;

        private const string LABEL_SPECIAL = " !#$%&'()-@^_`{}~";

        #endregion

        #region Functions

        /// <summary>
        /// The kind a volume of this size is given when none is asked for.
        /// </summary>
        public static FatKind ChooseKind(long bytes)
        {
            return bytes switch
            {
                <= MAX_FAT12_BYTES => FatKind.Fat12,
                <= MAX_FAT16_BYTES => FatKind.Fat16,
                <= MAX_FAT32_BYTES => FatKind.Fat32,
                _ => FatKind.ExFat
            };
        }

        /// <summary>
        /// The cluster size a volume of this kind and size is given when none is asked for.
        /// </summary>
        public static int ChooseClusterSize(FatKind kind, long bytes, int sectorSize)
        {
            long size = kind switch
            {
                FatKind.Fat12 => (long)BitOperations.RoundUpToPowerOf2((ulong)Math.Max(1, (bytes + 4083) / 4084)),
                FatKind.Fat16 => bytes switch
                {
                    <= 16 * MIB => 1024,
                    <= 128 * MIB => 2048,
                    <= 256 * MIB => 4096,
                    <= 512 * MIB => 8192,
                    <= 1 * GIB => 16384,
                    _ => 32768
                },
                FatKind.Fat32 => bytes switch
                {
                    <= 260 * MIB => 512,
                    <= 8 * GIB => 4096,
                    <= 16 * GIB => 8192,
                    <= 32 * GIB => 16384,
                    _ => 32768
                },
                _ => bytes switch
                {
                    <= 256 * MIB => 4096,
                    <= 32 * GIB => 32768,
                    _ => 131072
                }
            };

            return (int)Math.Max(size, sectorSize);
        }

        /// <summary>
        /// Checks a cluster size and gives it in sectors.
        /// </summary>
        /// <exception cref="ArgumentException">The size is not a power of two, is below the sector size, or above what the kind allows.</exception>
        public static int SectorsPerCluster(FatKind kind, int clusterSize, int sectorSize)
        {
            if (clusterSize < sectorSize || !BitOperations.IsPow2(clusterSize))
                throw new ArgumentException($"A cluster of {clusterSize} bytes is not a power of two of at least {sectorSize}.", nameof(clusterSize));

            int sectors = clusterSize / sectorSize;
            bool isTooLarge = kind == FatKind.ExFat ? clusterSize > MAX_EXFAT_CLUSTER_BYTES : sectors > MAX_FAT_CLUSTER_SECTORS;
            if (isTooLarge)
                throw new ArgumentException($"A {kind} cluster of {clusterSize} bytes is larger than the format allows.", nameof(clusterSize));

            return sectors;
        }

        /// <summary>
        /// The label as it is stored, or <c>null</c> for none.
        /// </summary>
        /// <remarks>
        /// On FAT12/16/32 a label is up to 11 ASCII characters that an 8.3 name may hold, or
        /// spaces but not at its start, stored in upper case, as Windows stores it. On exFAT it is up to 11 UTF-16
        /// units without control characters.
        /// </remarks>
        /// <exception cref="ArgumentException">The label is too long or holds a character the kind does not allow.</exception>
        public static string? NormalizeLabel(FatKind kind, string? label)
        {
            if (label == null)
                return null;

            if (label.Length > MAX_LABEL_LENGTH)
                throw new ArgumentException($"A label has at most {MAX_LABEL_LENGTH} characters; '{label}' has {label.Length}.", nameof(label));

            if (kind == FatKind.ExFat)
            {
                foreach (char c in label)
                {
                    if (c < ' ')
                        throw new ArgumentException($"The label '{label}' holds a control character.", nameof(label));
                }

                return label.Length == 0 ? null : label;
            }

            var upper = label.ToCharArray();
            for (int i = 0; i < upper.Length; i++)
            {
                char c = upper[i] is >= 'a' and <= 'z' ? (char)(upper[i] - ('a' - 'A')) : upper[i];
                if (c is not (>= 'A' and <= 'Z' or >= '0' and <= '9') && !LABEL_SPECIAL.Contains(c))
                    throw new ArgumentException($"The label '{label}' holds '{upper[i]}', which a FAT label cannot.", nameof(label));
                upper[i] = c;
            }

            string trimmed = new string(upper).TrimEnd(' ');
            if (trimmed.StartsWith(' '))
                throw new ArgumentException($"The label '{label}' starts with a space, which a FAT name cannot.", nameof(label));
            return trimmed.Length == 0 ? null : trimmed;
        }

        /// <summary>
        /// The 11 bytes a FAT label is stored in: the label padded with spaces, or "NO NAME".
        /// </summary>
        public static byte[] LabelBytes(string? label)
        {
            var bytes = new byte[DirectorySlot.NAME_LENGTH];
            string text = (label ?? "NO NAME").PadRight(DirectorySlot.NAME_LENGTH);
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)text[i];
            return bytes;
        }

        /// <summary>
        /// A serial number made from the moment, as DOS made it: the date and the time folded
        /// into each other.
        /// </summary>
        public static uint MakeSerial(DateTimeOffset now)
        {
            var local = now.DateTime;
            uint date = (uint)(local.Day + (local.Month << 8) + ((local.Second + local.Millisecond / 10) << 16) + ((local.Millisecond % 100) << 24));
            uint time = (uint)(local.Year + (local.Minute << 16) + (local.Hour << 24));
            return date + time;
        }

        #endregion
    }
}
