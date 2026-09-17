using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.ExFat;

namespace OutWit.Common.Fat.Checking
{
    /// <summary>
    /// What the table or the bitmap says of each cluster, against what the tree walk found
    /// in it; and the counts the volume keeps of free clusters.
    /// </summary>
    /// <remarks>
    /// Runs after <see cref="FatCheckTree"/>. A cluster marked used that nothing holds is
    /// lost, unless the table marks it bad — on exFAT a bad cluster is marked used in the
    /// bitmap too, so nothing takes it. On exFAT a cluster something holds that the bitmap
    /// marks free is a mismatch; the bitmap is compared only when its own chain is whole.
    /// Free and bad clusters are counted as the table or the bitmap marks them, whether or
    /// not something holds them: on FAT a held cluster marked so has already broken its
    /// chain. FSInfo's count is compared with the clusters the table marks free.
    /// </remarks>
    internal static class FatCheckAllocation
    {
        #region Constants

        private const int PERCENT_IN_USE = 112;

        private const byte PERCENT_UNKNOWN = 0xFF;

        private const byte MAX_PERCENT = 100;

        #endregion

        #region Functions

        public static async ValueTask CheckAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            if (context.Core.ExFat != null)
            {
                await CheckBitmapAsync(context, cancellationToken).ConfigureAwait(false);
                return;
            }

            await CheckTableAsync(context, cancellationToken).ConfigureAwait(false);
            await CheckFsInfoAsync(context, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask CheckTableAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var table = context.Core.Table;
            var owners = context.Owners;
            uint? firstLost = null;
            for (uint cluster = FatTable.FIRST_CLUSTER; cluster <= table.LastCluster; cluster++)
            {
                var entry = await table.GetAsync(cluster, cancellationToken).ConfigureAwait(false);
                switch (entry.Kind)
                {
                    case FatTableEntryKind.Free:
                        context.FreeClusters++;
                        break;
                    case FatTableEntryKind.Bad:
                        context.BadClusters++;
                        break;
                    default:
                        if (owners.IsOwned(cluster))
                            break;
                        context.LostClusters++;
                        firstLost ??= cluster;
                        break;
                }
            }

            if (firstLost != null)
                context.Report(FatProblemKind.LostClusters, null, firstLost,
                    $"{context.LostClusters} clusters are marked used and belong to nothing; the first is cluster {firstLost}.");
        }

        private static async ValueTask CheckFsInfoAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var info = context.Core.Info;
            if (info.Kind != FatKind.Fat32 || info.FsInfoSector is not { } fsInfoSector)
                return;

            var sector = new byte[info.SectorSize];
            await context.Core.Device.ReadAsync(fsInfoSector, sector, cancellationToken).ConfigureAwait(false);
            if (!FatFsInfo.IsValid(sector))
                return;

            uint recorded = FatFsInfo.ReadFreeCount(sector);
            if (recorded != FatFsInfo.UNKNOWN && recorded != context.FreeClusters)
                context.Report(FatProblemKind.FreeCount, null, null,
                    $"FSInfo records {recorded} free clusters; {context.FreeClusters} are free.");
        }

        private static async ValueTask CheckBitmapAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var core = context.Core;
            if (context.Bitmap is not { } entry)
                return;

            var bitmap = new ExFatBitmap(core, entry);

            var owners = context.Owners;
            uint? firstLost = null;
            uint? firstUnmarked = null;
            long unmarked = 0;
            try
            {
                for (uint cluster = FatTable.FIRST_CLUSTER; cluster <= core.Table.LastCluster; cluster++)
                {
                    bool isUsed = await bitmap.IsUsedAsync(cluster, cancellationToken).ConfigureAwait(false);
                    bool isOwned = owners.IsOwned(cluster);
                    if (isUsed && !isOwned)
                    {
                        if ((await core.Table.GetAsync(cluster, cancellationToken).ConfigureAwait(false)).Kind == FatTableEntryKind.Bad)
                        {
                            context.BadClusters++;
                            continue;
                        }

                        context.LostClusters++;
                        firstLost ??= cluster;
                    }
                    else if (!isUsed)
                    {
                        context.FreeClusters++;
                        if (!isOwned)
                            continue;
                        unmarked++;
                        firstUnmarked ??= cluster;
                    }
                }
            }
            catch (FatException error) when (error.Kind == FatErrorKind.Corrupt)
            {
                context.LostClusters = context.FreeClusters = context.BadClusters = 0;
                context.Report(FatProblemKind.Metadata, FatCheckMetadata.BITMAP_OWNER, null, error.Message);
                return;
            }

            if (firstLost != null)
                context.Report(FatProblemKind.LostClusters, null, firstLost,
                    $"{context.LostClusters} clusters are marked used in the bitmap and belong to nothing; the first is cluster {firstLost}.");
            if (firstUnmarked != null)
                context.Report(FatProblemKind.BitmapMismatch, null, firstUnmarked,
                    $"{unmarked} clusters in use are marked free in the bitmap; the first is cluster {firstUnmarked}.");

            await CheckPercentAsync(context, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks the share of the heap exFAT's boot sector says is in use: a percentage, or
        /// 0xFF for unknown.
        /// </summary>
        /// <remarks>
        /// Only a value no volume can hold is reported. Linux never updates the share and
        /// leaves what mkfs.exfat wrote, and fsck.exfat accepts that, so a stale share is no
        /// sign of damage.
        /// </remarks>
        private static async ValueTask CheckPercentAsync(FatCheckContext context, CancellationToken cancellationToken)
        {
            var sector = new byte[context.Core.Info.SectorSize];
            await context.Core.Device.ReadAsync(0, sector, cancellationToken).ConfigureAwait(false);
            byte percent = sector[PERCENT_IN_USE];
            if (percent > MAX_PERCENT && percent != PERCENT_UNKNOWN)
                context.Report(FatProblemKind.PercentInUse, null, null,
                    $"The boot sector says {percent}% of the clusters are in use.");
        }

        #endregion
    }
}
