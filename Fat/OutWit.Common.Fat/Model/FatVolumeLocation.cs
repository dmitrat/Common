using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// A FAT or exFAT volume found on a disk, and where it lies.
    /// </summary>
    /// <remarks>
    /// Open it with <c>new BlockDevicePartition(disk, FirstSector, SectorCount)</c>.
    /// </remarks>
    public sealed class FatVolumeLocation : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatVolumeLocation other)
                return false;

            return FirstSector.Is(other.FirstSector)
                   && SectorCount.Is(other.SectorCount)
                   && IsSameModel(Partition, other.Partition, tolerance)
                   && Volume.Is(other.Volume, tolerance);
        }

        /// <inheritdoc />
        public override FatVolumeLocation Clone()
        {
            return new FatVolumeLocation
            {
                Partition = Partition?.Clone(),
                FirstSector = FirstSector,
                SectorCount = SectorCount,
                Volume = Volume.Clone()
            };
        }

        private static bool IsSameModel(ModelBase? me, ModelBase? other, double tolerance)
        {
            if (me == null && other == null)
                return true;

            if (me == null || other == null)
                return false;

            return me.Is(other, tolerance);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The partition holding the volume, or <c>null</c> when the volume spans the whole disk.
        /// </summary>
        [ToString]
        public MbrPartitionEntry? Partition { get; init; }

        /// <summary>
        /// The disk sector the volume starts at.
        /// </summary>
        [ToString]
        public long FirstSector { get; init; }

        /// <summary>
        /// The sectors available to the volume: the partition, or the whole disk.
        /// </summary>
        [ToString]
        public long SectorCount { get; init; }

        /// <summary>
        /// The volume's layout.
        /// </summary>
        [ToString]
        public required FatVolumeInfo Volume { get; init; }

        #endregion
    }
}
