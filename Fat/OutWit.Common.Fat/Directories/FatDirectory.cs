using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;

namespace OutWit.Common.Fat.Directories
{
    /// <summary>
    /// One directory: its entries, read as they are needed, and where its slots lie.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both formats keep a directory as 32-byte slots in clusters; what the slots mean is
    /// the business of <see cref="FatDirectoryVfat"/> and <see cref="FatDirectoryExFat"/>.
    /// </para>
    /// <para>
    /// A directory is read a sector first, then twice as many sectors each time, up to
    /// <see cref="CHUNK_BYTES"/> and never past a run of consecutive clusters. Reading stops
    /// at the end marker, so a small directory costs one request however large its clusters
    /// are, and the unused tail of a large one is never fetched.
    /// </para>
    /// <para>
    /// A directory whose entry records its size — exFAT's — is read to that size. One that
    /// does not is read to the end of its chain, up to the format's limit; a chain that goes
    /// on past the limit without an end marker is corrupt.
    /// </para>
    /// </remarks>
    internal abstract class FatDirectory
    {
        #region Constants

        public const int CHUNK_BYTES = 64 * 1024;

        #endregion

        #region Fields

        private ClusterChain? m_chain;

        #endregion

        #region Constructors

        /// <param name="core">The volume.</param>
        /// <param name="item">The directory; the root when it lies in no directory.</param>
        protected FatDirectory(FatVolumeCore core, DirectoryItem item)
        {
            Core = core;
            Item = item;
            IsFixedRoot = item.IsRoot && core.HasFixedRoot;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens a directory in the format of the volume.
        /// </summary>
        public static FatDirectory Open(FatVolumeCore core, DirectoryItem item)
        {
            return core.Info.Kind == FatKind.ExFat ? new FatDirectoryExFat(core, item) : new FatDirectoryVfat(core, item);
        }

        /// <summary>
        /// Lists the directory, without <c>.</c>, <c>..</c>, the label or anything else that
        /// is not a file or a directory.
        /// </summary>
        /// <exception cref="FatException">The directory, or an entry in it, is corrupt.</exception>
        public abstract IAsyncEnumerable<DirectoryItem> EnumerateAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Finds an entry by name, ignoring case as the format does.
        /// </summary>
        /// <returns>The entry, or <c>null</c>.</returns>
        public abstract ValueTask<DirectoryItem?> FindAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Reads the volume label from the root directory.
        /// </summary>
        /// <returns>The label, or <c>null</c> when the root has none.</returns>
        public abstract ValueTask<string?> ReadLabelAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Finds the first run of free slots long enough for <paramref name="count"/>
        /// slots, or else the free run the directory ends with.
        /// </summary>
        /// <remarks>
        /// Slots the format marks free are free, and so is every slot from the end marker on.
        /// A run never starts after the end marker, since readers stop there.
        /// </remarks>
        public async ValueTask<FreeSlotRun> FindFreeAsync(int count, CancellationToken cancellationToken)
        {
            long index = 0;
            long start = 0;
            long run = 0;

            await foreach (var chunk in ReadChunksAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int offset = 0; offset + DirectorySlot.SIZE <= chunk.Length; offset += DirectorySlot.SIZE, index++)
                {
                    byte first = chunk.Span[offset];
                    if (first == DirectorySlot.END_OF_DIRECTORY)
                    {
                        if (run == 0)
                            start = index;
                        long capacity = await GetCapacityAsync(cancellationToken).ConfigureAwait(false);
                        return new FreeSlotRun(start, capacity - start, index);
                    }

                    if (!IsFreeSlot(first))
                    {
                        run = 0;
                        continue;
                    }

                    if (run++ == 0)
                        start = index;
                    if (run == count)
                        return new FreeSlotRun(start, run, -1);
                }
            }

            return run > 0 ? new FreeSlotRun(start, run, -1) : new FreeSlotRun(index, 0, -1);
        }

        /// <summary>
        /// The number of slots the directory's clusters, or the fixed root, hold.
        /// </summary>
        public async ValueTask<long> GetCapacityAsync(CancellationToken cancellationToken)
        {
            if (IsFixedRoot)
                return Core.Info.RootDirectoryEntries.GetValueOrDefault();
            if (IsEmpty)
                return 0;

            var chain = GetChain();
            await chain.ReadAllAsync(cancellationToken).ConfigureAwait(false);
            long bytes = chain.KnownCount * Core.ClusterSize;
            return Math.Min(bytes, RecordedBytes ?? bytes) / DirectorySlot.SIZE;
        }

        /// <summary>
        /// The byte on the device a slot starts at.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The slot is past the directory's end.</exception>
        public async ValueTask<long> GetSlotOffsetAsync(long slot, CancellationToken cancellationToken)
        {
            long position = slot * DirectorySlot.SIZE;
            if (IsFixedRoot)
            {
                if (slot < 0 || slot >= Core.Info.RootDirectoryEntries.GetValueOrDefault())
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "The slot lies past the root directory.");
                return Core.Info.RootDirectorySector.GetValueOrDefault() * Core.Info.SectorSize + position;
            }

            long index = position / Core.ClusterSize;
            var run = (IsEmpty ? null : await GetChain().FindAsync(index, 1, cancellationToken).ConfigureAwait(false))
                      ?? throw new ArgumentOutOfRangeException(nameof(slot), slot, $"The slot lies past the end of {Entry.Path}.");
            return Core.ClusterOffset(run.ClusterAt(index)) + position % Core.ClusterSize;
        }

        /// <summary>
        /// Adds zeroed clusters to the directory for at least <paramref name="slots"/> more slots.
        /// </summary>
        /// <exception cref="FatException">
        /// <see cref="FatErrorKind.NoSpace"/>: the directory is the fixed root, would pass
        /// its size limit, or the volume is full.
        /// </exception>
        public virtual async ValueTask GrowAsync(long slots, CancellationToken cancellationToken)
        {
            if (IsFixedRoot)
                throw new FatException(FatErrorKind.NoSpace, "The root directory is full; FAT12 and FAT16 cannot make it larger.");

            long capacity = await GetCapacityAsync(cancellationToken).ConfigureAwait(false);
            long clusters = (slots * DirectorySlot.SIZE + Core.ClusterSize - 1) / Core.ClusterSize;
            if ((capacity + clusters * Core.ClusterSize / DirectorySlot.SIZE) * DirectorySlot.SIZE > MaxBytes)
                throw new FatException(FatErrorKind.NoSpace, $"The directory {Entry.Path} is at its limit of {MaxBytes / DirectorySlot.SIZE} entries.");

            var chain = GetChain();
            var runs = await TakeZeroedAsync(chain.LastCluster, clusters, chain.KnownCount, cancellationToken).ConfigureAwait(false);
            await LinkOrGiveBackAsync(runs, runs, chain.LastCluster, cancellationToken).ConfigureAwait(false);
            chain.Append(runs);
        }

        /// <summary>
        /// Writes a chain through clusters, and gives back the ones just taken when the
        /// chain cannot be written — ending the old chain again where it ended — so that a
        /// failure leaves no cluster that nothing owns.
        /// </summary>
        /// <param name="runs">The clusters to chain, in order.</param>
        /// <param name="taken">The clusters among them that were taken for it.</param>
        /// <param name="after">The cluster the chain continues, or zero for a chain of its own.</param>
        protected async ValueTask LinkOrGiveBackAsync(IReadOnlyList<ClusterRun> runs, IReadOnlyList<ClusterRun> taken, uint after, CancellationToken cancellationToken)
        {
            try
            {
                await Core.Allocator.LinkAsync(runs, cancellationToken, after).ConfigureAwait(false);
            }
            catch
            {
                await Core.Allocator.RollBackAsync(after, taken, after != 0).ConfigureAwait(false);
                throw;
            }
        }

        /// <summary>
        /// Takes clusters without linking them and fills them with zeros; they are given back
        /// when the filling fails, so a directory never counts on a cluster that holds old data.
        /// </summary>
        protected async ValueTask<List<ClusterRun>> TakeZeroedAsync(uint after, long count, long firstIndex, CancellationToken cancellationToken)
        {
            var runs = await Core.Allocator.AllocateAsync(after, count, firstIndex, false, cancellationToken).ConfigureAwait(false);
            try
            {
                foreach (var run in runs)
                    await Core.ZeroClustersAsync(run, cancellationToken).ConfigureAwait(false);
                return runs;
            }
            catch
            {
                await ReleaseQuietlyAsync(runs).ConfigureAwait(false);
                throw;
            }
        }

        /// <remarks>
        /// Best effort: the failure that made the clusters useless is the one reported.
        /// </remarks>
        private async ValueTask ReleaseQuietlyAsync(List<ClusterRun> runs)
        {
            try
            {
                await Core.Allocator.ReleaseAsync(runs, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The clusters stay taken and belong to nothing; FatChecker reports them.
            }
        }

        /// <summary>
        /// The clusters of the directory, followed as far as asked.
        /// </summary>
        /// <exception cref="FatException">The directory has no clusters.</exception>
        protected ClusterChain GetChain()
        {
            if (Entry.FirstCluster == 0)
                throw new FatException(FatErrorKind.Corrupt, $"The directory {Entry.Path} has no clusters.");

            return m_chain ??= Core.OpenChain(Item);
        }

        /// <summary>
        /// Takes the directory as its entry now records it, with the clusters it now has.
        /// </summary>
        protected void Replace(DirectoryItem item, ClusterChain chain)
        {
            Item = item;
            m_chain = chain;
        }

        /// <summary>
        /// The directory's slots as stored, a chunk at a time, for the checker.
        /// </summary>
        /// <exception cref="FatException">The directory's chain is corrupt, or ends before its recorded size.</exception>
        public IAsyncEnumerable<ReadOnlyMemory<byte>> ReadSlotsAsync(CancellationToken cancellationToken)
        {
            return ReadChunksAsync(cancellationToken);
        }

        /// <summary>
        /// Whether a slot whose first byte this is, and which is not the end marker, is free.
        /// </summary>
        protected abstract bool IsFreeSlot(byte first);

        /// <summary>
        /// The directory's contents, a chunk at a time, from its first slot on.
        /// </summary>
        /// <exception cref="FatException">The directory's chain is corrupt, or ends before its recorded size.</exception>
        protected IAsyncEnumerable<ReadOnlyMemory<byte>> ReadChunksAsync(CancellationToken cancellationToken)
        {
            if (IsFixedRoot)
                return ReadFixedRootAsync(cancellationToken);
            return IsEmpty ? NothingAsync() : ReadClustersAsync(GetChain(), cancellationToken);
        }

        private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFixedRootAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var info = Core.Info;
            long remaining = (long)info.RootDirectoryEntries.GetValueOrDefault() * DirectorySlot.SIZE;
            long sector = info.RootDirectorySector.GetValueOrDefault();
            int maxSectors = Math.Max(1, CHUNK_BYTES / info.SectorSize);
            var buffer = new byte[maxSectors * info.SectorSize];

            for (int sectors = 1; remaining > 0; sectors = Math.Min(sectors * 2, maxSectors))
            {
                int count = (int)Math.Min(sectors, (remaining + info.SectorSize - 1) / info.SectorSize);
                var chunk = buffer.AsMemory(0, count * info.SectorSize);
                await Core.Device.ReadAsync(sector, chunk, cancellationToken).ConfigureAwait(false);

                int length = (int)Math.Min(chunk.Length, remaining);
                yield return chunk[..length];

                remaining -= length;
                sector += count;
            }
        }

        private async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadClustersAsync(ClusterChain chain, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var info = Core.Info;
            int sectorsPerCluster = info.SectorsPerCluster;
            int maxSectors = Math.Max(1, CHUNK_BYTES / info.SectorSize);
            long? recorded = RecordedBytes;
            if (recorded > MaxBytes)
                throw new FatException(FatErrorKind.Corrupt, $"The directory {Entry.Path} records {recorded} bytes, past the limit of {MaxBytes}.");

            long maxPosition = ((recorded ?? MaxBytes) + info.SectorSize - 1) / info.SectorSize;
            var buffer = new byte[maxSectors * info.SectorSize];
            long position = 0;

            for (int sectors = 1; ; sectors = Math.Min(sectors * 2, maxSectors))
            {
                if (recorded != null && position >= maxPosition)
                    yield break;

                long index = position / sectorsPerCluster;
                int within = (int)(position % sectorsPerCluster);
                long wanted = Math.Min(within + sectors, within + maxPosition - position);
                long ahead = (wanted + sectorsPerCluster - 1) / sectorsPerCluster;
                if (await chain.FindAsync(index, ahead, cancellationToken).ConfigureAwait(false) is not { } run)
                {
                    if (recorded != null)
                        throw new FatException(FatErrorKind.Corrupt,
                            $"The chain of the directory {Entry.Path} ends after {chain.KnownCount} clusters; it records {recorded} bytes.");
                    yield break;
                }

                if (position >= maxPosition)
                    throw new FatException(FatErrorKind.Corrupt,
                        $"The directory {Entry.Path} goes on past {MaxBytes / DirectorySlot.SIZE} entries without an end.");

                long left = Math.Min((run.EndIndex - index) * sectorsPerCluster - within, maxPosition - position);
                int count = (int)Math.Min(sectors, left);
                var chunk = buffer.AsMemory(0, count * info.SectorSize);
                await Core.Device.ReadAsync(Core.ClusterSector(run.ClusterAt(index)) + within, chunk, cancellationToken).ConfigureAwait(false);

                yield return chunk;
                position += count;
            }
        }

        private static async IAsyncEnumerable<ReadOnlyMemory<byte>> NothingAsync()
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The volume.
        /// </summary>
        protected FatVolumeCore Core { get; }

        /// <summary>
        /// The most a directory of this format may hold, in bytes.
        /// </summary>
        protected abstract long MaxBytes { get; }

        /// <summary>
        /// The size the directory's entry records, or <c>null</c> when the chain alone tells.
        /// </summary>
        protected virtual long? RecordedBytes => null;

        /// <summary>
        /// Whether the directory has no clusters because its recorded size is zero.
        /// </summary>
        protected bool IsEmpty => RecordedBytes == 0;

        /// <summary>
        /// The directory and its place.
        /// </summary>
        public DirectoryItem Item { get; private set; }

        /// <summary>
        /// The directory itself.
        /// </summary>
        public FatDirectoryEntry Entry => Item.Entry;

        /// <summary>
        /// Whether this is the fixed root of FAT12 or FAT16.
        /// </summary>
        public bool IsFixedRoot { get; }

        /// <summary>
        /// The directory's first cluster; zero for the fixed root.
        /// </summary>
        public uint Cluster => IsFixedRoot ? 0 : Entry.FirstCluster;

        #endregion
    }
}
