using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// A cluster chain, followed only as far as it has been asked about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The chain is kept as runs of consecutive clusters, so a file written in one piece
    /// is one run however long it is, and finding the cluster at a position is a search
    /// over runs rather than a walk.
    /// </para>
    /// <para>
    /// A chain that reaches a free, bad or reserved entry, or leaves the volume, is corrupt.
    /// So is one that comes back to a cluster it has already passed: that is a loop, and it
    /// is caught at the first repeated cluster rather than after the volume's worth of them.
    /// </para>
    /// </remarks>
    internal sealed class ClusterChain
    {
        #region Constants

        private static readonly Comparer<(uint First, uint Last)> BY_FIRST_CLUSTER =
            Comparer<(uint First, uint Last)>.Create((left, right) => left.First.CompareTo(right.First));

        #endregion

        #region Fields

        private readonly FatTable m_table;

        private readonly List<ClusterRun> m_runs = new();

        private readonly SortedSet<(uint First, uint Last)> m_closedRuns = new(BY_FIRST_CLUSTER);

        private readonly HashSet<uint> m_closedStarts = new();

        private bool m_isComplete;

        #endregion

        #region Constructors

        /// <summary>
        /// Starts a chain.
        /// </summary>
        /// <exception cref="FatException">The first cluster is not on the volume.</exception>
        public ClusterChain(FatTable table, uint firstCluster)
        {
            if (!table.IsCluster(firstCluster))
                throw new FatException(FatErrorKind.Corrupt,
                    $"A chain starts at cluster {firstCluster}, outside clusters {FatTable.FIRST_CLUSTER} to {table.LastCluster}.");

            m_table = table;
            FirstCluster = firstCluster;
            m_runs.Add(new ClusterRun(0, firstCluster, 1));
        }

        #endregion

        #region Functions

        /// <summary>
        /// Finds the run holding the cluster at a position of the chain.
        /// </summary>
        /// <remarks>
        /// The chain is followed through <paramref name="ahead"/> clusters from the position,
        /// or to its end, so that a caller about to read that many clusters gets the whole
        /// of a contiguous stretch as one run and can read it in one request.
        /// </remarks>
        /// <param name="index">The position, from zero.</param>
        /// <param name="ahead">How many clusters from the position the caller wants, at least one.</param>
        /// <param name="cancellationToken">Cancels the walk.</param>
        /// <returns>The run, or <c>null</c> when the chain ends before the position.</returns>
        /// <exception cref="FatException">The chain is corrupt.</exception>
        public async ValueTask<ClusterRun?> FindAsync(long index, long ahead, CancellationToken cancellationToken)
        {
            long wanted = index + Math.Max(1, ahead);
            while (KnownCount < wanted && await ExtendAsync(cancellationToken).ConfigureAwait(false))
            {
            }

            return index < KnownCount ? m_runs[Search(index)] : null;
        }

        /// <summary>
        /// A complete chain made of runs just allocated.
        /// </summary>
        public static ClusterChain FromRuns(FatTable table, IReadOnlyList<ClusterRun> runs)
        {
            var chain = new ClusterChain(table, runs[0].FirstCluster) { m_isComplete = true };
            chain.m_runs.Clear();
            chain.m_runs.Add(runs[0]);
            chain.Append(runs.Skip(1));
            return chain;
        }

        /// <summary>
        /// Adds clusters just allocated after the end of a complete chain.
        /// </summary>
        /// <exception cref="InvalidOperationException">The chain has not been followed to its end.</exception>
        public void Append(IEnumerable<ClusterRun> runs)
        {
            if (!m_isComplete)
                throw new InvalidOperationException("Only a chain followed to its end can grow.");

            foreach (var run in runs)
            {
                var last = m_runs[^1];
                if (run.FirstCluster == last.LastCluster + 1)
                {
                    m_runs[^1] = last with { Count = last.Count + run.Count };
                    continue;
                }

                m_closedRuns.Add((last.FirstCluster, last.LastCluster));
                m_closedStarts.Add(last.FirstCluster);
                m_runs.Add(run with { FirstIndex = last.EndIndex });
            }
        }

        /// <summary>
        /// Cuts a complete chain to its first clusters and returns the rest, in chain order.
        /// </summary>
        /// <param name="count">How many clusters to keep, at least one and at most the chain's length.</param>
        public List<ClusterRun> Truncate(long count)
        {
            if (!m_isComplete || count < 1 || count > KnownCount)
                throw new InvalidOperationException($"A complete chain of {KnownCount} clusters cannot be cut to {count}.");

            int keep = Search(count - 1);
            var cut = m_runs[keep];
            var removed = new List<ClusterRun>();
            if (cut.EndIndex > count)
                removed.Add(new ClusterRun(count, cut.ClusterAt(count), cut.EndIndex - count));
            removed.AddRange(m_runs.GetRange(keep + 1, m_runs.Count - keep - 1));

            m_runs.RemoveRange(keep + 1, m_runs.Count - keep - 1);
            m_runs[keep] = cut with { Count = count - cut.FirstIndex };

            m_closedRuns.Clear();
            m_closedStarts.Clear();
            for (int i = 0; i < m_runs.Count - 1; i++)
            {
                m_closedRuns.Add((m_runs[i].FirstCluster, m_runs[i].LastCluster));
                m_closedStarts.Add(m_runs[i].FirstCluster);
            }

            return removed;
        }

        /// <summary>
        /// Follows the chain to its end.
        /// </summary>
        /// <returns>All its runs, in order.</returns>
        /// <exception cref="FatException">The chain is corrupt.</exception>
        public async ValueTask<IReadOnlyList<ClusterRun>> ReadAllAsync(CancellationToken cancellationToken)
        {
            while (await ExtendAsync(cancellationToken).ConfigureAwait(false))
            {
            }

            return m_runs.ToArray();
        }

        private async ValueTask<bool> ExtendAsync(CancellationToken cancellationToken)
        {
            if (m_isComplete)
                return false;

            var last = m_runs[^1];
            uint cluster = last.LastCluster;
            var entry = await m_table.GetAsync(cluster, cancellationToken).ConfigureAwait(false);

            switch (entry.Kind)
            {
                case FatTableEntryKind.End:
                    m_isComplete = true;
                    return false;

                case FatTableEntryKind.Link:
                    break;

                default:
                    throw new FatException(FatErrorKind.Corrupt,
                        $"The chain from cluster {FirstCluster} runs into an entry marked {entry.Kind.ToString().ToLowerInvariant()} " +
                        $"(0x{entry.Value:X}) at cluster {cluster}.");
            }

            uint next = entry.Next;
            if (next == cluster + 1)
            {
                if (m_closedStarts.Contains(next))
                    throw Loop(next);

                m_runs[^1] = last with { Count = last.Count + 1 };
                return true;
            }

            if ((next >= last.FirstCluster && next <= cluster) || IsInClosedRun(next))
                throw Loop(next);

            m_closedRuns.Add((last.FirstCluster, cluster));
            m_closedStarts.Add(last.FirstCluster);
            m_runs.Add(new ClusterRun(last.EndIndex, next, 1));
            return true;
        }

        /// <remarks>
        /// Runs never overlap, so only the closed run that starts last at or before the
        /// cluster can hold it.
        /// </remarks>
        private bool IsInClosedRun(uint cluster)
        {
            if (m_closedRuns.Count == 0 || m_closedRuns.Min.First > cluster)
                return false;

            var candidate = m_closedRuns.GetViewBetween(m_closedRuns.Min, (cluster, cluster)).Max;
            return candidate.Last >= cluster;
        }

        private FatException Loop(uint cluster)
        {
            return new FatException(FatErrorKind.Corrupt,
                $"The chain from cluster {FirstCluster} comes back to cluster {cluster}; it loops.");
        }

        private int Search(long index)
        {
            int low = 0;
            int high = m_runs.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (m_runs[middle].FirstIndex <= index)
                    low = middle;
                else
                    high = middle - 1;
            }

            return low;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The first cluster of the chain.
        /// </summary>
        public uint FirstCluster { get; }

        /// <summary>
        /// How many clusters of the chain are known so far.
        /// </summary>
        public long KnownCount => m_runs[^1].EndIndex;

        /// <summary>
        /// Whether the end of the chain has been reached.
        /// </summary>
        public bool IsComplete => m_isComplete;

        /// <summary>
        /// The last cluster known.
        /// </summary>
        public uint LastCluster => m_runs[^1].LastCluster;

        #endregion
    }
}
