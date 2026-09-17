using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Values;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// What a disk holds: its partition table, if any, and the FAT and exFAT volumes on it.
    /// </summary>
    public sealed class FatDiskLayout : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatDiskLayout other)
                return false;

            return PartitionTable.Is(other.PartitionTable)
                   && DiskSignature.Is(other.DiskSignature)
                   && Partitions.Is(other.Partitions)
                   && Volumes.Is(other.Volumes)
                   && Problems.Is(other.Problems);
        }

        /// <inheritdoc />
        public override FatDiskLayout Clone()
        {
            return new FatDiskLayout
            {
                PartitionTable = PartitionTable,
                DiskSignature = DiskSignature,
                Partitions = Partitions.Select(partition => partition.Clone()).ToArray(),
                Volumes = Volumes.Select(volume => volume.Clone()).ToArray(),
                Problems = Problems.Select(problem => problem.Clone()).ToArray()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// How the disk is divided.
        /// </summary>
        [ToString]
        public PartitionTableKind PartitionTable { get; init; }

        /// <summary>
        /// The disk signature from the master boot record, or <c>null</c> without one.
        /// </summary>
        [ToString(Format = "X8")]
        public uint? DiskSignature { get; init; }

        /// <summary>
        /// The used partition table slots, whatever they hold; empty without a table.
        /// Extended partitions are listed but their logical partitions are not followed;
        /// slots of zero length are listed and skipped.
        /// </summary>
        public IReadOnlyList<MbrPartitionEntry> Partitions { get; init; } = Array.Empty<MbrPartitionEntry>();

        /// <summary>
        /// The FAT and exFAT volumes found, in partition table order.
        /// </summary>
        public IReadOnlyList<FatVolumeLocation> Volumes { get; init; } = Array.Empty<FatVolumeLocation>();

        /// <summary>
        /// The partitions that could not be used: slots that point outside the disk, and
        /// FAT or exFAT volumes that are broken, unsupported, or of another sector size.
        /// A partition that simply holds something else is neither a volume nor a problem.
        /// </summary>
        public IReadOnlyList<FatPartitionProblem> Problems { get; init; } = Array.Empty<FatPartitionProblem>();

        #endregion
    }
}
