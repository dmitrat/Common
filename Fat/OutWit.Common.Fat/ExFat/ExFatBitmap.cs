using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.ExFat
{
    /// <summary>
    /// exFAT's allocation bitmap: a bit for each cluster, set while the cluster is in use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read and written a sector at a time, as <see cref="FatTable"/> is: a few sectors are
    /// kept, and a changed one reaches the device on <see cref="FlushAsync"/> or when it has
    /// to make room. The bitmap's own clusters are followed through the table.
    /// </para>
    /// <para>
    /// Bit <c>n</c> of the bitmap — bit <c>n % 8</c> of byte <c>n / 8</c> — stands for
    /// cluster <c>n + 2</c>; bits past the last cluster mean nothing and are never changed.
    /// </para>
    /// </remarks>
    internal sealed class ExFatBitmap
    {
        #region Constants

        private const int WINDOW_SECTORS = 8;

        private const int CHUNK_BYTES = 1 << 20;

        #endregion

        #region Fields

        private readonly FatVolumeCore m_core;

        private readonly ExFatRootEntry m_entry;

        private readonly ClusterChain m_chain;

        private readonly List<FatTableSector> m_window = new(WINDOW_SECTORS);

        #endregion

        #region Constructors

        /// <exception cref="FatException">The bitmap's first cluster is not on the volume.</exception>
        public ExFatBitmap(FatVolumeCore core, ExFatRootEntry entry)
        {
            m_core = core;
            m_entry = entry;
            m_chain = new ClusterChain(core.Table, entry.FirstCluster);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Whether a cluster is in use.
        /// </summary>
        public async ValueTask<bool> IsUsedAsync(uint cluster, CancellationToken cancellationToken)
        {
            var (sector, bit) = Locate(cluster);
            var held = await HoldAsync(sector, cancellationToken).ConfigureAwait(false);
            return (held.Data[bit >> 3] & (1 << (bit & 7))) != 0;
        }

        /// <summary>
        /// Marks a cluster used or free. The change reaches the device on the next flush.
        /// </summary>
        public async ValueTask SetAsync(uint cluster, bool isUsed, CancellationToken cancellationToken)
        {
            var (sector, bit) = Locate(cluster);
            var held = await HoldAsync(sector, cancellationToken).ConfigureAwait(false);
            byte mask = (byte)(1 << (bit & 7));
            byte value = isUsed ? (byte)(held.Data[bit >> 3] | mask) : (byte)(held.Data[bit >> 3] & ~mask);
            if (value == held.Data[bit >> 3])
                return;

            held.Data[bit >> 3] = value;
            held.IsDirty = true;
        }

        /// <summary>
        /// Finds the first free cluster from <paramref name="start"/> on, coming round to the
        /// first cluster after the last.
        /// </summary>
        /// <returns>The cluster, or <c>null</c> when every cluster is in use.</returns>
        public async ValueTask<uint?> FindFreeAsync(uint start, CancellationToken cancellationToken)
        {
            long clusters = m_core.Info.ClusterCount;
            long first = Math.Clamp((long)start - FatTable.FIRST_CLUSTER, 0, clusters - 1);
            var found = await ScanAsync(first, clusters, cancellationToken).ConfigureAwait(false)
                        ?? await ScanAsync(0, first, cancellationToken).ConfigureAwait(false);
            return found is { } index ? (uint)(index + FatTable.FIRST_CLUSTER) : null;
        }

        /// <summary>
        /// Counts the free clusters, reading the whole bitmap in large pieces once what is
        /// held has been written.
        /// </summary>
        /// <exception cref="FatException">The bitmap's clusters are corrupt.</exception>
        public async ValueTask<long> CountFreeAsync(CancellationToken cancellationToken)
        {
            await FlushAsync(cancellationToken).ConfigureAwait(false);

            long clusters = m_core.Info.ClusterCount;
            long bytes = (clusters + 7) / 8;
            var chunk = new byte[(int)Math.Min(CHUNK_BYTES, bytes)];
            long used = 0;

            for (long offset = 0; offset < bytes; offset += chunk.Length)
            {
                int length = (int)Math.Min(chunk.Length, bytes - offset);
                await ReadAsync(offset, chunk.AsMemory(0, length), cancellationToken).ConfigureAwait(false);

                long bits = Math.Min(clusters - offset * 8, (long)length * 8);
                int whole = (int)(bits / 8);
                for (int i = 0; i < whole; i++)
                    used += BitOperations.PopCount(chunk[i]);
                if (bits % 8 != 0)
                    used += BitOperations.PopCount((uint)(chunk[whole] & ((1 << (int)(bits % 8)) - 1)));
            }

            return clusters - used;
        }

        /// <summary>
        /// Writes every changed sector.
        /// </summary>
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            foreach (var held in m_window)
            {
                if (held.IsDirty)
                    await WriteSectorAsync(held, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The first index from <paramref name="from"/> up to <paramref name="to"/> whose bit is clear.
        /// </summary>
        private async ValueTask<long?> ScanAsync(long from, long to, CancellationToken cancellationToken)
        {
            int bitsPerSector = m_core.Info.SectorSize * 8;
            long index = from;
            while (index < to)
            {
                var held = await HoldAsync(index / bitsPerSector, cancellationToken).ConfigureAwait(false);
                long sectorEnd = Math.Min(to, (index / bitsPerSector + 1) * bitsPerSector);
                for (; index < sectorEnd; index++)
                {
                    int bit = (int)(index % bitsPerSector);
                    byte value = held.Data[bit >> 3];
                    if (value == 0xFF && (bit & 7) == 0 && index + 8 <= sectorEnd)
                    {
                        index += 7;
                        continue;
                    }

                    if ((value & (1 << (bit & 7))) == 0)
                        return index;
                }
            }

            return null;
        }

        private (long Sector, int Bit) Locate(uint cluster)
        {
            if (!m_core.Table.IsCluster(cluster))
                throw new ArgumentOutOfRangeException(nameof(cluster), cluster, $"Clusters run from {FatTable.FIRST_CLUSTER} to {m_core.Table.LastCluster}.");

            long index = cluster - FatTable.FIRST_CLUSTER;
            int bitsPerSector = m_core.Info.SectorSize * 8;
            return (index / bitsPerSector, (int)(index % bitsPerSector));
        }

        private async ValueTask<FatTableSector> HoldAsync(long sector, CancellationToken cancellationToken)
        {
            for (int i = 0; i < m_window.Count; i++)
            {
                var held = m_window[i];
                if (held.Sector != sector)
                    continue;

                m_window.RemoveAt(i);
                m_window.Insert(0, held);
                return held;
            }

            FatTableSector entry;
            if (m_window.Count < WINDOW_SECTORS)
            {
                entry = new FatTableSector(sector, new byte[m_core.Info.SectorSize]);
            }
            else
            {
                entry = m_window[^1];
                if (entry.IsDirty)
                    await WriteSectorAsync(entry, cancellationToken).ConfigureAwait(false);
                m_window.RemoveAt(m_window.Count - 1);
                entry.Sector = sector;
            }

            await ReadAsync(sector * m_core.Info.SectorSize, entry.Data, cancellationToken).ConfigureAwait(false);
            m_window.Insert(0, entry);
            return entry;
        }

        private async ValueTask WriteSectorAsync(FatTableSector held, CancellationToken cancellationToken)
        {
            long device = await DeviceSectorAsync(held.Sector, cancellationToken).ConfigureAwait(false);
            await m_core.Device.WriteAsync(device, held.Data, cancellationToken).ConfigureAwait(false);
            held.IsDirty = false;
        }

        /// <summary>
        /// Reads bytes of the bitmap, whole sectors or not, through its chain.
        /// </summary>
        private async ValueTask ReadAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int clusterSize = m_core.ClusterSize;
            int done = 0;
            while (done < buffer.Length)
            {
                long index = position / clusterSize;
                int within = (int)(position % clusterSize);
                long ahead = (within + (long)(buffer.Length - done) + clusterSize - 1) / clusterSize;
                var run = await m_chain.FindAsync(index, ahead, cancellationToken).ConfigureAwait(false) ?? throw ShortChain();

                int length = (int)Math.Min(buffer.Length - done, (run.EndIndex - index) * clusterSize - within);
                await m_core.Device.ReadBytesAsync(m_core.ClusterOffset(run.ClusterAt(index)) + within, buffer.Slice(done, length), cancellationToken).ConfigureAwait(false);
                done += length;
                position += length;
            }
        }

        private async ValueTask<long> DeviceSectorAsync(long sector, CancellationToken cancellationToken)
        {
            int sectorsPerCluster = m_core.Info.SectorsPerCluster;
            long index = sector / sectorsPerCluster;
            var run = await m_chain.FindAsync(index, 1, cancellationToken).ConfigureAwait(false) ?? throw ShortChain();
            return m_core.ClusterSector(run.ClusterAt(index)) + sector % sectorsPerCluster;
        }

        private FatException ShortChain()
        {
            return new FatException(FatErrorKind.Corrupt,
                $"The allocation bitmap's chain from cluster {m_entry.FirstCluster} ends after {m_chain.KnownCount} clusters; its entry records {m_entry.DataLength} bytes.");
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether any held sector has changes not yet written.
        /// </summary>
        public bool IsDirty => m_window.Exists(held => held.IsDirty);

        #endregion
    }
}
