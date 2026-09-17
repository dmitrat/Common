using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// Walks every directory, reading each slot, and gives every cluster an entry holds to
    /// that entry: the root's chain and exFAT's own structures first, then the tree.
    /// </summary>
    /// <remarks>
    /// A directory is read to its end even past a broken entry; a directory whose clusters are
    /// broken, shared or of the wrong number is not entered, so what lies in it counts as lost.
    /// </remarks>
    internal static class FatCheckTree
    {
        #region Functions

        public static async ValueTask CheckAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var core = context.Core;
            var root = context.Names.Root;
            var upcase = core.ExFat != null ? await FatCheckMetadata.CheckAsync(context, cancellationToken).ConfigureAwait(false) : null;

            bool isRootBroken = false;
            if (!core.HasFixedRoot)
            {
                var walk = await FatCheckChain.WalkAsync(context, root.Entry.FirstCluster, FatCheckOwners.ROOT, cancellationToken).ConfigureAwait(false);
                context.ReportShared(walk, FatPath.ROOT);
                isRootBroken = walk.Problem != null;
                if (isRootBroken)
                    context.Report(FatProblemKind.BrokenChain, FatPath.ROOT, root.Entry.FirstCluster, $"The root's chain {walk.Problem}.");
            }

            var walker = new FatCheckWalker(context, upcase);
            await walker.RunAsync(new FatCheckVisit(root, FatCheckOwners.ROOT, isRootBroken), cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
