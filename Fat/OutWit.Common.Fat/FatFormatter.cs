using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Formatting;
using OutWit.Common.Fat.Model;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// Makes new FAT12, FAT16, FAT32 and exFAT volumes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Formatting writes the boot structures, the allocation tables, exFAT's bitmap and
    /// up-case table, and an empty root; the data area is left as it is. What the options
    /// leave open is chosen from the volume's size — see <see cref="FatFormatOptions"/>. The
    /// volume is described back by <see cref="FatDetector"/>, so what is returned is what a
    /// reader will find.
    /// </para>
    /// <para>
    /// A volume's boot sector is written last, after the old one — and any exFAT backup
    /// region — has been cleared; a disk's partition table is cleared first, with whatever
    /// boot structures a whole-disk volume left behind it, and written after its volume. A
    /// format cut short leaves nothing that passes for a volume, old or new.
    /// </para>
    /// </remarks>
    public static class FatFormatter
    {
        #region Constants

        private const long PARTITION_START_BYTES = 1L << 20;

        /// <summary>
        /// The sectors cleared before anything else, at the front of a volume and of a disk:
        /// exFAT's main and backup boot regions, which cover a FAT boot sector and a FAT32
        /// backup's place as well.
        /// </summary>
        private const int CLEARED_SECTORS = 2 * ExFatBootSector.BOOT_REGION_SECTORS;

        #endregion

        #region Functions

        /// <summary>
        /// Formats a device as one volume, with no partition table.
        /// </summary>
        /// <param name="device">The device; all of it becomes the volume.</param>
        /// <param name="options">How to format it; <see cref="FatFormatOptions.DEFAULT"/> when <c>null</c>.</param>
        /// <param name="cancellationToken">Cancels the format; what was written by then stays written.</param>
        /// <returns>The new volume's layout.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The device is read-only.</exception>
        /// <exception cref="ArgumentException">
        /// The options cannot make a volume of this size: the cluster size is not allowed, the
        /// kind cannot have the number of clusters it would give, the label is not valid, or
        /// the device is too small.
        /// </exception>
        public static async ValueTask<FatVolumeInfo> FormatAsync(IBlockDevice device, FatFormatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(device);
            options ??= FatFormatOptions.DEFAULT;
            ThrowIfReadOnly(device);

            return await FormatVolumeAsync(device, 0, options, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Writes a master boot record with one partition, from 1 MiB to the end of the disk,
        /// and formats it.
        /// </summary>
        /// <param name="disk">The disk; what its sector zero held is replaced.</param>
        /// <param name="options">How to format the partition; <see cref="FatFormatOptions.DEFAULT"/> when <c>null</c>.</param>
        /// <param name="cancellationToken">Cancels the format; what was written by then stays written.</param>
        /// <returns>The disk's new layout.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="disk"/> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The disk is read-only.</exception>
        /// <exception cref="ArgumentException">As for <see cref="FormatAsync"/>, or the disk has no room past 1 MiB.</exception>
        public static async ValueTask<FatDiskLayout> FormatDiskAsync(IBlockDevice disk, FatFormatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(disk);
            options ??= FatFormatOptions.DEFAULT;
            ThrowIfReadOnly(disk);

            long start = PARTITION_START_BYTES / disk.SectorSize;
            if (disk.SectorCount <= start)
                throw new ArgumentException($"A disk of {disk.SectorCount} sectors has no room for a partition after 1 MiB.", nameof(disk));

            long count = Math.Min(disk.SectorCount - start, uint.MaxValue);
            uint serial = options.VolumeSerial ?? FatFormatPlan.MakeSerial(options.Clock.GetLocalNow());
            await FatLayoutVfat.ZeroAsync(disk, 0, CLEARED_SECTORS, cancellationToken).ConfigureAwait(false);
            FatVolumeInfo volume;
            await using (var partition = new BlockDevicePartition(disk, start, count))
                volume = await FormatVolumeAsync(partition, start, options, serial, cancellationToken).ConfigureAwait(false);

            var record = MasterBootRecordWriter.Build(disk.SectorSize, serial, MasterBootRecordWriter.TypeOf(volume.Kind), start, count);
            await disk.WriteAsync(0, record, cancellationToken).ConfigureAwait(false);
            await disk.FlushAsync(cancellationToken).ConfigureAwait(false);
            return await FatDetector.DetectDiskAsync(disk, cancellationToken).ConfigureAwait(false);
        }

        private static async ValueTask<FatVolumeInfo> FormatVolumeAsync(IBlockDevice device, long hiddenSectors, FatFormatOptions options,
            uint? serial, CancellationToken cancellationToken)
        {
            var (kind, layout) = Plan(device, options);
            string? label = FatFormatPlan.NormalizeLabel(kind, options.Label);
            var now = options.Clock.GetLocalNow();
            uint volumeSerial = serial ?? options.VolumeSerial ?? FatFormatPlan.MakeSerial(now);

            await FatLayoutVfat.ZeroAsync(device, 0, Math.Min(CLEARED_SECTORS, device.SectorCount), cancellationToken).ConfigureAwait(false);
            if (layout is FatLayoutExFat exFat)
                await exFat.WriteAsync(device, hiddenSectors, volumeSerial, label, cancellationToken).ConfigureAwait(false);
            else
                await ((FatLayoutVfat)layout).WriteAsync(device, hiddenSectors, volumeSerial, label, now.DateTime, cancellationToken).ConfigureAwait(false);

            return await FatDetector.DetectVolumeAsync(device, cancellationToken).ConfigureAwait(false)
                   ?? throw new InvalidOperationException("The volume just written is not recognised.");
        }

        /// <summary>
        /// Lays the volume out as the options ask. Without a kind, the kind the size calls for
        /// is tried first and the others after it, since on large sectors the usual cluster
        /// size can leave a kind with too few clusters.
        /// </summary>
        /// <exception cref="ArgumentException">No kind that may be tried fits.</exception>
        private static (FatKind Kind, object Layout) Plan(IBlockDevice device, FatFormatOptions options)
        {
            int sectorSize = device.SectorSize;
            long bytes = device.SectorCount * sectorSize;
            var preferred = options.Kind ?? FatFormatPlan.ChooseKind(bytes);
            var kinds = options.Kind != null
                ? new[] { preferred }
                : new[] { preferred, FatKind.Fat12, FatKind.Fat16, FatKind.Fat32, FatKind.ExFat };

            ArgumentException? first = null;
            foreach (var kind in kinds)
            {
                try
                {
                    int clusterSize = options.ClusterSize ?? FatFormatPlan.ChooseClusterSize(kind, bytes, sectorSize);
                    int sectorsPerCluster = FatFormatPlan.SectorsPerCluster(kind, clusterSize, sectorSize);
                    object layout = kind == FatKind.ExFat
                        ? FatLayoutExFat.Plan(sectorSize, device.SectorCount, sectorsPerCluster)
                        : FatLayoutVfat.Plan(kind, sectorSize, device.SectorCount, sectorsPerCluster, options.FatCount, options.RootEntries);
                    return (kind, layout);
                }
                catch (ArgumentException error) when (error is not ArgumentOutOfRangeException)
                {
                    first ??= error;
                }
            }

            throw first!;
        }

        private static void ThrowIfReadOnly(IBlockDevice device)
        {
            if (device.IsReadOnly)
                throw new NotSupportedException("A read-only device cannot be formatted.");
        }

        #endregion
    }
}
