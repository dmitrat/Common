using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// Finds FAT12, FAT16, FAT32 and exFAT volumes, reading only boot sectors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recognising a volume costs one request for FAT and two for exFAT, whose boot
    /// region checksum covers twelve sectors. Nothing of the allocation table is read.
    /// </para>
    /// <para>
    /// "Not a FAT volume" and "a broken FAT volume" are kept apart: the first gives
    /// <c>null</c> or no entry, the second a <see cref="FatException"/> — or, for one
    /// partition of a partitioned disk, a <see cref="FatPartitionProblem"/>.
    /// </para>
    /// </remarks>
    public static class FatDetector
    {
        #region Functions

        /// <summary>
        /// Describes the volume that starts at sector zero of a device.
        /// </summary>
        /// <param name="device">A device holding one volume — a partition, or a disk without a partition table.</param>
        /// <param name="cancellationToken">Cancels the detection.</param>
        /// <returns>The volume's layout, or <c>null</c> when sector zero is not a FAT or exFAT boot sector.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="device"/> is <c>null</c>.</exception>
        /// <exception cref="FatException">The boot sector is FAT or exFAT but broken, unsupported, or of another sector size.</exception>
        public static async ValueTask<FatVolumeInfo?> DetectVolumeAsync(IBlockDevice device, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(device);
            if (device.SectorCount == 0)
                return null;

            var sector = new byte[device.SectorSize];
            await device.ReadAsync(0, sector, cancellationToken).ConfigureAwait(false);
            return await DescribeAsync(device, sector, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Describes a whole disk: its partition table, if any, and the volumes on it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sector zero is read as a boot sector first and as a master boot record second,
        /// as FatFs does — with one exception. Some formatters put a copy of the first
        /// partition's parameters in front of the table; such a sector records the
        /// partition's start as its hidden sectors, and when a table slot starts there the
        /// table wins.
        /// </para>
        /// <para>
        /// Every primary partition is probed whatever its type byte says. Extended
        /// partitions and slots of zero length are listed but not probed. A partition that
        /// cannot be used is reported in <see cref="FatDiskLayout.Problems"/> rather than
        /// thrown, so it does not hide the others.
        /// </para>
        /// </remarks>
        /// <param name="disk">The disk.</param>
        /// <param name="cancellationToken">Cancels the detection.</param>
        /// <returns>The layout; with no volumes when nothing is recognised.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="disk"/> is <c>null</c>.</exception>
        /// <exception cref="FatException">
        /// The disk has a GUID partition table, or it has no partition table and the volume
        /// spanning it is broken, unsupported, or of another sector size.
        /// </exception>
        public static async ValueTask<FatDiskLayout> DetectDiskAsync(IBlockDevice disk, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(disk);
            if (disk.SectorCount == 0)
                return new FatDiskLayout();

            var sector = new byte[disk.SectorSize];
            await disk.ReadAsync(0, sector, cancellationToken).ConfigureAwait(false);

            var record = MasterBootRecord.TryParse(sector);
            if (record == null || !IsTableBehindBootSector(sector, record))
            {
                var volume = await DescribeAsync(disk, sector, cancellationToken).ConfigureAwait(false);
                if (volume != null)
                {
                    return new FatDiskLayout
                    {
                        Volumes = new[] { new FatVolumeLocation { FirstSector = 0, SectorCount = disk.SectorCount, Volume = volume } }
                    };
                }
            }

            if (record == null)
                return new FatDiskLayout();

            if (record.IsGptProtective)
                throw new FatException(FatErrorKind.Unsupported, "The disk has a GUID partition table; only MBR is supported.");

            var volumes = new List<FatVolumeLocation>();
            var problems = new List<FatPartitionProblem>();
            foreach (var partition in record.Partitions)
                await ProbePartitionAsync(disk, partition, volumes, problems, cancellationToken).ConfigureAwait(false);

            return new FatDiskLayout
            {
                PartitionTable = PartitionTableKind.Mbr,
                DiskSignature = record.DiskSignature,
                Partitions = record.Partitions,
                Volumes = volumes,
                Problems = problems
            };
        }

        private static bool IsTableBehindBootSector(byte[] sector, MasterBootRecord record)
        {
            var boot = FatBootSector.TryParse(sector);
            return boot != null
                   && boot.HiddenSectors != 0
                   && record.Partitions.Any(partition => partition.FirstSector == boot.HiddenSectors);
        }

        private static async ValueTask ProbePartitionAsync(IBlockDevice disk, MbrPartitionEntry partition,
            List<FatVolumeLocation> volumes, List<FatPartitionProblem> problems, CancellationToken cancellationToken)
        {
            if (partition.IsExtended || partition.SectorCount == 0)
                return;

            if (partition.FirstSector == 0 || partition.FirstSector > disk.SectorCount - partition.SectorCount)
            {
                problems.Add(new FatPartitionProblem
                {
                    Partition = partition,
                    Kind = FatErrorKind.Corrupt,
                    Message = $"Partition {partition.Index} spans sectors {partition.FirstSector} to " +
                              $"{partition.FirstSector + partition.SectorCount - 1}, which does not fit between " +
                              $"the partition table and the end of the disk of {disk.SectorCount} sectors."
                });
                return;
            }

            await using var window = new BlockDevicePartition(disk, partition.FirstSector, partition.SectorCount);
            try
            {
                var volume = await DetectVolumeAsync(window, cancellationToken).ConfigureAwait(false);
                if (volume != null)
                {
                    volumes.Add(new FatVolumeLocation
                    {
                        Partition = partition,
                        FirstSector = partition.FirstSector,
                        SectorCount = partition.SectorCount,
                        Volume = volume
                    });
                }
            }
            catch (FatException error)
            {
                problems.Add(new FatPartitionProblem { Partition = partition, Kind = error.Kind, Message = error.Message });
            }
        }

        private static async ValueTask<FatVolumeInfo?> DescribeAsync(IBlockDevice device, byte[] sector, CancellationToken cancellationToken)
        {
            var fat = FatBootSector.TryParse(sector);
            if (fat != null)
                return fat.Describe(device.SectorSize, device.SectorCount);

            var exFat = ExFatBootSector.TryParse(sector);
            if (exFat == null)
                return null;

            exFat.CheckSectorSize(device.SectorSize);
            if (device.SectorCount < ExFatBootSector.BOOT_REGION_SECTORS)
                throw new FatException(FatErrorKind.Corrupt,
                    $"The device has {device.SectorCount} sectors, fewer than an exFAT boot region.");

            var region = new byte[ExFatBootSector.BOOT_REGION_SECTORS * device.SectorSize];
            sector.CopyTo(region, 0);
            await device.ReadAsync(1, region.AsMemory(device.SectorSize), cancellationToken).ConfigureAwait(false);

            return exFat.Describe(region, device.SectorCount);
        }

        #endregion
    }
}
