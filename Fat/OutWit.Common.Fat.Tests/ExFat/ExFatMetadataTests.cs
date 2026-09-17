using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatMetadataTests
    {
        #region Up-case Tests

        [Test]
        public async Task LookupsUseTheVolumesUpcaseTableTest()
        {
            var volume = new ExFatTestVolume().WithRoot(new byte[25], "ABCDEFGHIJKLMNOPQRSTUVWYYZ", new ExFatSetBuilder { Name = "x.bin" }.Build());
            var core = volume.Core(await volume.CreateAsync());
            var names = ExFatTestVolume.Names(core);
            var root = names.Open(names.Root);

            Assert.That((await root.FindAsync("Y.BIN", CancellationToken.None))?.Entry.Name, Is.EqualTo("x.bin"));
            Assert.That(await root.FindAsync("X.BIN", CancellationToken.None), Is.Null);
        }

        [Test]
        public async Task UpcaseTableIsReadOnceTest()
        {
            var volume = new ExFatTestVolume().WithRoot(new byte[25], ExFatTestVolume.LETTERS, new ExFatSetBuilder { Name = "x.txt" }.Build());
            var probe = await volume.CreateAsync();
            var names = ExFatTestVolume.Names(volume.Core(probe));
            await names.Open(names.Root).FindAsync("X.TXT", CancellationToken.None);
            probe.Clear();

            await names.Open(names.Root).FindAsync("X.TXT", CancellationToken.None);

            long upcaseSector = volume.Image.ClusterSector(ExFatTestVolume.UPCASE);
            Assert.That(probe.Requests.Where(r => r.Sector == upcaseSector), Is.Empty);
        }

        [Test]
        public async Task MissingUpcaseTableIsCorruptTest()
        {
            var volume = new ExFatTestVolume();
            volume.Image.Chain(ExFatTestVolume.ROOT);
            volume.Put(ExFatTestVolume.ROOT, ExFatSetBuilder.Join(ExFatSetBuilder.Bitmap(ExFatTestVolume.BITMAP, 25), new ExFatSetBuilder().Build()));
            var names = ExFatTestVolume.Names(volume.Core(await volume.CreateAsync()));

            var error = Assert.ThrowsAsync<FatException>(async () => await names.Open(names.Root).FindAsync("file.txt", CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("no up-case table"));
        }

        [Test]
        public async Task UpcaseTableThatDoesNotMatchItsChecksumIsCorruptTest()
        {
            var volume = new ExFatTestVolume().WithRoot(new byte[25], ExFatTestVolume.LETTERS, new ExFatSetBuilder().Build());
            volume.Put(ExFatTestVolume.UPCASE, ExFatSetBuilder.UpcaseTable("ABCDEFGHIJKLMNOPQRSTUVWXYz"));
            var names = ExFatTestVolume.Names(volume.Core(await volume.CreateAsync()));

            var error = Assert.ThrowsAsync<FatException>(async () => await names.Open(names.Root).FindAsync("file.txt", CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(error.Message, Does.Contain("up-case table sums to"));
        }

        [Test]
        public async Task UpcaseTableLongerThanItsChainIsCorruptTest()
        {
            var table = ExFatSetBuilder.UpcaseTable(new string('A', 600));
            var volume = new ExFatTestVolume();
            volume.Image.Chain(ExFatTestVolume.ROOT).Chain(ExFatTestVolume.UPCASE);
            volume.Put(ExFatTestVolume.UPCASE, table).Put(ExFatTestVolume.ROOT, ExFatSetBuilder.Join(
                ExFatSetBuilder.Bitmap(ExFatTestVolume.BITMAP, 25), ExFatSetBuilder.Upcase(ExFatTestVolume.UPCASE, table), new ExFatSetBuilder().Build()));
            var names = ExFatTestVolume.Names(volume.Core(await volume.CreateAsync()));

            var error = Assert.ThrowsAsync<FatException>(async () => await names.Open(names.Root).FindAsync("file.txt", CancellationToken.None));

            Assert.That(error!.Message, Does.Contain("ends after 1 clusters").And.Contain($"records {table.Length} bytes"));
        }

        #endregion

        #region Bitmap Tests

        [Test]
        public async Task FreeClustersIgnoreBitsPastTheLastClusterTest()
        {
            var bitmap = new byte[26];
            bitmap[0] = 0x1F;
            bitmap[10] = 0x81;
            bitmap[25] = 0xFA;
            var volume = new ExFatTestVolume(clusters: 203).WithRoot(bitmap);
            var core = volume.Core(await volume.CreateAsync());

            Assert.That(await core.CountFreeClustersAsync(CancellationToken.None), Is.EqualTo(203 - 5 - 2 - 1));
        }

        [Test]
        public async Task BitmapIsFollowedThroughItsChainTest()
        {
            var bitmap = new byte[625];
            Array.Fill(bitmap, (byte)0xFF, 0, 512);
            bitmap[600] = 0x01;
            var volume = new ExFatTestVolume(clusters: 5000, fatSectors: 40).WithRoot(bitmap);
            volume.Image.Chain(ExFatTestVolume.BITMAP, 7);
            volume.Clusters.RemoveAll(put => put.Cluster == ExFatTestVolume.BITMAP);
            volume.Put(ExFatTestVolume.BITMAP, bitmap[..512]).Put(7, bitmap[512..]);
            var core = volume.Core(await volume.CreateAsync());

            Assert.That(await core.CountFreeClustersAsync(CancellationToken.None), Is.EqualTo(5000 - 4096 - 1));
        }

        [Test]
        public async Task BitmapOfTheActiveTableCountsTest()
        {
            var volume = new ExFatTestVolume(clusters: 16, fatCount: 2, activeFat: 1).WithRoot(new byte[] { 0xFF, 0xFF },
                ExFatTestVolume.LETTERS, ExFatSetBuilder.Bitmap(6, 2, flags: 1));
            volume.Image.Chain(6);
            volume.Put(6, new byte[] { 0x03, 0x00 });
            var core = volume.Core(await volume.CreateAsync(copy: 1));

            Assert.That(await core.CountFreeClustersAsync(CancellationToken.None), Is.EqualTo(14));
            Assert.That((await core.ExFat!.GetBitmapAsync(CancellationToken.None)).FirstCluster, Is.EqualTo(6));
        }

        [Test]
        public async Task MissingBitmapIsCorruptTest()
        {
            var table = ExFatSetBuilder.UpcaseTable(ExFatTestVolume.LETTERS);
            var volume = new ExFatTestVolume();
            volume.Image.Chain(ExFatTestVolume.ROOT).Chain(ExFatTestVolume.UPCASE);
            volume.Put(ExFatTestVolume.UPCASE, table).Put(ExFatTestVolume.ROOT, ExFatSetBuilder.Upcase(ExFatTestVolume.UPCASE, table));
            var core = volume.Core(await volume.CreateAsync());

            var error = Assert.ThrowsAsync<FatException>(async () => await core.CountFreeClustersAsync(CancellationToken.None));

            Assert.That(error!.Message, Does.Contain("no allocation bitmap for table 0"));
        }

        [Test]
        public async Task BitmapTooShortForTheVolumeIsCorruptTest()
        {
            var volume = new ExFatTestVolume(clusters: 201).WithRoot(new byte[25]);
            var core = volume.Core(await volume.CreateAsync());

            var error = Assert.ThrowsAsync<FatException>(async () => await core.CountFreeClustersAsync(CancellationToken.None));

            Assert.That(error!.Message, Does.Contain("records 25 bytes; 201 clusters need 26"));
        }

        #endregion
    }
}
