using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Fat.Devices;

namespace OutWit.Common.Fat
{
    /// <summary>
    /// Finds the volume to mount and the devices the mounted volume owns.
    /// </summary>
    internal static class FatVolumeMounter
    {
        #region Functions

        /// <summary>
        /// Finds the volume at sector zero of a device.
        /// </summary>
        /// <exception cref="FatException">There is none.</exception>
        public static async ValueTask<(IBlockDevice Device, FatVolumeInfo Info, IAsyncDisposable[] Owned)> FindVolumeAsync(
            IBlockDevice device, bool leaveOpen, CancellationToken cancellationToken)
        {
            var info = await FatDetector.DetectVolumeAsync(device, cancellationToken).ConfigureAwait(false)
                       ?? throw new FatException(FatErrorKind.NotRecognized,
                           "The device holds no FAT or exFAT volume at sector zero; a partitioned disk is mounted with MountDiskAsync.");

            return (device, info, leaveOpen ? Array.Empty<IAsyncDisposable>() : new IAsyncDisposable[] { device });
        }

        /// <summary>
        /// Finds the first volume on a disk, and wraps its partition.
        /// </summary>
        /// <exception cref="FatException">There is none; the message names the partitions that were rejected.</exception>
        public static async ValueTask<(IBlockDevice Device, FatVolumeInfo Info, IAsyncDisposable[] Owned)> FindOnDiskAsync(
            IBlockDevice disk, bool leaveOpen, CancellationToken cancellationToken)
        {
            var layout = await FatDetector.DetectDiskAsync(disk, cancellationToken).ConfigureAwait(false);
            var location = layout.Volumes.FirstOrDefault();
            if (location == null)
            {
                string reasons = string.Concat(layout.Problems.Select(problem => $" Partition {problem.Partition.Index}: {problem.Message}"));
                throw new FatException(FatErrorKind.NotRecognized, "The disk holds no FAT or exFAT volume." + reasons);
            }

            var owned = new List<IAsyncDisposable>();
            IBlockDevice device = disk;
            if (location.Partition != null)
            {
                device = new BlockDevicePartition(disk, location.FirstSector, location.SectorCount);
                owned.Add(device);
            }

            if (!leaveOpen)
                owned.Add(disk);

            return (device, location.Volume, owned.ToArray());
        }

        #endregion
    }
}
