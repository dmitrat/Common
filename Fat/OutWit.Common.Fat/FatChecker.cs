using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Checking;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// Checks a mounted volume and reports what is wrong with it, changing nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The check reads the boot region, walks every directory and every chain, and then
    /// compares the allocation table, or exFAT's bitmap, with what the walk found. It finds
    /// what <c>fsck.fat -n</c> and <c>fsck.exfat -n</c> find: broken and cross-linked
    /// chains, lengths that do not match their clusters, lost clusters, a bitmap that does
    /// not match, broken entries and long names, wrong name hashes, directory loops, dot
    /// entries that point elsewhere, a free count that is wrong, a backup boot region that
    /// differs, and a volume left dirty.
    /// </para>
    /// <para>
    /// The check reads the whole table, so on a large volume on a slow device it takes a
    /// while. A volume mounted for writing is flushed first, so the check sees what the
    /// volume holds; no change should run while it does.
    /// </para>
    /// </remarks>
    public static class FatChecker
    {
        #region Functions

        /// <summary>
        /// Checks a volume.
        /// </summary>
        /// <param name="volume">The mounted volume.</param>
        /// <param name="cancellationToken">Cancels the check.</param>
        /// <returns>What was found; <see cref="FatCheckReport.IsClean"/> when nothing.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="volume"/> is <c>null</c>.</exception>
        /// <exception cref="ObjectDisposedException">The volume has been disposed.</exception>
        /// <exception cref="FatException">An open file cannot be flushed.</exception>
        /// <exception cref="System.IO.IOException">The device fails to read.</exception>
        public static async ValueTask<FatCheckReport> CheckAsync(FatVolume volume, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(volume);
            await volume.FlushAsync(cancellationToken).ConfigureAwait(false);

            var context = new FatCheckContext(volume.Core, volume.Names);
            await FatCheckBoot.CheckAsync(context, cancellationToken).ConfigureAwait(false);
            await FatCheckTree.CheckAsync(context, cancellationToken).ConfigureAwait(false);
            await FatCheckAllocation.CheckAsync(context, cancellationToken).ConfigureAwait(false);
            return context.ToReport();
        }

        #endregion
    }
}
