using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// Follows a table chain, or counts out a run the table does not describe, giving each
    /// cluster to an owner as it goes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="ClusterChain"/>, which throws at the first fault, this keeps what it
    /// reached, so the clusters before a break still count as used.
    /// </para>
    /// <para>
    /// A walk stops at the first cluster that already has an owner — its own, which means the
    /// chain loops, or another's, whose chain the rest is, as <c>fsck.fat</c> cuts the second
    /// of two files that share clusters. So every cluster is followed once whatever the
    /// damage, and nothing is kept per chain.
    /// </para>
    /// </remarks>
    internal static class FatCheckChain
    {
        #region Functions

        /// <summary>
        /// Follows the chain from <paramref name="first"/>.
        /// </summary>
        public static async ValueTask<FatCheckWalk> WalkAsync(FatCheckContext context, uint first, int owner, CancellationToken cancellationToken)
        {
            var table = context.Core.Table;
            if (!table.IsCluster(first))
                return new FatCheckWalk(0, $"starts at cluster {first}, outside clusters {FatTable.FIRST_CLUSTER} to {table.LastCluster}", 0, 0);

            long count = 0;
            uint cluster = first;
            while (true)
            {
                int previous = context.Owners.Claim(cluster, owner);
                if (previous == owner)
                    return new FatCheckWalk(count, $"comes back to cluster {cluster}", 0, 0);
                if (previous != 0)
                    return new FatCheckWalk(count, null, cluster, previous);

                count++;
                var entry = await table.GetAsync(cluster, cancellationToken).ConfigureAwait(false);
                switch (entry.Kind)
                {
                    case FatTableEntryKind.End:
                        return new FatCheckWalk(count, null, 0, 0);
                    case FatTableEntryKind.Link:
                        cluster = entry.Next;
                        break;
                    default:
                        return new FatCheckWalk(count,
                            $"runs into an entry marked {entry.Kind.ToString().ToLowerInvariant()} (0x{entry.Value:X}) at cluster {cluster}", 0, 0);
                }
            }
        }

        /// <summary>
        /// Counts out <paramref name="count"/> consecutive clusters from <paramref name="first"/>.
        /// </summary>
        public static FatCheckWalk Run(FatCheckContext context, uint first, long count, int owner)
        {
            var table = context.Core.Table;
            if (!table.IsCluster(first) || count > table.LastCluster - first + 1L)
                return new FatCheckWalk(0, $"records {count} consecutive clusters from cluster {first}, past the end of the volume", 0, 0);

            for (long i = 0; i < count; i++)
            {
                uint cluster = (uint)(first + i);
                int previous = context.Owners.Claim(cluster, owner);
                if (previous != 0)
                    return new FatCheckWalk(i, null, cluster, previous);
            }

            return new FatCheckWalk(count, null, 0, 0);
        }

        #endregion
    }
}
