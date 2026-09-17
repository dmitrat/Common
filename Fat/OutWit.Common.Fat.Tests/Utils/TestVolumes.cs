using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Utils
{
    /// <summary>
    /// Volumes to write to: blank ones of an exact size, and copies of reference images.
    /// </summary>
    internal static class TestVolumes
    {
        #region Constants

        public static readonly DateTime NOW = new(2026, 9, 16, 12, 30, 44);

        #endregion

        #region Functions

        /// <summary>
        /// Mounts a blank volume that stays in memory after the volume is disposed.
        /// </summary>
        public static async Task<(BlockDeviceMemory Disk, FatVolume Volume)> BlankAsync(FatKind kind, long clusters = 0,
            int sectorsPerCluster = 1, int rootEntries = 16, TimeProvider? clock = null, int fatCount = 2)
        {
            if (clusters == 0)
                clusters = kind switch { FatKind.Fat12 => 200, FatKind.Fat16 => 4200, _ => 300 };

            var disk = kind == FatKind.ExFat
                ? await BlankVolumeExFat.CreateAsync(clusters, sectorsPerCluster, fatCount: 1)
                : await BlankVolume.CreateAsync(kind, clusters, sectorsPerCluster, rootEntries, fatCount);
            var volume = await FatVolume.MountAsync(disk, Options(clock));
            Assert.That(volume.Info.Kind, Is.EqualTo(kind));
            return (disk, volume);
        }

        /// <summary>
        /// Mounts a writable copy of a reference image.
        /// </summary>
        public static async Task<(BlockDeviceMemory Disk, FatVolume Volume)> ReferenceAsync(string name, TimeProvider? clock = null)
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get(name), isReadOnly: false);
            return (disk, await FatVolume.MountDiskAsync(disk, Options(clock)));
        }

        /// <summary>
        /// Mounts a disk again, as a fresh process would see it.
        /// </summary>
        public static async Task<FatVolume> RemountAsync(IBlockDevice disk)
        {
            return await FatVolume.MountDiskAsync(disk, Options(null));
        }

        /// <summary>
        /// The parts of a blank volume below <see cref="FatVolume"/>.
        /// </summary>
        public static async Task<(BlockDeviceProbe Probe, FatVolumeCore Core, FatNamespace Names)> CoreAsync(FatKind kind,
            long clusters = 0, int rootEntries = 16)
        {
            if (clusters == 0)
                clusters = kind switch { FatKind.Fat12 => 200, FatKind.Fat16 => 4200, _ => 300 };

            var probe = new BlockDeviceProbe(kind == FatKind.ExFat
                ? await BlankVolumeExFat.CreateAsync(clusters)
                : await BlankVolume.CreateAsync(kind, clusters, rootEntries: rootEntries));
            var info = (await FatDetector.DetectVolumeAsync(probe))!;
            var core = new FatVolumeCore(probe, info, new FixedClock(NOW));
            var root = new FatDirectoryEntry { Attributes = FatAttributes.Directory, FirstCluster = info.RootCluster ?? 0 };
            return (probe, core, new FatNamespace(core, root));
        }

        public static FatVolumeOptions Options(TimeProvider? clock)
        {
            return new FatVolumeOptions { LeaveOpen = true, Clock = clock ?? new FixedClock(NOW) };
        }

        #endregion
    }
}
