using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;

namespace OutWit.Common.Fat.Volumes
{
    /// <summary>
    /// What a volume keeps inside about an entry, for the checker and the tests.
    /// </summary>
    internal static class FatVolumeInspection
    {
        #region Functions

        /// <summary>
        /// The clusters of what a path names, in order: its chain followed to the end, or
        /// the consecutive clusters its length counts.
        /// </summary>
        /// <exception cref="FatException">Nothing is there, or its clusters are corrupt.</exception>
        public static async ValueTask<IReadOnlyList<ClusterRun>> GetClusterRunsAsync(this FatVolume volume, string path,
            CancellationToken cancellationToken)
        {
            volume.Core.ThrowIfDisposed();
            var item = await volume.Names.RequireAsync(path, null, cancellationToken).ConfigureAwait(false);
            if (item.Entry.FirstCluster == 0)
                return Array.Empty<ClusterRun>();

            return await volume.Core.OpenChain(item).ReadAllAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// What a path names, with its place and, on exFAT, its stream extension.
        /// </summary>
        public static ValueTask<DirectoryItem?> GetItemAsync(this FatVolume volume, string path, CancellationToken cancellationToken)
        {
            volume.Core.ThrowIfDisposed();
            return volume.Names.FindAsync(FatPath.Split(path), cancellationToken);
        }

        #endregion
    }
}
