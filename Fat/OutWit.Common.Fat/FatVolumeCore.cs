using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Files;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// What the parts of a mounted volume share: the device, the layout, the table, the
    /// allocator, the clock and the open files.
    /// </summary>
    internal sealed class FatVolumeCore
    {
        #region Constants

        private const int ZERO_CHUNK_BYTES = 1 << 20;

        #endregion

        #region Constructors

        public FatVolumeCore(IBlockDevice device, FatVolumeInfo info, TimeProvider? clock = null)
        {
            Device = device;
            Info = info;
            Table = FatTable.Create(device, info);
            Clock = clock ?? TimeProvider.System;
            ClusterSize = info.ClusterSize;
            if (info.Kind == FatKind.ExFat)
            {
                ExFat = new ExFatMetadata(this);
                Allocator = new ExFatAllocator(this);
            }
            else
            {
                Allocator = new FatAllocator(device, Table);
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Opens a directory in the volume's format.
        /// </summary>
        public FatDirectory OpenDirectory(DirectoryItem item)
        {
            return FatDirectory.Open(this, item);
        }

        /// <summary>
        /// The clusters of an entry: consecutive ones its length counts when it says the
        /// table does not describe them, otherwise the chain the table describes.
        /// </summary>
        /// <exception cref="FatException">The first cluster is not on the volume, or the clusters run past its end.</exception>
        public ClusterChain OpenChain(DirectoryItem item)
        {
            uint first = item.Entry.FirstCluster;
            if (!item.IsContiguous)
                return new ClusterChain(Table, first);

            long clusters = ClustersFor(item.DataLength);
            if (clusters == 0 || !Table.IsCluster(first) || clusters > Table.LastCluster - first + 1L)
                throw new FatException(FatErrorKind.Corrupt,
                    $"{item.Entry.Path} records {clusters} consecutive clusters from cluster {first}; the volume's clusters run from {FatTable.FIRST_CLUSTER} to {Table.LastCluster}.");

            return ClusterChain.FromRuns(Table, new[] { new ClusterRun(0, first, clusters) });
        }

        /// <summary>
        /// How many clusters hold so many bytes, for any length a volume can record.
        /// </summary>
        public long ClustersFor(long bytes)
        {
            return bytes / ClusterSize + (bytes % ClusterSize != 0 ? 1 : 0);
        }

        /// <summary>
        /// Counts the free clusters, reading the whole table or bitmap.
        /// </summary>
        public ValueTask<long> CountFreeClustersAsync(CancellationToken cancellationToken)
        {
            return Allocator.CountFreeAsync(cancellationToken);
        }

        /// <summary>
        /// Says a change is about to be written: on exFAT the volume is marked dirty first.
        /// </summary>
        public ValueTask BeginChangeAsync(CancellationToken cancellationToken)
        {
            return ExFat?.MarkDirtyAsync(cancellationToken) ?? ValueTask.CompletedTask;
        }

        /// <summary>
        /// Says everything is written and no file is open for writing: on exFAT the dirty
        /// mark goes, with the percentage in use when it is known.
        /// </summary>
        public ValueTask EndChangesAsync(CancellationToken cancellationToken)
        {
            if (ExFat == null)
                return ValueTask.CompletedTask;

            return ExFat.MarkCleanAsync(Allocator.IsCountExact ? Allocator.FreeCount : null, cancellationToken);
        }

        /// <summary>
        /// The sector a cluster starts at.
        /// </summary>
        public long ClusterSector(uint cluster)
        {
            return Info.ClusterHeapSector + (long)(cluster - FatTable.FIRST_CLUSTER) * Info.SectorsPerCluster;
        }

        /// <summary>
        /// The byte a cluster starts at.
        /// </summary>
        public long ClusterOffset(uint cluster)
        {
            return ClusterSector(cluster) * Info.SectorSize;
        }

        /// <summary>
        /// The local wall-clock time, as FAT records it.
        /// </summary>
        public DateTime Now()
        {
            return Clock.GetLocalNow().DateTime;
        }

        /// <summary>
        /// Fills a run of clusters with zeros.
        /// </summary>
        public async ValueTask ZeroClustersAsync(ClusterRun run, CancellationToken cancellationToken)
        {
            long remaining = run.Count * ClusterSize;
            long sector = ClusterSector(run.FirstCluster);
            int chunkSectors = (int)Math.Max(1, Math.Min(ZERO_CHUNK_BYTES / Info.SectorSize, remaining / Info.SectorSize));
            var zeros = new byte[chunkSectors * Info.SectorSize];

            while (remaining > 0)
            {
                int sectors = (int)Math.Min(chunkSectors, remaining / Info.SectorSize);
                await Device.WriteAsync(sector, zeros.AsMemory(0, sectors * Info.SectorSize), cancellationToken).ConfigureAwait(false);
                sector += sectors;
                remaining -= sectors * Info.SectorSize;
            }
        }

        /// <summary>
        /// Writes the table, FSInfo and whatever the device holds back.
        /// </summary>
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            await Allocator.FlushAsync(cancellationToken).ConfigureAwait(false);
            await Device.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Throws once the volume has been disposed.
        /// </summary>
        public void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(IsDisposed, typeof(FatVolume));
        }

        /// <summary>
        /// Throws unless the volume can be written.
        /// </summary>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        public void ThrowIfReadOnly()
        {
            ThrowIfDisposed();
            if (Device.IsReadOnly)
                throw new NotSupportedException("The volume is on a read-only device.");
        }

        #endregion

        #region Properties

        public IBlockDevice Device { get; }

        public FatVolumeInfo Info { get; }

        public FatTable Table { get; }

        public ClusterAllocator Allocator { get; }

        public FatOpenFiles OpenFiles { get; } = new();

        public TimeProvider Clock { get; }

        public int ClusterSize { get; }

        /// <summary>
        /// The up-case table and allocation bitmap; exFAT only.
        /// </summary>
        public ExFatMetadata? ExFat { get; }

        /// <summary>
        /// Whether nothing can be written, because the device is read-only.
        /// </summary>
        public bool IsReadOnly => Device.IsReadOnly;

        /// <summary>
        /// The longest file the volume can record: 4 GiB less a byte on FAT, as much as the
        /// clusters hold on exFAT.
        /// </summary>
        public long MaxFileLength => ExFat != null ? (long)Info.ClusterCount * ClusterSize : uint.MaxValue;

        public bool HasFixedRoot => Info.Kind is FatKind.Fat12 or FatKind.Fat16;

        /// <summary>
        /// Whether the high half of a first-cluster field counts; FAT32 only.
        /// </summary>
        public bool HasHighCluster => Info.Kind == FatKind.Fat32;

        public bool IsDisposed { get; set; }

        #endregion
    }
}
