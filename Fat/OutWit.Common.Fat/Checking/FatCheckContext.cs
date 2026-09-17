using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// What a check has found so far, and the volume it looks at.
    /// </summary>
    internal sealed class FatCheckContext
    {
        #region Constants

        /// <summary>
        /// How many problems of one kind are listed before the rest are only counted.
        /// </summary>
        public const int MAX_PER_KIND = 100;

        #endregion

        #region Fields

        private readonly List<FatProblem> m_problems = new();

        private readonly Dictionary<FatProblemKind, int> m_counts = new();

        #endregion

        #region Constructors

        public FatCheckContext(FatVolumeCore core, FatNamespace names)
        {
            Core = core;
            Names = names;
            Owners = new FatCheckOwners(core.Info.ClusterCount);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Notes a problem, unless its kind has already been listed often enough.
        /// </summary>
        public void Report(FatProblemKind kind, string? path, uint? cluster, string message)
        {
            int count = m_counts.TryGetValue(kind, out int seen) ? seen : 0;
            m_counts[kind] = count + 1;
            if (count < MAX_PER_KIND)
                m_problems.Add(new FatProblem { Kind = kind, Path = path, Cluster = cluster, Message = message });
        }

        /// <summary>
        /// Reports the cluster a walk found to belong to another owner, if it found one.
        /// </summary>
        /// <param name="walk">The walk.</param>
        /// <param name="path">What was walked, as the problem names it.</param>
        public void ReportShared(FatCheckWalk walk, string path)
        {
            if (walk.SharedWith == 0)
                return;

            string message = IsListed(FatProblemKind.CrossLinked)
                ? $"Cluster {walk.SharedCluster} of {path} belongs to {Owners.Describe(walk.SharedWith)} too."
                : string.Empty;
            Report(FatProblemKind.CrossLinked, path, walk.SharedCluster, message);
        }

        /// <summary>
        /// Whether a problem of this kind would still be listed, so that its message is worth making.
        /// </summary>
        public bool IsListed(FatProblemKind kind)
        {
            return !m_counts.TryGetValue(kind, out int seen) || seen < MAX_PER_KIND;
        }

        /// <summary>
        /// The report of everything found.
        /// </summary>
        public FatCheckReport ToReport()
        {
            var problems = m_problems.ToList();
            foreach (var (kind, count) in m_counts.Where(pair => pair.Value > MAX_PER_KIND))
                problems.Add(new FatProblem { Kind = kind, Message = $"{count - MAX_PER_KIND} more problems of this kind are not listed." });

            return new FatCheckReport
            {
                Problems = problems,
                Files = Files,
                Directories = Directories,
                UsedClusters = Owners.Used,
                LostClusters = LostClusters,
                BadClusters = BadClusters,
                FreeClusters = FreeClusters
            };
        }

        #endregion

        #region Properties

        public FatVolumeCore Core { get; }

        public FatNamespace Names { get; }

        public FatCheckOwners Owners { get; }

        public int Files { get; set; }

        public int Directories { get; set; }

        public long LostClusters { get; set; }

        public long BadClusters { get; set; }

        public long FreeClusters { get; set; }

        /// <summary>
        /// exFAT's allocation bitmap, once its chain is known to be whole; <c>null</c> until then.
        /// </summary>
        public ExFatRootEntry? Bitmap { get; set; }

        #endregion
    }
}
