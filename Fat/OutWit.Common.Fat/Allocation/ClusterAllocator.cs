using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Allocation
{
    /// <summary>
    /// Finds, takes, links and releases clusters, and keeps the count of free ones.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A search starts right after the cluster a chain ends at, so a growing file stays in
    /// one piece where it can, and otherwise at the next-free hint, which moves forward as
    /// clusters are taken. Nothing scans the whole free map unless the volume is full or
    /// <see cref="CountFreeAsync"/> is asked for.
    /// </para>
    /// <para>
    /// What marks a cluster free is the format's business: the table itself on FAT12/16/32,
    /// the allocation bitmap on exFAT, where the table only links clusters of files that
    /// have a chain. A count loaded from the volume is kept up to date but never trusted to
    /// refuse an allocation; a count this class has taken itself is.
    /// </para>
    /// </remarks>
    internal abstract class ClusterAllocator
    {
        #region Constructors

        protected ClusterAllocator(FatTable table)
        {
            Table = table;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Takes clusters, and links them into a chain when asked to.
        /// </summary>
        /// <param name="after">The cluster the new clusters follow, or zero for a new chain.</param>
        /// <param name="count">How many clusters to take, at least one.</param>
        /// <param name="firstIndex">The chain position of the first new cluster, for the runs returned.</param>
        /// <param name="isChained">
        /// Whether the table links the new clusters to <paramref name="after"/> and to each
        /// other, ending the chain at the last. Always so on FAT12/16/32; on exFAT a file whose
        /// clusters follow each other needs no chain.
        /// </param>
        /// <param name="cancellationToken">Cancels the allocation.</param>
        /// <returns>The new clusters as runs, in chain order.</returns>
        /// <exception cref="FatException">
        /// <see cref="FatErrorKind.NoSpace"/>: there are not enough free clusters. Nothing is
        /// taken and the chain is left as it was.
        /// </exception>
        public async ValueTask<List<ClusterRun>> AllocateAsync(uint after, long count, long firstIndex, bool isChained,
            CancellationToken cancellationToken)
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (IsCountExact && FreeCount < count)
                throw NoSpace(count);

            var runs = new List<ClusterRun>();
            uint previous = after;
            uint start = after != 0 ? Next(after) : NextFree;
            try
            {
                for (long taken = 0; taken < count; taken++)
                {
                    uint cluster = await FindFreeOrThrowAsync(start, count, cancellationToken).ConfigureAwait(false);
                    await TakeAsync(cluster, isChained, cancellationToken).ConfigureAwait(false);

                    if (runs.Count > 0 && runs[^1].LastCluster + 1 == cluster)
                        runs[^1] = runs[^1] with { Count = runs[^1].Count + 1 };
                    else
                        runs.Add(new ClusterRun(firstIndex + taken, cluster, 1));

                    // Counted as soon as it is taken, so that a link that fails and the
                    // roll-back that gives the cluster back leave the count where it was.
                    FreeCount--;
                    IsChanged = true;

                    if (isChained && previous != 0)
                        await Table.SetAsync(previous, cluster, cancellationToken).ConfigureAwait(false);

                    previous = cluster;
                    start = Next(cluster);
                    NextFree = start;
                }
            }
            catch
            {
                await RollBackAsync(after, runs, isChained).ConfigureAwait(false);
                throw;
            }

            return runs;
        }

        /// <summary>
        /// Marks clusters free.
        /// </summary>
        public async ValueTask ReleaseAsync(IEnumerable<ClusterRun> runs, CancellationToken cancellationToken)
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            foreach (var run in runs)
            {
                for (long i = 0; i < run.Count; i++)
                {
                    await FreeAsync(run.ClusterAt(run.FirstIndex + i), cancellationToken).ConfigureAwait(false);
                    FreeCount++;
                    IsChanged = true;
                }
            }
        }

        /// <summary>
        /// Ends a chain at a cluster.
        /// </summary>
        public ValueTask EndChainAsync(uint cluster, CancellationToken cancellationToken)
        {
            return Table.SetAsync(cluster, Table.EndOfChainMark, cancellationToken);
        }

        /// <summary>
        /// Writes a chain through clusters already taken, in order, ending at the last — for
        /// exFAT clusters that had none, and for clusters taken without a chain so they could
        /// be filled before anything counts on them.
        /// </summary>
        /// <param name="runs">The clusters, in chain order.</param>
        /// <param name="after">The cluster the chain continues, or zero.</param>
        /// <param name="cancellationToken">Cancels the change.</param>
        public async ValueTask LinkAsync(IReadOnlyList<ClusterRun> runs, CancellationToken cancellationToken, uint after = 0)
        {
            uint previous = after;
            foreach (var run in runs)
            {
                for (long i = 0; i < run.Count; i++)
                {
                    uint cluster = run.ClusterAt(run.FirstIndex + i);
                    if (previous != 0)
                        await Table.SetAsync(previous, cluster, cancellationToken).ConfigureAwait(false);
                    previous = cluster;
                }
            }

            if (previous != 0)
                await EndChainAsync(previous, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Counts the free clusters by reading the whole free map — on a slow device, a long
        /// operation. The count is kept exact from then on.
        /// </summary>
        public async ValueTask<long> CountFreeAsync(CancellationToken cancellationToken)
        {
            await LoadAsync(cancellationToken).ConfigureAwait(false);
            long free = await CountAsync(cancellationToken).ConfigureAwait(false);

            IsChanged |= FreeCount != free;
            FreeCount = free;
            IsCountExact = true;
            return free;
        }

        /// <summary>
        /// Writes what the table and the free map hold back.
        /// </summary>
        public virtual ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            return Table.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Reads what the volume says about free clusters, once.
        /// </summary>
        protected abstract ValueTask LoadAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The first free cluster from <paramref name="start"/> on, coming round past the
        /// last; <c>null</c> when there is none.
        /// </summary>
        protected abstract ValueTask<uint?> FindFreeAsync(uint start, CancellationToken cancellationToken);

        /// <summary>
        /// Marks a free cluster used; on exFAT, ends a chain at it too when it is to be chained.
        /// </summary>
        protected abstract ValueTask TakeAsync(uint cluster, bool isChained, CancellationToken cancellationToken);

        /// <summary>
        /// Marks a cluster free.
        /// </summary>
        protected abstract ValueTask FreeAsync(uint cluster, CancellationToken cancellationToken);

        /// <summary>
        /// Counts the free clusters on the volume.
        /// </summary>
        protected abstract ValueTask<long> CountAsync(CancellationToken cancellationToken);

        /// <summary>
        /// The cluster after another, the first after the last.
        /// </summary>
        protected uint Next(uint cluster)
        {
            return cluster >= Table.LastCluster ? FatTable.FIRST_CLUSTER : cluster + 1;
        }

        private async ValueTask<uint> FindFreeOrThrowAsync(uint start, long wanted, CancellationToken cancellationToken)
        {
            if (await FindFreeAsync(start, cancellationToken).ConfigureAwait(false) is { } cluster)
                return cluster;

            FreeCount = 0;
            IsCountExact = true;
            throw NoSpace(wanted);
        }

        /// <summary>
        /// Gives back clusters just taken when what was to count on them could not be
        /// written: marks them free and, when they were chained, ends the chain again at
        /// <paramref name="after"/>.
        /// </summary>
        /// <remarks>
        /// Best effort: the failure that caused the roll-back is the one to report, and a
        /// device that fails here too leaves the rest to <see cref="FatChecker"/>.
        /// </remarks>
        /// <param name="after">The cluster the taken clusters were to follow, or zero.</param>
        /// <param name="runs">The clusters taken, in chain order.</param>
        /// <param name="isChained">Whether the chain was to run from <paramref name="after"/> through them.</param>
        public async ValueTask RollBackAsync(uint after, IReadOnlyList<ClusterRun> runs, bool isChained)
        {
            try
            {
                await ReleaseAsync(runs, CancellationToken.None).ConfigureAwait(false);
                if (isChained && after != 0)
                    await EndChainAsync(after, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The free map is left as far as the device allowed; FatChecker reports the rest.
            }
        }

        private static FatException NoSpace(long wanted)
        {
            return new FatException(FatErrorKind.NoSpace, $"The volume has no room for {wanted} more clusters.");
        }

        #endregion

        #region Properties

        /// <summary>
        /// The table that links clusters.
        /// </summary>
        public FatTable Table { get; }

        /// <summary>
        /// The number of free clusters, when known.
        /// </summary>
        public long? FreeCount { get; protected set; }

        /// <summary>
        /// Whether <see cref="FreeCount"/> was counted by this class rather than read from the volume.
        /// </summary>
        public bool IsCountExact { get; private set; }

        /// <summary>
        /// Where the next search for a free cluster starts.
        /// </summary>
        protected uint NextFree { get; set; } = FatTable.FIRST_CLUSTER;

        /// <summary>
        /// Whether the count or the hint changed since they were last written.
        /// </summary>
        protected bool IsChanged { get; set; }

        #endregion
    }
}
