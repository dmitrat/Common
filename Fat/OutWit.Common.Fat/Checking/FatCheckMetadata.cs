using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// exFAT's allocation bitmaps and up-case table: that the root has them, and that their
    /// chains hold what their entries record.
    /// </summary>
    internal static class FatCheckMetadata
    {
        #region Constants

        public const string BITMAP_OWNER = "(allocation bitmap)";

        public const string SECOND_BITMAP_OWNER = "(second allocation bitmap)";

        public const string UPCASE_OWNER = "(up-case table)";

        #endregion

        #region Functions

        /// <summary>
        /// Gives the bitmaps and the up-case table their clusters, and sets
        /// <see cref="FatCheckContext.Bitmap"/> when the active bitmap can be read.
        /// </summary>
        /// <returns>The up-case table, or <c>null</c> when it cannot be read; its problem is then reported.</returns>
        /// <remarks>A volume with two tables, TexFAT, has a bitmap for each; the other is claimed too, and not compared.</remarks>
        public static async ValueTask<ExFatUpcaseTable?> CheckAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var exFat = context.Core.ExFat!;
            ExFatRootEntry bitmap;
            ExFatRootEntry upcase;
            try
            {
                bitmap = await exFat.GetBitmapAsync(cancellationToken).ConfigureAwait(false);
                upcase = await exFat.GetUpcaseEntryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.Corrupt)
            {
                context.Report(FatProblemKind.Metadata, FatPath.ROOT, null, error.Message);
                return null;
            }

            if (await ClaimAsync(context, bitmap, BITMAP_OWNER, cancellationToken).ConfigureAwait(false))
                context.Bitmap = bitmap;
            await ClaimOtherBitmapsAsync(context, bitmap, cancellationToken).ConfigureAwait(false);
            if (!await ClaimAsync(context, upcase, UPCASE_OWNER, cancellationToken).ConfigureAwait(false))
                return null;

            try
            {
                return await exFat.GetUpcaseTableAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.Corrupt)
            {
                context.Report(FatProblemKind.Metadata, UPCASE_OWNER, upcase.FirstCluster, error.Message);
                return null;
            }
        }

        private static async ValueTask ClaimOtherBitmapsAsync(FatCheckContext context, ExFatRootEntry active, CancellationToken cancellationToken)
        {
            var root = (FatDirectoryExFat)context.Core.OpenDirectory(context.Names.Root);
            var (_, critical) = await root.ReadRootEntriesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var entry in critical)
            {
                if (entry.Type == ExFatEntry.ALLOCATION_BITMAP && entry.Slot != active.Slot)
                    await ClaimAsync(context, entry, SECOND_BITMAP_OWNER, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <returns>Whether the chain is whole, its own, and as long as the entry needs.</returns>
        private static async ValueTask<bool> ClaimAsync(FatCheckContext context, ExFatRootEntry entry, string name, CancellationToken cancellationToken)
        {
            var core = context.Core;
            var walk = await FatCheckChain.WalkAsync(context, entry.FirstCluster, context.Owners.Add(name, 0), cancellationToken).ConfigureAwait(false);
            context.ReportShared(walk, name);
            long needed = core.ClustersFor((long)ulong.Min(entry.DataLength, long.MaxValue));
            if (walk.IsWhole && walk.Count == needed)
                return true;

            if (walk.Problem != null || walk.SharedWith == 0)
                context.Report(FatProblemKind.Metadata, name, entry.FirstCluster, walk.Problem != null
                    ? $"The chain of {name} {walk.Problem}."
                    : $"{name} records {entry.DataLength} bytes, which need {needed} clusters; its chain has {walk.Count}.");
            return false;
        }

        #endregion
    }
}
