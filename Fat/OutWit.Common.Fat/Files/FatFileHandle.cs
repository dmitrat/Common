using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// An open file: its contents, and its directory entry, which changes follow on commit.
    /// </summary>
    /// <remarks>
    /// Commit writes in the order that keeps a crash harmless: the table first, so clusters
    /// the file grew by are linked before the entry counts on them; then the entry; then
    /// the release of clusters the file gave up, which the entry no longer counts on.
    /// </remarks>
    internal sealed class FatFileHandle
    {
        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly FatDirectory m_directory;

        private readonly FatFileContent m_content;

        private bool m_isModified;

        #endregion

        #region Constructors

        /// <exception cref="FatException">The file's first cluster is not on the volume.</exception>
        public FatFileHandle(FatVolumeCore core, FatDirectory directory, DirectoryItem item)
        {
            m_core = core;
            m_directory = directory;
            m_content = new FatFileContent(core, item);
            Item = item;
        }

        #endregion

        #region Functions

        public ValueTask<int> ReadAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return m_content.ReadAsync(position, buffer, cancellationToken);
        }

        /// <exception cref="FatException">The volume is full, or the file would pass what the volume can record.</exception>
        public ValueTask WriteAsync(long position, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            return WithCutReleasedAsync(() => m_content.WriteAsync(position, data, cancellationToken), cancellationToken);
        }

        /// <exception cref="FatException">The volume is full, or the file would pass what the volume can record.</exception>
        public ValueTask SetLengthAsync(long length, CancellationToken cancellationToken)
        {
            return WithCutReleasedAsync(() => m_content.SetLengthAsync(length, cancellationToken), cancellationToken);
        }

        public ValueTask CheckChainAsync(CancellationToken cancellationToken)
        {
            return m_content.CheckChainAsync(cancellationToken);
        }

        /// <summary>
        /// Makes the next commit write the entry, as for a file just created.
        /// </summary>
        public void MarkModified()
        {
            m_isModified = true;
        }

        /// <summary>
        /// Writes the entry and the table, and flushes the device. Does nothing when the
        /// file has not changed since the last commit.
        /// </summary>
        public async ValueTask CommitAsync(CancellationToken cancellationToken)
        {
            if (m_isModified)
            {
                await m_content.TrimAsync(cancellationToken).ConfigureAwait(false);
                await m_core.Allocator.FlushAsync(cancellationToken).ConfigureAwait(false);

                var state = new FatFileState(m_content.FirstCluster, m_content.Length, m_content.ValidLength, m_content.IsContiguous);
                Item = await FatDirectoryWriter.Open(m_core, m_directory).UpdateAsync(Item, state, true, cancellationToken).ConfigureAwait(false);
                m_isModified = false;
            }
            else if (!m_content.HasCut)
            {
                return;
            }

            await m_content.ReleaseCutAsync(cancellationToken).ConfigureAwait(false);
            await m_core.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs a change that takes clusters, and once more after a commit when the volume
        /// is full only because clusters this file gave up wait to be released.
        /// </summary>
        /// <remarks>An allocation that fails takes nothing, so the change can be run again.</remarks>
        private async ValueTask WithCutReleasedAsync(Func<ValueTask> change, CancellationToken cancellationToken)
        {
            await m_core.BeginChangeAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                m_isModified = true;
                await change().ConfigureAwait(false);
            }
            catch (FatException error) when (error.Kind == FatErrorKind.NoSpace && m_content.HasCut)
            {
                await CommitAsync(cancellationToken).ConfigureAwait(false);
                m_isModified = true;
                await change().ConfigureAwait(false);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// The file and its place, as of when it was opened or last committed.
        /// </summary>
        public DirectoryItem Item { get; private set; }

        public long Length => m_content.Length;

        #endregion
    }
}
