using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Fat.Boot
{
    /// <summary>
    /// One used slot of a master boot record's partition table.
    /// </summary>
    public sealed class MbrPartitionEntry : ModelBase
    {
        #region Constants

        /// <summary>
        /// The type of a CHS-addressed extended partition.
        /// </summary>
        public const byte TYPE_EXTENDED_CHS = 0x05;

        /// <summary>
        /// The type of an LBA-addressed extended partition.
        /// </summary>
        public const byte TYPE_EXTENDED_LBA = 0x0F;

        /// <summary>
        /// The type of a Linux extended partition.
        /// </summary>
        public const byte TYPE_EXTENDED_LINUX = 0x85;

        /// <summary>
        /// The type that marks a protective MBR in front of a GUID partition table.
        /// </summary>
        public const byte TYPE_GPT_PROTECTIVE = 0xEE;

        #endregion

        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not MbrPartitionEntry other)
                return false;

            return Index.Is(other.Index)
                   && IsActive.Is(other.IsActive)
                   && Type.Is(other.Type)
                   && FirstSector.Is(other.FirstSector)
                   && SectorCount.Is(other.SectorCount);
        }

        /// <inheritdoc />
        public override MbrPartitionEntry Clone()
        {
            return new MbrPartitionEntry
            {
                Index = Index,
                IsActive = IsActive,
                Type = Type,
                FirstSector = FirstSector,
                SectorCount = SectorCount
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// The slot in the table, from 0 to 3.
        /// </summary>
        [ToString]
        public int Index { get; init; }

        /// <summary>
        /// Whether the slot is marked bootable.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// The partition type byte, such as 0x0C for FAT32 with LBA or 0x07 for exFAT.
        /// It is a hint only; the volume itself says what it is.
        /// </summary>
        [ToString(Format = "X2")]
        public byte Type { get; init; }

        /// <summary>
        /// The first sector of the partition on the disk.
        /// </summary>
        [ToString]
        public long FirstSector { get; init; }

        /// <summary>
        /// The length of the partition in sectors.
        /// </summary>
        [ToString]
        public long SectorCount { get; init; }

        /// <summary>
        /// Whether the slot points at a chain of logical partitions rather than a volume.
        /// </summary>
        public bool IsExtended => Type is TYPE_EXTENDED_CHS or TYPE_EXTENDED_LBA or TYPE_EXTENDED_LINUX;

        #endregion
    }
}
