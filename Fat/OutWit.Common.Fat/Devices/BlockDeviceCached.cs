using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Fat.Devices
{
    /// <summary>
    /// A least-recently-used sector cache in front of another device, with writes held
    /// back until a flush, or passed straight through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes a slow device usable: a sector of the allocation table or a
    /// directory is fetched when it is first needed and then answered from memory.
    /// Sectors not yet cached are fetched in runs, one request per run of consecutive
    /// misses.
    /// </para>
    /// <para>
    /// Written sectors are only marked dirty. They reach the inner device when the cache
    /// is flushed, when it is disposed, or when a dirty sector would have to be evicted
    /// to make room — and then all of them go, in ascending order, one request per run
    /// of consecutive sectors. The order of writes is therefore by sector, not by time;
    /// a flush is the point at which the inner device is consistent. A run is split at
    /// <see cref="MAX_RUN_BYTES"/>. A flush that fails leaves its sectors dirty, so a later
    /// flush retries them. Room for a write is made before any of it is applied, so when
    /// making room fails — it can mean flushing — the write is not applied at all, unless
    /// it is larger than the cache; and because making room can mean flushing, a read
    /// can fail with the inner device's write error too.
    /// </para>
    /// <para>
    /// Created to write through, the cache passes every write to the inner device at once,
    /// as it was asked for, and keeps a copy of what was written; nothing is ever dirty.
    /// That keeps the order of writes a file system chose — which matters on a device that
    /// may be cut off in the middle, such as a card behind a radio link — and still answers
    /// repeated reads from memory. A write that fails drops its sectors from the cache, so
    /// they are read again from the device.
    /// </para>
    /// <para>
    /// Disposal flushes first, unless the inner device has been flushed since it was last
    /// written. If that flush fails, nothing is released — the cache keeps its dirty
    /// sectors and the inner device stays open — so the flush or the disposal can be
    /// retried.
    /// </para>
    /// </remarks>
    public sealed class BlockDeviceCached : BlockDeviceBase
    {
        #region Constants

        /// <summary>
        /// The number of sectors cached when no capacity is given.
        /// </summary>
        public const int DEFAULT_CAPACITY = 1024;

        /// <summary>
        /// The most bytes written to the inner device in one request when flushing.
        /// </summary>
        public const int MAX_RUN_BYTES = 1 << 20;

        #endregion

        #region Fields

        private readonly IBlockDevice m_inner;

        private readonly bool m_leaveOpen;

        private readonly bool m_writesThrough;

        private readonly Dictionary<long, LinkedListNode<BlockDeviceCachedEntry>> m_index = new();

        private readonly LinkedList<BlockDeviceCachedEntry> m_recency = new();

        private int m_dirtyCount;

        private bool m_needsFlush;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a cache in front of a device.
        /// </summary>
        /// <param name="inner">The device to cache; its geometry and read-only state are taken as they are.</param>
        /// <param name="capacity">The number of sectors to hold, at least one.</param>
        /// <param name="leaveOpen">Whether the inner device stays open when the cache is disposed.</param>
        /// <param name="writesThrough">Whether writes go to the inner device at once, in the order they are made.</param>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is less than one.</exception>
        public BlockDeviceCached(IBlockDevice inner, int capacity = DEFAULT_CAPACITY, bool leaveOpen = false, bool writesThrough = false)
            : base(Require(inner).SectorSize, inner.SectorCount, inner.IsReadOnly)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

            m_inner = inner;
            m_leaveOpen = leaveOpen;
            m_writesThrough = writesThrough;
            Capacity = capacity;
        }

        #endregion

        #region Functions

        private static IBlockDevice Require(IBlockDevice inner)
        {
            ArgumentNullException.ThrowIfNull(inner);
            return inner;
        }

        private async ValueTask AddAsync(long sector, ReadOnlyMemory<byte> data, bool isDirty, CancellationToken cancellationToken)
        {
            byte[] buffer = m_index.Count >= Capacity
                ? await EvictAsync(cancellationToken).ConfigureAwait(false)
                : new byte[SectorSize];

            Add(sector, data, isDirty, buffer);
        }

        private void Add(long sector, ReadOnlyMemory<byte> data, bool isDirty, byte[] buffer)
        {
            var entry = new BlockDeviceCachedEntry(sector, buffer) { IsDirty = isDirty };
            data.Span.CopyTo(buffer);
            m_index[sector] = m_recency.AddFirst(entry);
            if (isDirty)
                m_dirtyCount++;
        }

        /// <summary>
        /// Evicts what a write needs room for before any of it is applied — evicting can mean
        /// flushing, which can fail — and hands back the buffers freed, one for each sector
        /// the write adds. A write larger than the cache gets room for what the cache holds.
        /// </summary>
        private async ValueTask<Stack<byte[]>> MakeRoomAsync(long sector, int count, CancellationToken cancellationToken)
        {
            int missing = 0;
            for (int i = 0; i < count; i++)
            {
                if (!m_index.ContainsKey(sector + i))
                    missing++;
            }

            var spare = new Stack<byte[]>();
            while (m_index.Count + missing > Capacity && m_index.Count > count - missing)
            {
                long victim = m_recency.Last!.Value.Sector;
                spare.Push(await EvictAsync(cancellationToken).ConfigureAwait(false));
                if (victim >= sector && victim < sector + count)
                    missing++;
            }

            return spare;
        }

        /// <summary>
        /// Keeps a copy of sectors just written to the inner device.
        /// </summary>
        private async ValueTask RememberAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            for (int i = 0; i < buffer.Length / SectorSize; i++)
            {
                var data = buffer.Slice(i * SectorSize, SectorSize);
                if (m_index.TryGetValue(sector + i, out var node))
                {
                    data.Span.CopyTo(node.Value.Data);
                    Touch(node);
                }
                else
                {
                    await AddAsync(sector + i, data, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private void Forget(long sector, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (m_index.Remove(sector + i, out var node))
                    m_recency.Remove(node);
            }
        }

        private async ValueTask<byte[]> EvictAsync(CancellationToken cancellationToken)
        {
            var victim = m_recency.Last ?? throw new InvalidOperationException("The cache is full but holds nothing to evict.");
            if (victim.Value.IsDirty)
                await WriteDirtyAsync(cancellationToken).ConfigureAwait(false);

            m_recency.Remove(victim);
            m_index.Remove(victim.Value.Sector);
            return victim.Value.Data;
        }

        private void Touch(LinkedListNode<BlockDeviceCachedEntry> node)
        {
            if (node != m_recency.First)
            {
                m_recency.Remove(node);
                m_recency.AddFirst(node);
            }
        }

        private async ValueTask WriteDirtyAsync(CancellationToken cancellationToken)
        {
            if (m_dirtyCount == 0)
                return;

            var dirty = m_recency.Where(entry => entry.IsDirty).OrderBy(entry => entry.Sector).ToList();

            int maxRun = MAX_RUN_BYTES / SectorSize;
            int start = 0;
            while (start < dirty.Count)
            {
                int end = start + 1;
                while (end < dirty.Count && end - start < maxRun && dirty[end].Sector == dirty[end - 1].Sector + 1)
                    end++;

                await WriteRunAsync(dirty, start, end - start, cancellationToken).ConfigureAwait(false);
                start = end;
            }
        }

        private async ValueTask WriteRunAsync(List<BlockDeviceCachedEntry> dirty, int start, int count, CancellationToken cancellationToken)
        {
            int length = count * SectorSize;
            byte[] run = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                for (int i = 0; i < count; i++)
                    dirty[start + i].Data.CopyTo(run, i * SectorSize);

                await m_inner.WriteAsync(dirty[start].Sector, run.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                m_needsFlush = true;

                for (int i = 0; i < count; i++)
                    dirty[start + i].IsDirty = false;
                m_dirtyCount -= count;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(run, clearArray: true);
            }
        }

        #endregion

        #region Core

        /// <inheritdoc />
        protected override async ValueTask ReadSectorsAsync(long sector, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int count = buffer.Length / SectorSize;
            int i = 0;

            while (i < count)
            {
                if (m_index.TryGetValue(sector + i, out var node))
                {
                    node.Value.Data.CopyTo(buffer.Span.Slice(i * SectorSize, SectorSize));
                    Touch(node);
                    Hits++;
                    i++;
                    continue;
                }

                int end = i + 1;
                while (end < count && !m_index.ContainsKey(sector + end))
                    end++;

                var run = buffer.Slice(i * SectorSize, (end - i) * SectorSize);
                await m_inner.ReadAsync(sector + i, run, cancellationToken).ConfigureAwait(false);
                Misses += end - i;

                for (int k = i; k < end; k++)
                    await AddAsync(sector + k, buffer.Slice(k * SectorSize, SectorSize), false, cancellationToken).ConfigureAwait(false);

                i = end;
            }
        }

        /// <inheritdoc />
        protected override async ValueTask WriteSectorsAsync(long sector, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            int count = buffer.Length / SectorSize;
            if (m_writesThrough)
            {
                try
                {
                    await m_inner.WriteAsync(sector, buffer, cancellationToken).ConfigureAwait(false);
                    m_needsFlush = true;
                }
                catch
                {
                    Forget(sector, count);
                    throw;
                }

                await RememberAsync(sector, buffer, cancellationToken).ConfigureAwait(false);
                return;
            }

            var spare = await MakeRoomAsync(sector, count, cancellationToken).ConfigureAwait(false);
            for (int i = 0; i < count; i++)
            {
                var data = buffer.Slice(i * SectorSize, SectorSize);

                if (!m_index.TryGetValue(sector + i, out var node))
                {
                    if (spare.Count > 0)
                        Add(sector + i, data, true, spare.Pop());
                    else
                        await AddAsync(sector + i, data, true, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                data.Span.CopyTo(node.Value.Data);
                if (!node.Value.IsDirty)
                {
                    node.Value.IsDirty = true;
                    m_dirtyCount++;
                }
                Touch(node);
            }
        }

        /// <inheritdoc />
        protected override async ValueTask FlushSectorsAsync(CancellationToken cancellationToken)
        {
            await WriteDirtyAsync(cancellationToken).ConfigureAwait(false);
            await m_inner.FlushAsync(cancellationToken).ConfigureAwait(false);
            m_needsFlush = false;
        }

        /// <inheritdoc />
        protected override async ValueTask DisposeCoreAsync()
        {
            // What the inner device owes a flush for is not only the dirty sectors: a flush
            // whose inner flush failed has written them already, and a cache that writes
            // through never holds any. A device flushed since it was last written owes nothing.
            if (m_dirtyCount > 0 || m_needsFlush)
                await FlushSectorsAsync(CancellationToken.None).ConfigureAwait(false);

            m_index.Clear();
            m_recency.Clear();
            if (!m_leaveOpen)
                await m_inner.DisposeAsync().ConfigureAwait(false);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The device behind the cache.
        /// </summary>
        public IBlockDevice Inner => m_inner;

        /// <summary>
        /// Whether writes go to the inner device at once.
        /// </summary>
        public bool WritesThrough => m_writesThrough;

        /// <summary>
        /// The most sectors the cache holds.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// The sectors held now.
        /// </summary>
        public int CachedCount => m_index.Count;

        /// <summary>
        /// The sectors written but not yet passed to the inner device.
        /// </summary>
        public int DirtyCount => m_dirtyCount;

        /// <summary>
        /// Sectors answered from the cache since it was created.
        /// </summary>
        public long Hits { get; private set; }

        /// <summary>
        /// Sectors fetched from the inner device since the cache was created.
        /// </summary>
        public long Misses { get; private set; }

        #endregion
    }
}
