using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// The partition table in sector zero of a disk.
    /// </summary>
    internal sealed class MasterBootRecord
    {
        #region Constants

        public const int TABLE_OFFSET = 446;

        public const int ENTRY_SIZE = 16;

        public const int ENTRY_COUNT = 4;

        public const int DISK_SIGNATURE_OFFSET = 440;

        public const byte STATUS_INACTIVE = 0x00;

        public const byte STATUS_ACTIVE = 0x80;

        private const int ENTRY_STATUS = 0;

        private const int ENTRY_TYPE = 4;

        private const int ENTRY_FIRST_SECTOR = 8;

        private const int ENTRY_SECTOR_COUNT = 12;

        #endregion

        #region Constructors

        private MasterBootRecord(uint diskSignature, IReadOnlyList<MbrPartitionEntry> partitions)
        {
            DiskSignature = diskSignature;
            Partitions = partitions;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads the table, or returns <c>null</c> when the sector is not one.
        /// </summary>
        /// <remarks>
        /// The boot signature must be present and every slot's status byte must be 0x00
        /// or 0x80, as the Linux partition parser also requires; anything else is boot
        /// code, not a table. Slots of type zero are unused and left out.
        /// </remarks>
        public static MasterBootRecord? TryParse(ReadOnlySpan<byte> sector)
        {
            if (!BootSignature.IsPresent(sector))
                return null;

            var partitions = new List<MbrPartitionEntry>(ENTRY_COUNT);
            for (int index = 0; index < ENTRY_COUNT; index++)
            {
                var entry = sector.Slice(TABLE_OFFSET + index * ENTRY_SIZE, ENTRY_SIZE);
                byte status = entry[ENTRY_STATUS];
                if (status != STATUS_INACTIVE && status != STATUS_ACTIVE)
                    return null;

                byte type = entry[ENTRY_TYPE];
                if (type == 0)
                    continue;

                partitions.Add(new MbrPartitionEntry
                {
                    Index = index,
                    IsActive = status == STATUS_ACTIVE,
                    Type = type,
                    FirstSector = BinaryPrimitives.ReadUInt32LittleEndian(entry[ENTRY_FIRST_SECTOR..]),
                    SectorCount = BinaryPrimitives.ReadUInt32LittleEndian(entry[ENTRY_SECTOR_COUNT..])
                });
            }

            uint signature = BinaryPrimitives.ReadUInt32LittleEndian(sector[DISK_SIGNATURE_OFFSET..]);
            return new MasterBootRecord(signature, partitions);
        }

        #endregion

        #region Properties

        public uint DiskSignature { get; }

        public IReadOnlyList<MbrPartitionEntry> Partitions { get; }

        public bool IsGptProtective => Partitions.Any(partition => partition.Type == MbrPartitionEntry.TYPE_GPT_PROTECTIVE);

        #endregion
    }
}
