using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatDirectoryExFatTests
    {
        #region Constants

        private const int CLUSTER = FatTableImage.SECTOR_SIZE;

        #endregion

        #region Size Tests

        [Test]
        public async Task DirectoryIsReadToItsRecordedSizeTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10, 11, 12);
            volume.Put(10, ExFatTestVolume.FreeSlots(16))
                .Put(11, ExFatSetBuilder.Join(Set("a.txt"), ExFatTestVolume.FreeSlots(13)))
                .Put(12, Set("b.txt"));
            var probe = await volume.CreateAsync();
            var directory = volume.Core(probe).OpenDirectory(ExFatTestVolume.Directory(10, 2 * CLUSTER, isContiguous: false));

            var names = await directory.EnumerateAsync(CancellationToken.None).Select(item => item.Entry.Name).ToListAsync();

            Assert.That(names, Is.EqualTo(new[] { "a.txt" }));
            Assert.That(probe.Of(BlockDeviceOperation.Read).Where(r => r.Sector == volume.Image.ClusterSector(12)), Is.Empty);
            Assert.That(await directory.GetCapacityAsync(CancellationToken.None), Is.EqualTo(32));
        }

        [Test]
        public async Task ChainShorterThanTheRecordedSizeIsCorruptTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10, 11);
            volume.Put(10, ExFatTestVolume.FreeSlots(32));
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, 4 * CLUSTER, isContiguous: false));

            var error = Assert.ThrowsAsync<FatException>(async () => await directory.EnumerateAsync(CancellationToken.None).ToListAsync());

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("/dir ends after 2 clusters; it records 2048 bytes"));
        }

        [Test]
        public async Task OversizedDirectoryIsCorruptTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10);
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, 257L << 20, isContiguous: false));

            var error = Assert.ThrowsAsync<FatException>(async () => await directory.EnumerateAsync(CancellationToken.None).ToListAsync());

            Assert.That(error!.Message, Does.Contain("past the limit of 268435456"));
        }

        [Test]
        public async Task EmptyDirectoryHasNoClustersTest()
        {
            var volume = new ExFatTestVolume();
            var probe = await volume.CreateAsync();
            var directory = volume.Core(probe).OpenDirectory(ExFatTestVolume.Directory(0, 0, isContiguous: false));

            Assert.That(await directory.EnumerateAsync(CancellationToken.None).ToListAsync(), Is.Empty);
            Assert.That(await directory.FindAsync("x", CancellationToken.None), Is.Null);
            Assert.That(await directory.GetCapacityAsync(CancellationToken.None), Is.Zero);
            Assert.That((await directory.FindFreeAsync(3, CancellationToken.None)).Available, Is.Zero);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await directory.GetSlotOffsetAsync(0, CancellationToken.None));
        }

        [Test]
        public async Task RootIsReadToTheEndOfItsChainTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(ExFatTestVolume.ROOT, 20);
            volume.Put(ExFatTestVolume.ROOT, ExFatSetBuilder.Join(Set("a.txt"), ExFatTestVolume.FreeSlots(13))).Put(20, Set("b.txt"));
            var names = ExFatTestVolume.Names(volume.Core(await volume.CreateAsync()));

            var listed = await names.Open(names.Root).EnumerateAsync(CancellationToken.None).Select(item => item.Entry.Path).ToListAsync();

            Assert.That(listed, Is.EqualTo(new[] { "/a.txt", "/b.txt" }));
        }

        [Test]
        public async Task ChainLongerThanTheRecordedSizeIsFollowedNoFurtherTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10, 11, 12, 13, 14).Set(14, 0);
            volume.Put(10, ExFatTestVolume.FreeSlots(64));
            var probe = await volume.CreateAsync();
            var directory = volume.Core(probe).OpenDirectory(ExFatTestVolume.Directory(10, 4 * CLUSTER, isContiguous: false));

            Assert.That(await directory.EnumerateAsync(CancellationToken.None).ToListAsync(), Is.Empty);
        }

        [Test]
        public async Task SetCutShortByTheEndOfTheDirectoryIsCorruptTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10, 11);
            var slots = ExFatSetBuilder.Join(ExFatTestVolume.FreeSlots(14), Set("cut.txt"));
            volume.Put(10, slots[..CLUSTER]).Put(11, slots[CLUSTER..]);
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, CLUSTER, isContiguous: false));

            var error = Assert.ThrowsAsync<FatException>(async () => await directory.EnumerateAsync(CancellationToken.None).ToListAsync());

            Assert.That(error!.Message, Does.Contain("slot 14 of /dir is cut short by the end of the directory"));
        }

        [Test]
        public async Task LongRunIsNotCountedPastTheVolumeTest()
        {
            var volume = new ExFatTestVolume();
            var core = volume.Core(await volume.CreateAsync());

            var error = Assert.Throws<FatException>(() => core.OpenChain(ExFatTestVolume.Directory(10, long.MaxValue, isContiguous: true)));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        #endregion

        #region Broken Set Tests

        [Test]
        public async Task EntriesBeforeABrokenSetAreListedTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10);
            volume.Put(10, ExFatSetBuilder.Join(Set("good.txt"), Broken("bad.txt"), Set("after.txt")));
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, CLUSTER, isContiguous: false));

            var listed = new List<string>();
            var error = Assert.ThrowsAsync<FatException>(async () =>
            {
                await foreach (var item in directory.EnumerateAsync(CancellationToken.None))
                    listed.Add(item.Entry.Name);
            });

            Assert.That(listed, Is.EqualTo(new[] { "good.txt" }));
            Assert.That(error!.Message, Does.Contain("slot 3 of /dir does not match its checksum"));
        }

        [Test]
        public async Task LookupPassesOverABrokenSetTest()
        {
            var volume = new ExFatTestVolume().WithRoot(new byte[25], ExFatTestVolume.LETTERS);
            volume.Image.Chain(10);
            volume.Put(10, ExFatSetBuilder.Join(Set("good.txt"), Broken("bad.txt"), Set("after.txt")));
            var core = volume.Core(await volume.CreateAsync());
            var directory = core.OpenDirectory(ExFatTestVolume.Directory(10, CLUSTER, isContiguous: false));

            var before = await directory.FindAsync("GOOD.TXT", CancellationToken.None);
            var after = await directory.FindAsync("AFTER.TXT", CancellationToken.None);
            var missing = Assert.ThrowsAsync<FatException>(async () => await directory.FindAsync("missing.txt", CancellationToken.None));

            Assert.That((before?.FirstSlot, after?.FirstSlot), Is.EqualTo(((long?)0, (long?)6)));
            Assert.That(missing!.Message, Does.Contain("does not match its checksum"));
        }

        [Test]
        public async Task BrokenSetInTheRootHidesNothingElseTest()
        {
            var subdirectory = new ExFatSetBuilder { Name = "DIR", Attributes = FatAttributes.Directory, FirstCluster = 20, DataLength = CLUSTER, IsContiguous = true };
            var volume = new ExFatTestVolume().WithRoot(new byte[25], ExFatTestVolume.LETTERS, Broken("BAD.TXT"), subdirectory.Build());
            volume.Put(20, Set("a.txt"));
            var core = volume.Core(await volume.CreateAsync());
            var names = ExFatTestVolume.Names(core);

            var found = await names.FindAsync(new[] { "dir", "A.TXT" }, CancellationToken.None);

            Assert.That(found?.Entry.Path, Is.EqualTo("/DIR/a.txt"));
            Assert.That(await names.Open(names.Root).ReadLabelAsync(CancellationToken.None), Is.Null);
            Assert.That(await core.CountFreeClustersAsync(CancellationToken.None), Is.EqualTo(200));
            Assert.ThrowsAsync<FatException>(async () => await names.Open(names.Root).EnumerateAsync(CancellationToken.None).ToListAsync());
        }

        #endregion

        #region Contiguous Tests

        [Test]
        public async Task ContiguousDirectoryIsReadWithoutTheTableTest()
        {
            var volume = new ExFatTestVolume();
            volume.Put(20, ExFatSetBuilder.Join(Set("a.txt"), ExFatTestVolume.FreeSlots(13)))
                .Put(21, ExFatSetBuilder.Join(Set("b.txt"), ExFatTestVolume.FreeSlots(13)))
                .Put(22, Set("c.txt"));
            var probe = await volume.CreateAsync();
            var directory = volume.Core(probe).OpenDirectory(ExFatTestVolume.Directory(20, 3 * CLUSTER, isContiguous: true));

            var listed = await directory.EnumerateAsync(CancellationToken.None).Select(item => item.Entry.Name).ToListAsync();

            Assert.That(listed, Is.EqualTo(new[] { "a.txt", "b.txt", "c.txt" }));
            Assert.That(probe.Of(BlockDeviceOperation.Read).Where(r => r.Sector < volume.Image.Info.ClusterHeapSector), Is.Empty);
            Assert.That(await directory.GetSlotOffsetAsync(33, CancellationToken.None),
                Is.EqualTo(volume.Image.ClusterSector(22) * CLUSTER + 32));
        }

        [Test]
        public async Task ContiguousDirectoryPastTheVolumeIsCorruptTest()
        {
            var volume = new ExFatTestVolume();
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(199, 5 * CLUSTER, isContiguous: true));

            var error = Assert.ThrowsAsync<FatException>(async () => await directory.EnumerateAsync(CancellationToken.None).ToListAsync());

            Assert.That(error!.Message, Does.Contain("records 5 consecutive clusters from cluster 199"));
        }

        #endregion

        #region Slot Tests

        [Test]
        public async Task SetAcrossAClusterBoundaryIsReadTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10, 30);
            var slots = ExFatSetBuilder.Join(ExFatTestVolume.FreeSlots(15), Set("across.txt"), Set("after.txt"));
            volume.Put(10, slots[..CLUSTER]).Put(30, slots[CLUSTER..]);
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, 2 * CLUSTER, isContiguous: false));

            var items = await directory.EnumerateAsync(CancellationToken.None).ToListAsync();

            Assert.That(items.Select(item => (item.Entry.Name, item.FirstSlot, item.LastSlot)),
                Is.EqualTo(new[] { ("across.txt", 15L, 17L), ("after.txt", 18L, 20L) }));
        }

        [Test]
        public async Task FreeSlotsAreThoseNotInUseTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(10);
            var slots = ExFatSetBuilder.Join(Set("a.txt"), ExFatSetBuilder.Entry(0x41), ExFatSetBuilder.Entry(0x05), Set("b.txt"),
                ExFatSetBuilder.Entry(0x7F), ExFatSetBuilder.Entry(0x05), ExFatSetBuilder.Entry(0x40), ExFatSetBuilder.Entry(0x00));
            volume.Put(10, slots);
            var directory = volume.Core(await volume.CreateAsync()).OpenDirectory(ExFatTestVolume.Directory(10, CLUSTER, isContiguous: false));

            var two = await directory.FindFreeAsync(2, CancellationToken.None);
            var four = await directory.FindFreeAsync(4, CancellationToken.None);

            Assert.That((two.Start, two.Available, two.EndMarker), Is.EqualTo((3L, 2L, -1L)));
            Assert.That((four.Start, four.Available, four.EndMarker), Is.EqualTo((8L, 8L, 11L)));
        }

        #endregion

        #region Tools

        private static byte[] Set(string name)
        {
            return new ExFatSetBuilder { Name = name }.Build();
        }

        /// <summary>
        /// A set whose checksum does not match.
        /// </summary>
        private static byte[] Broken(string name)
        {
            var set = Set(name);
            set[40] ^= 1;
            return set;
        }

        #endregion
    }
}
