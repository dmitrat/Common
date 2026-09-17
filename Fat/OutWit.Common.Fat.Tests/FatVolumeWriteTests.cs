using System.Buffers.Binary;
using OutWit.Common.Fat.Tests.Files;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    /// <summary>
    /// Creating, deleting and moving entries.
    /// </summary>
    [TestFixture]
    public class FatVolumeWriteTests
    {
        #region Directory Tests

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task DirectoryIsCreatedWithItsParentsTest(FatKind kind)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(kind);

            var created = await volume.CreateDirectoryAsync("/One/Two/three");
            var again = await volume.CreateDirectoryAsync("/one/TWO/three");

            await volume.DisposeAsync();
            await using var reopened = await TestVolumes.RemountAsync(disk);
            var listing = await reopened.EnumerateAsync(recursive: true).Select(entry => entry.Path).ToListAsync();
            Assert.That(listing, Is.EqualTo(new[] { "/One", "/One/Two", "/One/Two/three" }));
            Assert.That(created.Path, Is.EqualTo("/One/Two/three"));
            Assert.That(again.FirstCluster, Is.EqualTo(created.FirstCluster));
            Assert.That(created.Created, Is.EqualTo(TestVolumes.NOW));
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task DotEntriesPointAtTheRightClustersTest(FatKind kind)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(kind);
            await using var _ = volume;

            var top = await volume.CreateDirectoryAsync("/top");
            var inner = await volume.CreateDirectoryAsync("/top/inner");

            Assert.That(await DotClustersAsync(volume, top), Is.EqualTo((top.FirstCluster, 0u)));
            Assert.That(await DotClustersAsync(volume, inner), Is.EqualTo((inner.FirstCluster, top.FirstCluster)));
        }

        [Test]
        public async Task FileInTheWayIsReportedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file", new byte[] { 1 });

            var here = Assert.ThrowsAsync<FatException>(async () => await volume.CreateDirectoryAsync("/FILE"));
            var through = Assert.ThrowsAsync<FatException>(async () => await volume.CreateDirectoryAsync("/file/sub"));

            Assert.That(here!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(through!.Kind, Is.EqualTo(FatErrorKind.NotADirectory));
        }

        [Test]
        public async Task FullVolumeLeavesNoHalfMadeDirectoryTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12, clusters: 2);
            await using var _ = volume;

            await volume.CreateDirectoryAsync("/a");
            await volume.CreateDirectoryAsync("/b");
            var error = Assert.ThrowsAsync<FatException>(async () => await volume.CreateDirectoryAsync("/c"));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(await volume.EnumerateAsync().Select(entry => entry.Name).ToListAsync(), Is.EqualTo(new[] { "a", "b" }));
            Assert.That(await volume.CountFreeClustersAsync(), Is.Zero);
        }

        [Test]
        public async Task FullFixedRootIsReportedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12, rootEntries: 224);
            await using var _ = volume;
            for (int i = 0; i < 224; i++)
                await volume.WriteAllBytesAsync($"/F{i:D3}.TXT", Array.Empty<byte>());

            var file = Assert.ThrowsAsync<FatException>(async () => await volume.WriteAllBytesAsync("/F224.TXT", new byte[1]));
            var directory = Assert.ThrowsAsync<FatException>(async () => await volume.CreateDirectoryAsync("/D"));
            await volume.DeleteAsync("/F100.TXT");
            await volume.WriteAllBytesAsync("/NEW.TXT", new byte[1]);

            Assert.That(file!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(directory!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.EqualTo(224));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(199));
        }

        #endregion

        #region Delete Tests

        [Test]
        public async Task DeletedFileGivesBackItsClustersTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            long free = await volume.CountFreeClustersAsync();
            await volume.WriteAllBytesAsync("/Some file.bin", new byte[5000]);

            await volume.DeleteAsync("/SOME FILE.BIN");

            Assert.That(await volume.ExistsAsync("/Some file.bin"), Is.False);
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free));
        }

        [Test]
        public async Task DirectoryWithEntriesNeedsRecursionTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat32);
            await using var _ = volume;
            long free = await volume.CountFreeClustersAsync();
            await volume.CreateDirectoryAsync("/tree/branch");
            await volume.WriteAllBytesAsync("/tree/branch/leaf.bin", new byte[3000]);
            await volume.WriteAllBytesAsync("/tree/file.txt", new byte[10]);

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/tree"));
            await volume.DeleteAsync("/tree", recursive: true);

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NotEmpty));
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.Zero);
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free));
        }

        [Test]
        public async Task EmptyDirectoryIsDeletedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12);
            await using var _ = volume;
            await volume.CreateDirectoryAsync("/empty");

            await volume.DeleteAsync("/empty");

            Assert.That(await volume.ExistsAsync("/empty"), Is.False);
        }

        [Test]
        public async Task ProtectedEntriesAreNotDeletedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/locked.txt", new byte[1]);
            await FatFileOpenerTests.MarkReadOnlyAsync(volume, "/locked.txt");
            await volume.WriteAllBytesAsync("/open.txt", new byte[1]);
            await using var open = await volume.OpenReadAsync("/open.txt");

            var locked = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/locked.txt"));
            var busy = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/open.txt"));
            var missing = Assert.ThrowsAsync<FatException>(async () => await volume.DeleteAsync("/missing.txt"));

            Assert.That(locked!.Kind, Is.EqualTo(FatErrorKind.AccessDenied));
            Assert.That(busy!.Kind, Is.EqualTo(FatErrorKind.InUse));
            Assert.That(missing!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            Assert.ThrowsAsync<ArgumentException>(async () => await volume.DeleteAsync("/", recursive: true));
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.EqualTo(2));
        }

        #endregion

        #region Move Tests

        [Test]
        public async Task RenameKeepsDataAttributesAndTimesTest()
        {
            var clock = new FixedClock(TestVolumes.NOW);
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16, clock: clock);
            await using var _ = volume;
            var before = await volume.WriteAllBytesAsync("/old.txt", new byte[] { 1, 2 });
            clock.Now = TestVolumes.NOW.AddDays(1);

            var after = await volume.MoveAsync("/old.txt", "/A much longer new name.txt");

            Assert.That(after.Path, Is.EqualTo("/A much longer new name.txt"));
            Assert.That(after.ShortName, Is.EqualTo("AMUCHL~1.TXT"));
            Assert.That((after.FirstCluster, after.Length, after.Attributes), Is.EqualTo((before.FirstCluster, before.Length, before.Attributes)));
            Assert.That((after.Created, after.Modified), Is.EqualTo((before.Created, before.Modified)));
            Assert.That(await volume.ExistsAsync("/old.txt"), Is.False);
            Assert.That(await volume.ReadAllBytesAsync("/a much longer new name.txt"), Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public async Task CaseOnlyRenameTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/readme.txt", new byte[] { 1 });

            var upper = await volume.MoveAsync("/readme.txt", "/README.TXT");
            var same = await volume.MoveAsync("/README.TXT", "/README.TXT");
            var mixed = await volume.MoveAsync("/readme.TXT", "/ReadMe.txt");

            Assert.That((upper.Name, upper.ShortName), Is.EqualTo(("README.TXT", "README.TXT")));
            Assert.That(same.Name, Is.EqualTo("README.TXT"));
            Assert.That((mixed.Name, mixed.ShortName), Is.EqualTo(("ReadMe.txt", "README.TXT")));
            Assert.That(await volume.EnumerateAsync().Select(entry => entry.Name).ToListAsync(), Is.EqualTo(new[] { "ReadMe.txt" }));
        }

        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task MovedDirectoryFollowsItsNewParentTest(FatKind kind)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind);
            await using var _ = volume;
            var target = await volume.CreateDirectoryAsync("/target");
            await volume.CreateDirectoryAsync("/moving/child");
            await volume.WriteAllBytesAsync("/moving/child/file.txt", new byte[] { 4 });

            var moved = await volume.MoveAsync("/moving", "/target/moved");
            var child = (await volume.GetEntryAsync("/target/moved/child"))!;
            Assert.That(await DotClustersAsync(volume, moved), Is.EqualTo((moved.FirstCluster, target.FirstCluster)));

            var back = await volume.MoveAsync("/target/moved", "/back");
            Assert.That(back.FirstCluster, Is.EqualTo(moved.FirstCluster));
            Assert.That(await DotClustersAsync(volume, back), Is.EqualTo((back.FirstCluster, 0u)));
            Assert.That(await DotClustersAsync(volume, child), Is.EqualTo((child.FirstCluster, moved.FirstCluster)));
            Assert.That(await volume.ReadAllBytesAsync("/back/child/file.txt"), Is.EqualTo(new byte[] { 4 }));
        }

        [Test]
        public async Task DirectoryCannotMoveIntoItselfTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.CreateDirectoryAsync("/a/b/c");

            Assert.ThrowsAsync<ArgumentException>(async () => await volume.MoveAsync("/a", "/a/inside"));
            Assert.ThrowsAsync<ArgumentException>(async () => await volume.MoveAsync("/a", "/A/b/c/deeper"));
            Assert.That(await volume.EnumerateAsync(recursive: true).CountAsync(), Is.EqualTo(3));
        }

        [Test]
        public async Task ReplacingNeedsPermissionTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            long free = await volume.CountFreeClustersAsync();
            await volume.WriteAllBytesAsync("/source.bin", new byte[600]);
            await volume.WriteAllBytesAsync("/target.bin", new byte[3000]);
            await volume.CreateDirectoryAsync("/dir");

            var refused = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/source.bin", "/target.bin"));
            var overDirectory = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/source.bin", "/dir", overwrite: true));
            var directoryOver = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/dir", "/target.bin", overwrite: true));
            await volume.MoveAsync("/source.bin", "/TARGET.BIN", overwrite: true);

            Assert.That(refused!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(overDirectory!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(directoryOver!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(await volume.ReadAllBytesAsync("/target.bin"), Has.Length.EqualTo(600));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free - 3));
        }

        [Test]
        public async Task ReplacedFileComesBackWhenTheMoveDoesNotFitTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12, rootEntries: 16);
            await using var _ = volume;
            for (int i = 0; i < 15; i++)
                await volume.WriteAllBytesAsync($"/F{i:D2}.TXT", new byte[] { (byte)i });
            await volume.WriteAllBytesAsync("/readme.txt", new byte[] { 99 });

            var error = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/F00.TXT", "/ReadMe.txt", overwrite: true));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(await volume.ReadAllBytesAsync("/readme.txt"), Is.EqualTo(new byte[] { 99 }));
            Assert.That(await volume.ReadAllBytesAsync("/F00.TXT"), Is.EqualTo(new byte[] { 0 }));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task MoveProblemsAreReportedTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file.txt", new byte[1]);
            await volume.WriteAllBytesAsync("/free.txt", new byte[1]);
            await using var open = await volume.OpenReadAsync("/file.txt");

            var busy = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/file.txt", "/other.txt"));
            var missing = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/nothing.txt", "/other.txt"));
            var nowhere = Assert.ThrowsAsync<FatException>(async () => await volume.MoveAsync("/free.txt", "/no/other.txt"));

            Assert.That(busy!.Kind, Is.EqualTo(FatErrorKind.InUse));
            Assert.That(missing!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            Assert.That(nowhere!.Kind, Is.EqualTo(FatErrorKind.NotFound));
            Assert.ThrowsAsync<ArgumentException>(async () => await volume.MoveAsync("/", "/x"));
            Assert.ThrowsAsync<ArgumentException>(async () => await volume.MoveAsync("/free.txt", "/bad|name"));
            Assert.That(await volume.ExistsAsync("/free.txt"), Is.True);
        }

        #endregion

        #region Lifetime Tests

        [Test]
        public async Task ReadOnlyDeviceRefusesChangesTest()
        {
            var image = await ReferenceImages.OpenAsync(ReferenceImages.Get("fat12-floppy"));
            await using var volume = await FatVolume.MountAsync(image);

            Assert.That(volume.IsReadOnly, Is.True);
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.CreateDirectoryAsync("/new"));
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.DeleteAsync("/README.TXT"));
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.MoveAsync("/README.TXT", "/x.txt"));
            Assert.ThrowsAsync<NotSupportedException>(async () => await volume.WriteAllBytesAsync("/x.txt", new byte[1]));
            Assert.DoesNotThrowAsync(async () => await volume.FlushAsync());
        }

        [Test]
        public async Task DisposingTheVolumeClosesItsWritersTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            var writer = await volume.OpenAsync("/unfinished.txt", FileMode.CreateNew, FileAccess.Write);
            await writer.WriteAsync(new byte[] { 1, 2, 3 });
            var reader = await volume.WriteAllBytesAsync("/other.txt", new byte[1]);
            await using var open = await volume.OpenReadAsync(reader.Path);

            await volume.DisposeAsync();

            Assert.That(writer.CanWrite, Is.False);
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await open.ReadAsync(new byte[1]));
            await using var reopened = await TestVolumes.RemountAsync(disk);
            Assert.That(await reopened.ReadAllBytesAsync("/unfinished.txt"), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public async Task FlushWritesOpenFilesTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat32);
            await using var _ = volume;
            await using var writer = await volume.OpenAsync("/growing.txt", FileMode.CreateNew, FileAccess.Write);
            await writer.WriteAsync(new byte[] { 1, 2, 3 });

            await volume.FlushAsync();

            await using var copy = await TestVolumes.RemountAsync(disk);
            Assert.That(await copy.ReadAllBytesAsync("/growing.txt"), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(await BlankVolume.ReadFsInfoAsync(disk), Is.EqualTo((298u, 4u)));
        }

        [Test]
        public async Task FailedDeviceReleaseCanBeRetriedTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            var owner = await FatVolume.MountAsync(probe);
            var writer = await owner.OpenAsync("/late.txt", FileMode.CreateNew, FileAccess.Write);
            await writer.WriteAsync(new byte[] { 2 });

            probe.FailingDisposals = 1;
            Assert.ThrowsAsync<IOException>(async () => await owner.DisposeAsync());
            Assert.That(probe.IsDisposed, Is.False);
            Assert.That(writer.CanWrite, Is.False);
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await owner.GetEntryAsync("/late.txt"));

            await owner.DisposeAsync();
            Assert.That(probe.IsDisposed, Is.True);
        }

        #endregion

        #region Tools

        /// <summary>
        /// The clusters a directory's <c>.</c> and <c>..</c> entries point at.
        /// </summary>
        private static async Task<(uint Self, uint Parent)> DotClustersAsync(FatVolume volume, FatDirectoryEntry directory)
        {
            var slots = new byte[volume.Info.SectorSize];
            await volume.Device.ReadAsync(ImageSlots.ClusterSector(volume.Info, 0, directory.FirstCluster), slots);

            Assert.That(System.Text.Encoding.ASCII.GetString(slots, 0, 11), Is.EqualTo(".          "));
            Assert.That(System.Text.Encoding.ASCII.GetString(slots, 32, 11), Is.EqualTo("..         "));
            return (Cluster(slots.AsSpan(0, 32)), Cluster(slots.AsSpan(32, 32)));
        }

        private static uint Cluster(ReadOnlySpan<byte> slot)
        {
            return (uint)BinaryPrimitives.ReadUInt16LittleEndian(slot[20..]) << 16 | BinaryPrimitives.ReadUInt16LittleEndian(slot[26..]);
        }

        #endregion
    }
}
