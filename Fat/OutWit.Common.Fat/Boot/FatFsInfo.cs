using System;
using System.Buffers.Binary;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The FSInfo sector of a FAT32 volume: a count of free clusters and where to start
    /// looking for one.
    /// </summary>
    /// <remarks>
    /// Both values are hints. A sector without its three signatures is not FSInfo and is
    /// neither trusted nor written.
    /// </remarks>
    internal static class FatFsInfo
    {
        #region Constants

        public const uint UNKNOWN = 0xFFFFFFFF;

        private const int OFFSET_LEAD_SIGNATURE = 0;

        private const int OFFSET_STRUCTURE_SIGNATURE = 484;

        private const int OFFSET_FREE_COUNT = 488;

        private const int OFFSET_NEXT_FREE = 492;

        private const int OFFSET_TRAIL_SIGNATURE = 508;

        private const uint LEAD_SIGNATURE = 0x41615252;

        private const uint STRUCTURE_SIGNATURE = 0x61417272;

        private const uint TRAIL_SIGNATURE = 0xAA550000;

        #endregion

        #region Functions

        /// <summary>
        /// Tells whether a sector carries the FSInfo signatures.
        /// </summary>
        public static bool IsValid(ReadOnlySpan<byte> sector)
        {
            return sector.Length >= BootSignature.MIN_SECTOR_SIZE
                   && ReadUInt32(sector, OFFSET_LEAD_SIGNATURE) == LEAD_SIGNATURE
                   && ReadUInt32(sector, OFFSET_STRUCTURE_SIGNATURE) == STRUCTURE_SIGNATURE
                   && ReadUInt32(sector, OFFSET_TRAIL_SIGNATURE) == TRAIL_SIGNATURE;
        }

        /// <summary>
        /// The free cluster count, or <see cref="UNKNOWN"/>.
        /// </summary>
        public static uint ReadFreeCount(ReadOnlySpan<byte> sector)
        {
            return ReadUInt32(sector, OFFSET_FREE_COUNT);
        }

        /// <summary>
        /// The cluster to start searching at, or <see cref="UNKNOWN"/>.
        /// </summary>
        public static uint ReadNextFree(ReadOnlySpan<byte> sector)
        {
            return ReadUInt32(sector, OFFSET_NEXT_FREE);
        }

        /// <summary>
        /// Stores both hints, leaving the rest of the sector as it is.
        /// </summary>
        public static void Write(Span<byte> sector, uint freeCount, uint nextFree)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(sector[OFFSET_FREE_COUNT..], freeCount);
            BinaryPrimitives.WriteUInt32LittleEndian(sector[OFFSET_NEXT_FREE..], nextFree);
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> sector, int offset)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(sector[offset..]);
        }

        #endregion
    }
}
