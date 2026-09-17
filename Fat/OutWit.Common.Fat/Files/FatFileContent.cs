using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Files
{
    /// <summary>
    /// The bytes of one file and the clusters that hold them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The file always has exactly the clusters the length needs: growing takes clusters,
    /// continuing from its last one where the volume allows; shrinking ends the chain and
    /// releases the rest; an empty file has no clusters at all.
    /// </para>
    /// <para>
    /// On exFAT a file whose clusters follow each other has no chain in the table. It stays
    /// so while new clusters follow the old; the first time they do not, a chain is written
    /// through all of them. The valid length moves with writes: bytes between it and a write
    /// past it are written as zeros, and a longer length set on its own is not written at
    /// all — past the valid length a file reads as zeros. FAT has no valid length and no
    /// sparse files, so there a longer length is written as zeros.
    /// </para>
    /// <para>
    /// Clusters cut off by a shorter length stay taken, and linked, until
    /// <see cref="ReleaseCutAsync"/>, which the owner calls once the directory entry no longer
    /// counts on them; the chain's new end is written then too.
    /// </para>
    /// </remarks>
    internal sealed class FatFileContent
    {
        #region Constants

        private const int ZERO_CHUNK_BYTES = 1 << 20;

        #endregion

        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly string m_path;

        private readonly bool m_isExFat;

        private readonly List<ClusterRun> m_cut = new();

        private ClusterChain? m_chain;

        private uint m_pendingEnd;

        #endregion

        #region Constructors

        /// <exception cref="FatException">The file's clusters are not on the volume.</exception>
        public FatFileContent(FatVolumeCore core, DirectoryItem item)
        {
            m_core = core;
            m_path = item.Entry.Path;
            m_isExFat = core.ExFat != null;
            Length = item.Entry.Length;
            ValidLength = Math.Min(item.ValidLength, Length);
            FirstCluster = item.Entry.FirstCluster;
            if (Length > 0)
            {
                m_chain = core.OpenChain(item);
                IsContiguous = item.IsContiguous;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Reads from a position; nothing past the end, and zeros past the valid length.
        /// </summary>
        public async ValueTask<int> ReadAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            long available = Length - position;
            if (available <= 0 || buffer.IsEmpty)
                return 0;

            int count = (int)Math.Min(buffer.Length, available);
            int stored = (int)Math.Clamp(ValidLength - position, 0, count);
            if (stored > 0)
            {
                await TransferAsync(position, stored, (offset, index, length) =>
                    m_core.Device.ReadBytesAsync(offset, buffer.Slice(index, length), cancellationToken), cancellationToken).ConfigureAwait(false);
            }

            buffer.Span[stored..count].Clear();
            return count;
        }

        /// <summary>
        /// Writes at a position, growing the file as needed.
        /// </summary>
        /// <exception cref="FatException">The volume is full, or the file would pass what the volume can record.</exception>
        public async ValueTask WriteAsync(long position, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            if (data.IsEmpty)
                return;

            long end = position + data.Length;
            CheckLength(end);
            await EnsureClustersAsync(m_core.ClustersFor(end), cancellationToken).ConfigureAwait(false);

            if (position > ValidLength)
                await WriteZerosAsync(ValidLength, position, cancellationToken).ConfigureAwait(false);

            await TransferAsync(position, data.Length, (offset, index, length) =>
                m_core.Device.WriteBytesAsync(offset, data.Slice(index, length), cancellationToken), cancellationToken).ConfigureAwait(false);

            Length = Math.Max(Length, end);
            ValidLength = Math.Max(ValidLength, end);
        }

        /// <summary>
        /// Makes the file longer or shorter.
        /// </summary>
        /// <exception cref="FatException">The volume is full, or the file would pass what the volume can record.</exception>
        public async ValueTask SetLengthAsync(long length, CancellationToken cancellationToken)
        {
            CheckLength(length);
            if (length == Length)
                return;

            if (length > Length)
            {
                await EnsureClustersAsync(m_core.ClustersFor(length), cancellationToken).ConfigureAwait(false);
                if (!m_isExFat)
                {
                    await WriteZerosAsync(Length, length, cancellationToken).ConfigureAwait(false);
                    ValidLength = length;
                }
            }
            else
            {
                if (m_chain != null)
                    await ShrinkAsync(m_core.ClustersFor(length), cancellationToken).ConfigureAwait(false);
                ValidLength = Math.Min(ValidLength, length);
            }

            Length = length;
        }

        /// <summary>
        /// Follows the chain to the last cluster the file needs, reading only the table.
        /// </summary>
        /// <exception cref="FatException">The chain is corrupt or ends before the file does.</exception>
        public async ValueTask CheckChainAsync(CancellationToken cancellationToken)
        {
            if (m_chain != null && await m_chain.FindAsync(m_core.ClustersFor(Length) - 1, 1, cancellationToken).ConfigureAwait(false) == null)
                throw ShortChain();
        }

        /// <summary>
        /// Cuts the clusters to those the length needs, in case a failed write took more, so
        /// that an empty file has none.
        /// </summary>
        public async ValueTask TrimAsync(CancellationToken cancellationToken)
        {
            if (m_chain != null)
                await ShrinkAsync(m_core.ClustersFor(Length), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Ends the chain where a shorter length put its end, and releases the clusters cut off.
        /// </summary>
        /// <remarks>
        /// A cluster at a time, each forgotten once it is free, so a release that fails part of
        /// the way can be run again without freeing a cluster twice.
        /// </remarks>
        public async ValueTask ReleaseCutAsync(CancellationToken cancellationToken)
        {
            var allocator = m_core.Allocator;
            if (m_pendingEnd != 0)
            {
                await allocator.EndChainAsync(m_pendingEnd, cancellationToken).ConfigureAwait(false);
                m_pendingEnd = 0;
            }

            while (m_cut.Count > 0)
            {
                var run = m_cut[0];
                await allocator.ReleaseAsync(new[] { run with { Count = 1 } }, cancellationToken).ConfigureAwait(false);
                if (run.Count == 1)
                    m_cut.RemoveAt(0);
                else
                    m_cut[0] = new ClusterRun(run.FirstIndex + 1, run.FirstCluster + 1, run.Count - 1);
            }
        }

        private async ValueTask EnsureClustersAsync(long clusters, CancellationToken cancellationToken)
        {
            var allocator = m_core.Allocator;
            if (m_chain == null)
            {
                var runs = await allocator.AllocateAsync(0, clusters, 0, !m_isExFat, cancellationToken).ConfigureAwait(false);
                IsContiguous = m_isExFat && runs.Count == 1;
                if (m_isExFat && !IsContiguous)
                    await LinkOrGiveBackAsync(runs, runs, cancellationToken).ConfigureAwait(false);
                m_chain = ClusterChain.FromRuns(m_core.Table, runs);
                FirstCluster = runs[0].FirstCluster;
                return;
            }

            if (await m_chain.FindAsync(clusters - 1, 1, cancellationToken).ConfigureAwait(false) != null)
                return;

            long have = m_chain.KnownCount;
            if (have < m_core.ClustersFor(Length))
                throw ShortChain();

            uint last = m_chain.LastCluster;
            var more = await allocator.AllocateAsync(last, clusters - have, have, !IsContiguous, cancellationToken).ConfigureAwait(false);
            if (IsContiguous && (more.Count > 1 || more[0].FirstCluster != last + 1))
            {
                var all = await ReadAllWithAsync(more, cancellationToken).ConfigureAwait(false);
                await LinkOrGiveBackAsync(all, more, cancellationToken).ConfigureAwait(false);
                IsContiguous = false;
            }

            if (m_pendingEnd == last)
                m_pendingEnd = 0;
            m_chain.Append(more);
        }

        /// <summary>
        /// The chain's clusters followed by clusters just taken, for a chain to be written
        /// through all of them; the new clusters are given back when the old ones cannot be read.
        /// </summary>
        private async ValueTask<List<ClusterRun>> ReadAllWithAsync(List<ClusterRun> more, CancellationToken cancellationToken)
        {
            try
            {
                var existing = await m_chain!.ReadAllAsync(cancellationToken).ConfigureAwait(false);
                return existing.Concat(more).ToList();
            }
            catch
            {
                await m_core.Allocator.RollBackAsync(0, more, false).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Writes a chain through clusters, and gives back the ones just taken when the
        /// chain cannot be written, so that a failure leaves no cluster that nothing owns.
        /// </summary>
        /// <param name="runs">The whole chain, in order.</param>
        /// <param name="taken">The clusters among them that were taken for it.</param>
        private async ValueTask LinkOrGiveBackAsync(IReadOnlyList<ClusterRun> runs, IReadOnlyList<ClusterRun> taken, CancellationToken cancellationToken)
        {
            try
            {
                await m_core.Allocator.LinkAsync(runs, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await m_core.Allocator.RollBackAsync(0, taken, false).ConfigureAwait(false);
                throw;
            }
        }

        private async ValueTask ShrinkAsync(long clusters, CancellationToken cancellationToken)
        {
            var chain = m_chain!;
            var all = await chain.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            if (clusters == 0)
            {
                m_cut.AddRange(all);
                m_chain = null;
                m_pendingEnd = 0;
                FirstCluster = 0;
                IsContiguous = false;
                return;
            }

            if (chain.KnownCount <= clusters)
                return;

            var removed = chain.Truncate(clusters);
            if (!IsContiguous)
                m_pendingEnd = chain.LastCluster;
            m_cut.AddRange(removed);
        }

        private async ValueTask WriteZerosAsync(long from, long to, CancellationToken cancellationToken)
        {
            var zeros = new byte[(int)Math.Min(ZERO_CHUNK_BYTES, to - from)];
            for (long position = from; position < to; position += zeros.Length)
            {
                int count = (int)Math.Min(zeros.Length, to - position);
                await TransferAsync(position, count, (offset, index, length) =>
                    m_core.Device.WriteBytesAsync(offset, zeros.AsMemory(0, length), cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Calls <paramref name="action"/> once for each contiguous stretch of the range, with
        /// the device offset, the offset within the range, and the length.
        /// </summary>
        private async ValueTask TransferAsync(long position, int count, Func<long, int, int, ValueTask> action, CancellationToken cancellationToken)
        {
            var chain = m_chain ?? throw ShortChain();
            int clusterSize = m_core.ClusterSize;
            int done = 0;

            while (done < count)
            {
                long index = position / clusterSize;
                int within = (int)(position % clusterSize);
                long ahead = (within + (long)(count - done) + clusterSize - 1) / clusterSize;

                var run = await chain.FindAsync(index, ahead, cancellationToken).ConfigureAwait(false)
                          ?? throw ShortChain();

                int length = (int)Math.Min(count - done, (run.EndIndex - index) * clusterSize - within);
                await action(m_core.ClusterOffset(run.ClusterAt(index)) + within, done, length).ConfigureAwait(false);
                done += length;
                position += length;
            }
        }

        private FatException ShortChain()
        {
            return new FatException(FatErrorKind.Corrupt,
                $"The chain of {m_path} ends after {m_chain?.KnownCount ?? 0} clusters; the file needs {m_core.ClustersFor(Length)}.");
        }

        private void CheckLength(long length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);
            if (length > m_core.MaxFileLength)
                throw new FatException(FatErrorKind.TooLarge, $"A file on this volume holds at most {m_core.MaxFileLength} bytes; {length} were asked for.");
        }

        #endregion

        #region Properties

        public long Length { get; private set; }

        /// <summary>
        /// How much of the file was written; the rest reads as zeros.
        /// </summary>
        public long ValidLength { get; private set; }

        public uint FirstCluster { get; private set; }

        /// <summary>
        /// Whether the clusters follow each other without a table chain; exFAT only.
        /// </summary>
        public bool IsContiguous { get; private set; }

        /// <summary>
        /// Whether a chain end or clusters wait for <see cref="ReleaseCutAsync"/>.
        /// </summary>
        public bool HasCut => m_cut.Count > 0 || m_pendingEnd != 0;

        #endregion
    }
}
