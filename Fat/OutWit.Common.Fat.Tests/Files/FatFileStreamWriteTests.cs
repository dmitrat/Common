using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.Files
{
    /// <summary>
    /// Writing through <see cref="OutWit.Common.Fat.Files.FatFileStream"/>.
    /// </summary>
    [TestFixture]
    public class FatFileStreamWriteTests
    {
        #region Write Tests

        [TestCase(FatKind.Fat12, 1)]
        [TestCase(FatKind.Fat12, 511)]
        [TestCase(FatKind.Fat16, 512)]
        [TestCase(FatKind.Fat16, 513)]
        [TestCase(FatKind.Fat32, 5000)]
        public async Task WrittenBytesReadBackTest(FatKind kind, int length)
        {
            var (disk, volume) = await TestVolumes.BlankAsync(kind);
            var data = FatVolumeOracleTests.Pattern("data", length);

            await using (var stream = await volume.OpenAsync("/file.bin", FileMode.CreateNew, FileAccess.Write))
            {
                await stream.WriteAsync(data.AsMemory(0, length / 2));
                await stream.WriteAsync(data.AsMemory(length / 2));
                Assert.That(stream.Position, Is.EqualTo(length));
                Assert.That(stream.Length, Is.EqualTo(length));
            }

            await volume.DisposeAsync();
            await using var reopened = await TestVolumes.RemountAsync(disk);
            Assert.That(await reopened.ReadAllBytesAsync("/file.bin"), Is.EqualTo(data));
            Assert.That((await reopened.GetEntryAsync("/file.bin"))!.Length, Is.EqualTo(length));
        }

        [Test]
        public async Task OverwriteInTheMiddleKeepsTheRestTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            var data = FatVolumeOracleTests.Pattern("base", 3000);
            await volume.WriteAllBytesAsync("/file.bin", data);

            await using (var stream = await volume.OpenAsync("/file.bin", FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Position = 1000;
                await stream.WriteAsync(new byte[] { 1, 2, 3 });
            }

            data[1000] = 1;
            data[1001] = 2;
            data[1002] = 3;
            Assert.That(await volume.ReadAllBytesAsync("/file.bin"), Is.EqualTo(data));
        }

        [Test]
        public async Task WritePastTheEndFillsTheGapWithZerosTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await disk.WriteAsync(volume.Info.ClusterHeapSector, Enumerable.Repeat((byte)0xEE, 20 * 512).ToArray());
            await using var _ = volume;

            await using (var stream = await volume.OpenAsync("/sparse.bin", FileMode.CreateNew, FileAccess.Write))
            {
                stream.Seek(2000, SeekOrigin.Begin);
                await stream.WriteAsync(new byte[] { 7 });
            }

            var expected = new byte[2001];
            expected[2000] = 7;
            Assert.That(await volume.ReadAllBytesAsync("/sparse.bin"), Is.EqualTo(expected));
        }

        [Test]
        public async Task ShorterThenLongerBringsBackZerosTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file.bin", Enumerable.Repeat((byte)0xAB, 2000).ToArray());

            await using (var stream = await volume.OpenAsync("/file.bin", FileMode.Open, FileAccess.ReadWrite))
            {
                stream.Position = 1500;
                await stream.SetLengthAsync(10);
                Assert.That(stream.Position, Is.EqualTo(10));
                await stream.SetLengthAsync(700);
            }

            var expected = new byte[700];
            expected.AsSpan(0, 10).Fill(0xAB);
            Assert.That(await volume.ReadAllBytesAsync("/file.bin"), Is.EqualTo(expected));
        }

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat32)]
        public async Task ShrinkingReleasesClustersTest(FatKind kind)
        {
            var (_, volume) = await TestVolumes.BlankAsync(kind);
            await using var _ = volume;
            long free = await volume.CountFreeClustersAsync();
            await volume.WriteAllBytesAsync("/file.bin", new byte[10 * 512]);
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free - 10));

            await using (var stream = await volume.OpenAsync("/file.bin", FileMode.Open, FileAccess.Write))
                await stream.SetLengthAsync(513);
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free - 2));

            await using (var stream = await volume.OpenAsync("/file.bin", FileMode.Truncate, FileAccess.Write))
                Assert.That(stream.Length, Is.Zero);
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free));
            Assert.That((await volume.GetEntryAsync("/file.bin"))!.FirstCluster, Is.Zero);
        }

        [Test]
        public async Task SynchronousMembersWorkTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;

            using (var stream = volume.OpenAsync("/sync.txt", FileMode.Create, FileAccess.ReadWrite).AsTask().Result)
            {
                stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
                stream.Write(new ReadOnlySpan<byte>(new byte[] { 4 }));
                stream.WriteByte(5);
                stream.Flush();
                stream.SetLength(4);
                stream.Position = 0;
                Assert.That(stream.ReadByte(), Is.EqualTo(1));
            }

            Assert.That(await volume.ReadAllBytesAsync("/sync.txt"), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
        }

        #endregion

        #region Entry Tests

        [Test]
        public async Task EntryChangesOnFlushTest()
        {
            var clock = new FixedClock(new DateTime(2026, 1, 2, 3, 4, 6));
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16, clock: clock);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file.txt", new byte[] { 1 });
            clock.Now = new DateTime(2026, 5, 6, 7, 8, 10);

            await using var stream = await volume.OpenAsync("/file.txt", FileMode.Append, FileAccess.Write);
            await stream.WriteAsync(new byte[] { 2, 3 });
            Assert.That((await volume.GetEntryAsync("/file.txt"))!.Length, Is.EqualTo(1));

            await stream.FlushAsync();
            var entry = (await volume.GetEntryAsync("/file.txt"))!;
            Assert.That(entry.Length, Is.EqualTo(3));
            Assert.That(entry.Modified, Is.EqualTo(clock.Now));
            Assert.That(entry.Created, Is.EqualTo(new DateTime(2026, 1, 2, 3, 4, 6)));
            Assert.That(entry.Attributes, Is.EqualTo(FatAttributes.Archive));
            Assert.That(stream.Entry, Was.EqualTo(entry));
        }

        [Test]
        public async Task UnchangedFileIsNotWrittenTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.WriteAllBytesAsync("/file.txt", new byte[] { 1 });
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            await using var reopened = await FatVolume.MountAsync(probe, TestVolumes.Options(null));

            await using (var stream = await reopened.OpenAsync("/file.txt", FileMode.Open, FileAccess.ReadWrite))
                Assert.That(stream.ReadByte(), Is.EqualTo(1));

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
            Assert.That(probe.Of(BlockDeviceOperation.Flush), Is.Empty);
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task FailedWriteKeepsThePositionAndTheClustersTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            await using var reopened = await FatVolume.MountAsync(probe, TestVolumes.Options(null));
            long free = await reopened.CountFreeClustersAsync();

            await using (var stream = await reopened.OpenAsync("/file.bin", FileMode.CreateNew, FileAccess.Write))
            {
                probe.FailingWrites = 1;
                Assert.ThrowsAsync<IOException>(async () => await stream.WriteAsync(new byte[4000]));
                Assert.That((stream.Position, stream.Length), Is.EqualTo((0L, 0L)));
            }

            Assert.That((await reopened.GetEntryAsync("/file.bin"))!.FirstCluster, Is.Zero);
            Assert.That(await reopened.CountFreeClustersAsync(), Is.EqualTo(free));
        }

        [Test]
        public async Task StreamWorksAfterAFailedWriteTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await volume.DisposeAsync();
            var probe = new BlockDeviceProbe(disk);
            await using var reopened = await FatVolume.MountAsync(probe, TestVolumes.Options(null));

            await using (var stream = await reopened.OpenAsync("/file.bin", FileMode.CreateNew, FileAccess.Write))
            {
                probe.FailingWrites = 1;
                Assert.ThrowsAsync<IOException>(async () => await stream.WriteAsync(new byte[] { 9, 9 }));
                await stream.WriteAsync(new byte[] { 1, 2 });
            }

            Assert.That(await reopened.ReadAllBytesAsync("/file.bin"), Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public async Task FullVolumeKeepsTheFileAsItWasTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12, clusters: 20);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/keep.bin", new byte[] { 5 });
            long free = await volume.CountFreeClustersAsync();

            await using (var stream = await volume.OpenAsync("/keep.bin", FileMode.Append, FileAccess.Write))
            {
                var error = Assert.ThrowsAsync<FatException>(async () => await stream.WriteAsync(new byte[20 * 512]));
                Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            }

            Assert.That(await volume.ReadAllBytesAsync("/keep.bin"), Is.EqualTo(new byte[] { 5 }));
            Assert.That(await volume.CountFreeClustersAsync(), Is.EqualTo(free));
        }

        [TestCase(FileMode.Create)]
        [TestCase(FileMode.Truncate)]
        public async Task RewritingTheFileThatFillsTheVolumeFitsTest(FileMode mode)
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat12, clusters: 20);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/all.bin", new byte[20 * 512]);
            Assert.That(await volume.CountFreeClustersAsync(), Is.Zero);

            var data = FatVolumeOracleTests.Pattern("anew", 19 * 512 + 1);
            await using (var stream = await volume.OpenAsync("/all.bin", mode, FileAccess.Write))
                await stream.WriteAsync(data);

            Assert.That(await volume.ReadAllBytesAsync("/all.bin"), Is.EqualTo(data));
            Assert.That(await volume.CountFreeClustersAsync(), Is.Zero);
        }

        [Test]
        public async Task FileCannotPassFourGibibytesTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await using var stream = await volume.OpenAsync("/big.bin", FileMode.CreateNew, FileAccess.Write);

            var bySize = Assert.ThrowsAsync<FatException>(async () => await stream.SetLengthAsync(uint.MaxValue + 1L));
            stream.Position = uint.MaxValue;
            var byWrite = Assert.ThrowsAsync<FatException>(async () => await stream.WriteAsync(new byte[1]));

            Assert.That(bySize!.Kind, Is.EqualTo(FatErrorKind.TooLarge));
            Assert.That(byWrite!.Kind, Is.EqualTo(FatErrorKind.TooLarge));
            Assert.That(stream.Length, Is.Zero);
        }

        #endregion

        #region Mode Tests

        [Test]
        public async Task AppendingCannotGoBackTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/log.txt", new byte[] { 1, 2 });

            await using (var stream = await volume.OpenAsync("/log.txt", FileMode.Append, FileAccess.Write))
            {
                Assert.That(stream.Position, Is.EqualTo(2));
                Assert.Throws<IOException>(() => stream.Seek(1, SeekOrigin.Begin));
                Assert.ThrowsAsync<IOException>(async () => await stream.SetLengthAsync(1));
                stream.Seek(4, SeekOrigin.Begin);
                await stream.WriteAsync(new byte[] { 3 });
                Assert.That(stream.CanRead, Is.False);
            }

            Assert.That(await volume.ReadAllBytesAsync("/log.txt"), Is.EqualTo(new byte[] { 1, 2, 0, 0, 3 }));
        }

        [Test]
        public async Task AccessLimitsTheStreamTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file.txt", new byte[] { 1 });

            await using (var reader = await volume.OpenReadAsync("/file.txt"))
            {
                Assert.That((reader.CanRead, reader.CanWrite), Is.EqualTo((true, false)));
                Assert.ThrowsAsync<NotSupportedException>(async () => await reader.WriteAsync(new byte[1]));
                Assert.ThrowsAsync<NotSupportedException>(async () => await reader.SetLengthAsync(0));
            }

            await using (var writer = await volume.OpenAsync("/file.txt", FileMode.Open, FileAccess.Write))
            {
                Assert.That((writer.CanRead, writer.CanWrite), Is.EqualTo((false, true)));
                Assert.ThrowsAsync<NotSupportedException>(async () => await writer.ReadAsync(new byte[1]));
            }
        }

        [Test]
        public async Task WriterExcludesEveryOtherStreamTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            await volume.WriteAllBytesAsync("/file.txt", new byte[] { 1 });

            await using (var first = await volume.OpenReadAsync("/file.txt"))
            await using (var second = await volume.OpenReadAsync("/file.txt"))
            {
                var writer = Assert.ThrowsAsync<FatException>(async () => await volume.OpenAsync("/file.txt", FileMode.Open, FileAccess.Write));
                Assert.That(writer!.Kind, Is.EqualTo(FatErrorKind.InUse));
            }

            await using (var writer = await volume.OpenAsync("/FILE.TXT", FileMode.Open, FileAccess.ReadWrite))
            {
                var reader = Assert.ThrowsAsync<FatException>(async () => await volume.OpenReadAsync("/file.txt"));
                Assert.That(reader!.Kind, Is.EqualTo(FatErrorKind.InUse));
            }

            await using var again = await volume.OpenAsync("/file.txt", FileMode.Open, FileAccess.Write);
            Assert.That(again.CanWrite, Is.True);
        }

        [Test]
        public async Task DisposedStreamRefusesWorkTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.Fat16);
            await using var _ = volume;
            var stream = await volume.OpenAsync("/file.txt", FileMode.Create, FileAccess.ReadWrite);
            await stream.DisposeAsync();

            Assert.That((stream.CanRead, stream.CanWrite, stream.CanSeek), Is.EqualTo((false, false, false)));
            Assert.ThrowsAsync<ObjectDisposedException>(async () => await stream.WriteAsync(new byte[1]));
            Assert.Throws<ObjectDisposedException>(() => stream.Flush());
            Assert.DoesNotThrowAsync(async () => await stream.DisposeAsync());
            Assert.DoesNotThrow(stream.Dispose);
        }

        #endregion
    }
}
