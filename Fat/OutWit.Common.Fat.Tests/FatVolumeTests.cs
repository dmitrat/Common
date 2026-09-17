using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatVolumeTests
    {
        #region Constants

        private static readonly FatVolumeOptions LEAVE_OPEN = new() { LeaveOpen = true };

        #endregion

        #region Mount Tests

        [Test]
        public async Task MountReadsOnlyTheBootSectorTest()
        {
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(ReferenceImages.Get("fat32-c1")));

            await using var volume = await FatVolume.MountAsync(probe);

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Read, 0, 1) }));
            Assert.That(volume.Info.Kind, Is.EqualTo(FatKind.Fat32));
            Assert.That(volume.Device, Is.SameAs(probe));
            Assert.That(volume.Root.Path, Is.EqualTo("/"));
            Assert.That(volume.Root.FirstCluster, Is.EqualTo(2));
        }

        [Test]
        public void BlankDeviceIsNotRecognisedTest()
        {
            var error = Assert.ThrowsAsync<FatException>(async () => await FatVolume.MountAsync(new BlockDeviceMemory(100)));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotRecognized));
        }

        [Test]
        public async Task PartitionedDiskIsMountedByDiskTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("fat16-mbr-c16"));

            var error = Assert.ThrowsAsync<FatException>(async () => await FatVolume.MountAsync(disk, LEAVE_OPEN));
            await using var volume = await FatVolume.MountDiskAsync(disk);

            Assert.That(error!.Message, Does.Contain(nameof(FatVolume.MountDiskAsync)));
            Assert.That(volume.Device, Is.InstanceOf<BlockDevicePartition>());
            Assert.That(await volume.GetLabelAsync(), Is.EqualTo("FAT16 MBR"));
        }

        [Test]
        public async Task ExFatIsMountedForWritingTest()
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get("exfat-c8"), isReadOnly: false);
            await using (var volume = await FatVolume.MountAsync(disk, LEAVE_OPEN))
            {
                Assert.That(volume.Info.Kind, Is.EqualTo(FatKind.ExFat));
                Assert.That(volume.IsReadOnly, Is.False);
                Assert.That(volume.Root.FirstCluster, Is.EqualTo(volume.Info.RootCluster));

                await volume.WriteAllBytesAsync("/data/new file.txt", "written"u8.ToArray());
                await volume.MoveAsync("/README.TXT", "/READ.ME");
            }

            await using var reopened = await FatVolume.MountAsync(disk, LEAVE_OPEN);
            Assert.That(await reopened.ReadAllBytesAsync("/DATA/NEW FILE.TXT"), Is.EqualTo("written"u8.ToArray()));
            Assert.That(await reopened.ExistsAsync("/README.TXT"), Is.False);
            Assert.That(await reopened.ReadAllBytesAsync("/read.me"), Has.Length.EqualTo(36));
        }

        [Test]
        public async Task ExFatOnAReadOnlyDeviceRefusesChangesTest()
        {
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(ReferenceImages.Get("exfat-c8")));
            await using var volume = await FatVolume.MountAsync(probe);

            Assert.That(volume.IsReadOnly, Is.True);
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.CreateAsync("/new.txt"));
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.DeleteAsync("/README.TXT"));
            await volume.FlushAsync();
            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        [Test]
        public async Task DiskWithoutUsableVolumeNamesTheRejectedPartitionsTest()
        {
            var broken = FatBootSectorBuilder.ForClusters(3000, false);
            broken.RootEntries = 0;
            await using var disk = new BlockDeviceMemory(10000);
            await disk.WriteAsync(0, MbrBuilder.Build(0, new MbrSlot(1, 0x01, 100, 5000)));
            await disk.WriteAsync(100, broken.Build());

            var error = Assert.ThrowsAsync<FatException>(async () => await FatVolume.MountDiskAsync(disk, LEAVE_OPEN));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotRecognized));
            Assert.That(error.Message, Does.Contain("Partition 1: ").And.Contain("no root directory"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task DiskMountTakesTheFirstVolumeOfAnyKindTest(bool isExFatFirst)
        {
            var exFat = new ExFatBootRegionBuilder();
            var fat = FatBootSectorBuilder.ForClusters(3000, false);
            await using var disk = new BlockDeviceMemory(90000);
            var exFatSlot = new MbrSlot(isExFatFirst ? 0 : 1, 0x07, 100, (uint)exFat.VolumeLength);
            var fatSlot = new MbrSlot(isExFatFirst ? 1 : 0, 0x01, 40000, (uint)fat.TotalSectors);
            await disk.WriteAsync(0, MbrBuilder.Build(0, isExFatFirst ? new[] { exFatSlot, fatSlot } : new[] { fatSlot, exFatSlot }));
            await disk.WriteAsync(100, exFat.Build());
            await disk.WriteAsync(40000, fat.Build());

            await using var volume = await FatVolume.MountDiskAsync(disk, LEAVE_OPEN);

            Assert.That(volume.Info.Kind, Is.EqualTo(isExFatFirst ? FatKind.ExFat : FatKind.Fat12));
            Assert.That(((BlockDevicePartition)volume.Device).FirstSector, Is.EqualTo(isExFatFirst ? 100 : 40000));
        }

        [Test]
        public void NullDeviceIsRejectedTest()
        {
            Assert.ThrowsAsync<ArgumentNullException>(async () => await FatVolume.MountAsync(null!));
            Assert.ThrowsAsync<ArgumentNullException>(async () => await FatVolume.MountDiskAsync(null!));
        }

        #endregion

        #region Path Tests

        [TestCase("/")]
        [TestCase("")]
        [TestCase("./.")]
        [TestCase("\\data\\..")]
        public async Task RootIsFoundByAnyOfItsNamesTest(string path)
        {
            await using var volume = await MountAsync("fat12-floppy");

            Assert.That(await volume.GetEntryAsync(path), Is.SameAs(volume.Root));
            Assert.That(await volume.ExistsAsync(path), Is.True);
        }

        [Test]
        public async Task MissingPathsAreNotFoundTest()
        {
            await using var volume = await MountAsync("fat12-floppy");

            Assert.That(await volume.GetEntryAsync("/nothing"), Is.Null);
            Assert.That(await volume.GetEntryAsync("/data/nothing"), Is.Null);
            Assert.That(await volume.GetEntryAsync("/README.TXT/inside"), Is.Null);
            Assert.That(await volume.ExistsAsync("/nothing"), Is.False);
        }

        [Test]
        public async Task WrongKindOfEntryIsReportedTest()
        {
            await using var volume = await MountAsync("fat12-floppy");

            var missing = Assert.ThrowsAsync<FatException>(async () => await volume.OpenReadAsync("/nothing"));
            var file = Assert.ThrowsAsync<FatException>(async () => await volume.EnumerateAsync("/README.TXT").ToListAsync());
            var directory = Assert.ThrowsAsync<FatException>(async () => await volume.ReadAllBytesAsync("/data"));

            Assert.That(missing!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            Assert.That(file!.Kind, Is.EqualTo(FatErrorKind.NotADirectory));
            Assert.That(directory!.Kind, Is.EqualTo(FatErrorKind.NotAFile));
        }

        [Test]
        public async Task PathAboveRootIsRejectedTest()
        {
            await using var volume = await MountAsync("fat12-floppy");

            Assert.ThrowsAsync<ArgumentException>(async () => await volume.GetEntryAsync("/.."));
        }

        [Test]
        public async Task ListingIsShallowByDefaultTest()
        {
            await using var volume = await MountAsync("fat12-floppy");

            var names = await volume.EnumerateAsync("/unicode").Select(e => e.Name).ToListAsync();

            Assert.That(names, Is.EquivalentTo(new[] { "Ünïcödé.txt", "日本語のファイル.txt", "Привет мир.txt", "emoji \U0001F600.txt" }));
        }

        [TestCase("fat16-c1", false)]
        [TestCase("fat32-c1", false)]
        [TestCase("fat32-c1", true)]
        public async Task DirectoryThatIsItsOwnAncestorStopsTheListingTest(string name, bool pointAtRoot)
        {
            var disk = await ReferenceImages.OpenAsync(ReferenceImages.Get(name), isReadOnly: false);
            await using var volume = await FatVolume.MountAsync(disk);
            var empty = await volume.GetEntryAsync("/Empty Directory");
            uint target = pointAtRoot ? volume.Root.FirstCluster : empty!.FirstCluster;
            long sector = ImageSlots.ClusterSector(volume.Info, 0, empty!.FirstCluster);
            await ImageSlots.WriteAsync(disk, sector * volume.Info.SectorSize + 2 * DirectorySlotBuilder.SLOT,
                DirectorySlotBuilder.Short("LOOP       ", FatAttributes.Directory, target));

            int listed = 0;
            var error = Assert.ThrowsAsync<FatException>(async () =>
            {
                await foreach (var _ in volume.EnumerateAsync("/", recursive: true))
                    listed++;
            });

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("/Empty Directory/LOOP").And.Contain("its own ancestors"));
            Assert.That(listed, Is.LessThan(1000));
            string deeper = pointAtRoot ? "/Empty Directory/LOOP/Empty Directory/LOOP" : "/Empty Directory/LOOP/LOOP/LOOP";
            Assert.That(await volume.GetEntryAsync(deeper), Is.Not.Null, "a path is finite, so a lookup through the loop still ends");
        }

        [TestCase(FileMode.Create, FileAccess.Write)]
        [TestCase(FileMode.Open, FileAccess.ReadWrite)]
        [TestCase(FileMode.Append, FileAccess.Write)]
        public async Task WritingIsNotSupportedYetTest(FileMode mode, FileAccess access)
        {
            await using var volume = await MountAsync("fat12-floppy");

            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.OpenAsync("/README.TXT", mode, access));
        }

        #endregion

        #region Lifetime Tests

        [TestCase(false)]
        [TestCase(true)]
        public async Task VolumeReleasesTheDeviceItOwnsTest(bool leaveOpen)
        {
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(ReferenceImages.Get("fat12-floppy")));

            await (await FatVolume.MountAsync(probe, new FatVolumeOptions { LeaveOpen = leaveOpen })).DisposeAsync();

            Assert.That(probe.IsDisposed, Is.EqualTo(!leaveOpen));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task DiskVolumeReleasesTheDiskItOwnsTest(bool leaveOpen)
        {
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(ReferenceImages.Get("fat32-mbr-c2")));

            await (await FatVolume.MountDiskAsync(probe, new FatVolumeOptions { LeaveOpen = leaveOpen })).DisposeAsync();

            Assert.That(probe.IsDisposed, Is.EqualTo(!leaveOpen));
        }

        [Test]
        public async Task FailedReleaseLeavesTheVolumeOpenTest()
        {
            var probe = new BlockDeviceProbe(await ReferenceImages.OpenAsync(ReferenceImages.Get("fat12-floppy"), isReadOnly: false));
            var cache = new BlockDeviceCached(probe);
            var volume = await FatVolume.MountAsync(cache);
            var boot = new byte[512];
            await cache.ReadAsync(0, boot);
            await cache.WriteAsync(0, boot);
            probe.FailingWrites = 1;

            Assert.ThrowsAsync<IOException>(async () => await volume.DisposeAsync());
            Assert.That(probe.IsDisposed, Is.False);
            Assert.That(await volume.GetLabelAsync(), Is.EqualTo("FLOPPY"));

            await volume.DisposeAsync();
            Assert.That(probe.IsDisposed, Is.True);
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await volume.GetLabelAsync());
        }

        [Test]
        public async Task DisposedVolumeRefusesWorkTest()
        {
            var volume = await MountAsync("fat12-floppy");
            await volume.DisposeAsync();

            Assert.ThrowsAsync<ObjectDisposedException>(async () => await volume.GetEntryAsync("/README.TXT"));
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await volume.GetLabelAsync());
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await volume.EnumerateAsync().ToListAsync());
            Assert.DoesNotThrowAsync(async () => await volume.DisposeAsync());
        }

        #endregion

        #region Tools

        private static async Task<FatVolume> MountAsync(string name)
        {
            return await FatVolume.MountDiskAsync(await ReferenceImages.OpenAsync(ReferenceImages.Get(name)));
        }

        #endregion
    }
}
