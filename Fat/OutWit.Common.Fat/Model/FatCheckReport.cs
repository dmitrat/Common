using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Fat.Model
{
    /// <summary>
    /// What <see cref="FatChecker"/> found on a volume.
    /// </summary>
    public sealed class FatCheckReport : ModelBase
    {
        #region Model Base

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FatCheckReport other)
                return false;

            return Problems.Is(other.Problems)
                   && Files.Is(other.Files)
                   && Directories.Is(other.Directories)
                   && UsedClusters.Is(other.UsedClusters)
                   && LostClusters.Is(other.LostClusters)
                   && BadClusters.Is(other.BadClusters)
                   && FreeClusters.Is(other.FreeClusters);
        }

        /// <inheritdoc />
        public override FatCheckReport Clone()
        {
            return new FatCheckReport
            {
                Problems = Problems.Select(problem => problem.Clone()).ToArray(),
                Files = Files,
                Directories = Directories,
                UsedClusters = UsedClusters,
                LostClusters = LostClusters,
                BadClusters = BadClusters,
                FreeClusters = FreeClusters
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What is wrong, in the order it was found; empty for a clean volume. Problems of
        /// one kind stop being listed after the first hundred; one more of that kind then
        /// says how many were left out.
        /// </summary>
        public IReadOnlyList<FatProblem> Problems { get; init; } = Array.Empty<FatProblem>();

        /// <summary>
        /// Whether nothing is wrong.
        /// </summary>
        [ToString]
        public bool IsClean => Problems.Count == 0;

        /// <summary>
        /// The files found.
        /// </summary>
        [ToString]
        public int Files { get; init; }

        /// <summary>
        /// The directories found, the root not counted.
        /// </summary>
        [ToString]
        public int Directories { get; init; }

        /// <summary>
        /// The clusters that belong to a file, a directory, or exFAT's bitmap and up-case
        /// table. On a clean volume these, the free and the bad clusters add up to
        /// <see cref="FatVolumeInfo.ClusterCount"/>.
        /// </summary>
        public long UsedClusters { get; init; }

        /// <summary>
        /// The clusters marked in use that belong to nothing.
        /// </summary>
        public long LostClusters { get; init; }

        /// <summary>
        /// The clusters the table marks bad; FAT12/16/32 only.
        /// </summary>
        public long BadClusters { get; init; }

        /// <summary>
        /// The clusters the allocation table, or exFAT's bitmap, marks free. On a damaged
        /// volume some of them may belong to a file as well.
        /// </summary>
        [ToString]
        public long FreeClusters { get; init; }

        #endregion
    }
}
