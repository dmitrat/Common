using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Allocation
{
    [TestFixture]
    public class FatTableTests
    {
        #region Decoding Tests

        [Test]
        public async Task Fat12PacksEvenAndOddEntriesTest()
        {
            var image = new FatTableImage(FatKind.Fat12, 1000, 3).Set(2, 0xABC).Set(3, 0x123).Set(4, 0xFF7).Set(5, 0x005);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That(await Values(table, 2, 3, 4, 5), Is.EqualTo(new uint[] { 0xABC, 0x123, 0xFF7, 0x005 }));
        }

        [TestCase(341u)]
        [TestCase(682u)]
        public async Task Fat12EntryAcrossSectorBoundaryTest(uint cluster)
        {
            var image = new FatTableImage(FatKind.Fat12, 1000, 3).Set(cluster, 0x5A5).Set(cluster - 1, 0x3C3).Set(cluster + 1, 0x7E7);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That(await Values(table, cluster - 1, cluster, cluster + 1), Is.EqualTo(new uint[] { 0x3C3, 0x5A5, 0x7E7 }));
        }

        [Test]
        public async Task Fat16EntriesDecodeTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 5000, 20).Set(2, 0x1234).Set(4999, 0xFFF8).Set(255, 256);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That(await Values(table, 2, 4999, 255), Is.EqualTo(new uint[] { 0x1234, 0xFFF8, 256 }));
        }

        [Test]
        public async Task Fat32IgnoresReservedHighBitsTest()
        {
            var image = new FatTableImage(FatKind.Fat32, 70000, 560).Set(2, 0xF0000005).Set(3, 0xFFFFFFFF).Set(4, 0x10000000);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            var link = await table.GetAsync(2, CancellationToken.None);
            var end = await table.GetAsync(3, CancellationToken.None);
            var free = await table.GetAsync(4, CancellationToken.None);

            Assert.That((link.Kind, link.Next), Is.EqualTo((FatTableEntryKind.Link, 5u)));
            Assert.That(end.Kind, Is.EqualTo(FatTableEntryKind.End));
            Assert.That(free.Kind, Is.EqualTo(FatTableEntryKind.Free));
        }

        [Test]
        public async Task ExFatCountsEveryBitTest()
        {
            var image = new FatTableImage(FatKind.ExFat, 70000, 560).Set(2, 0x10000005).Set(3, 0x0FFFFFFF).Set(4, 0xFFFFFFFF).Set(5, 700);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That(await Values(table, 2, 3, 4, 5), Is.EqualTo(new uint[] { 0x10000005, 0x0FFFFFFF, 0xFFFFFFFF, 700 }));
            Assert.That((await table.GetAsync(2, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Invalid));
            Assert.That((await table.GetAsync(3, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Invalid));
            Assert.That(table.EndOfChainMark, Is.EqualTo(0xFFFFFFFF));
        }

        [Test]
        public async Task ExFatKeepsTheWholeValueWrittenTest()
        {
            var image = new FatTableImage(FatKind.ExFat, 1000, 8);
            var device = await image.CreateDeviceAsync();
            var table = image.CreateTable(device);

            await table.SetAsync(10, 0xFFFFFFFF, CancellationToken.None);
            await table.SetAsync(11, 12, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            var sector = new byte[FatTableImage.SECTOR_SIZE];
            await device.ReadAsync(FatTableImage.TABLE_OFFSET, sector);
            Assert.That(BitConverter.ToUInt32(sector, 40), Is.EqualTo(0xFFFFFFFF));
            Assert.That(BitConverter.ToUInt32(sector, 44), Is.EqualTo(12));
        }

        #endregion

        #region Classification Tests

        [TestCase(FatKind.Fat12, 0u, "Free")]
        [TestCase(FatKind.Fat12, 1u, "Invalid")]
        [TestCase(FatKind.Fat12, 2u, "Link")]
        [TestCase(FatKind.Fat12, 1001u, "Link")]
        [TestCase(FatKind.Fat12, 1002u, "Invalid")]
        [TestCase(FatKind.Fat12, 0xFF6u, "Invalid")]
        [TestCase(FatKind.Fat12, 0xFF7u, "Bad")]
        [TestCase(FatKind.Fat12, 0xFF8u, "End")]
        [TestCase(FatKind.Fat12, 0xFFFu, "End")]
        [TestCase(FatKind.Fat16, 0xFFF6u, "Invalid")]
        [TestCase(FatKind.Fat16, 0xFFF7u, "Bad")]
        [TestCase(FatKind.Fat16, 0xFFF8u, "End")]
        [TestCase(FatKind.Fat32, 0x0FFFFFF7u, "Bad")]
        [TestCase(FatKind.Fat32, 0x0FFFFFF8u, "End")]
        [TestCase(FatKind.Fat32, 0x0FFFFFF0u, "Invalid")]
        [TestCase(FatKind.ExFat, 0u, "Free")]
        [TestCase(FatKind.ExFat, 1001u, "Link")]
        [TestCase(FatKind.ExFat, 0xFFFFFFF6u, "Invalid")]
        [TestCase(FatKind.ExFat, 0xFFFFFFF7u, "Bad")]
        [TestCase(FatKind.ExFat, 0xFFFFFFF8u, "End")]
        [TestCase(FatKind.ExFat, 0xFFFFFFFFu, "End")]
        [TestCase(FatKind.ExFat, 0x0FFFFFF8u, "Invalid")]
        public async Task EntryValueIsClassifiedTest(FatKind kind, uint value, string expected)
        {
            var image = new FatTableImage(kind, 1000, 8).Set(10, value);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That((await table.GetAsync(10, CancellationToken.None)).Kind.ToString(), Is.EqualTo(expected));
        }

        [TestCase(0u)]
        [TestCase(1u)]
        [TestCase(1002u)]
        public async Task ClusterOutsideVolumeIsRejectedTest(uint cluster)
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8);
            var table = image.CreateTable(await image.CreateDeviceAsync());

            Assert.That(table.LastCluster, Is.EqualTo(1001));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await table.GetAsync(cluster, CancellationToken.None));
        }

        #endregion

        #region Copy Tests

        [Test]
        public async Task UnmirroredTableReadsItsActiveCopyTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8, fatCount: 2, isMirrored: false, activeFat: 1).Set(7, 0x0042);
            var table = image.CreateTable(await image.CreateDeviceAsync(copy: 1));

            Assert.That(table.FirstSector, Is.EqualTo(9));
            Assert.That((await table.GetAsync(7, CancellationToken.None)).Next, Is.EqualTo(0x42));
        }

        [Test]
        public async Task MirroredTableReadsTheFirstCopyTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8, fatCount: 2, isMirrored: true, activeFat: 0).Set(7, 0x0042);
            var table = image.CreateTable(await image.CreateDeviceAsync(copy: 0));

            Assert.That(table.FirstSector, Is.EqualTo(1));
            Assert.That((await table.GetAsync(7, CancellationToken.None)).Next, Is.EqualTo(0x42));
        }

        #endregion

        #region Cost Tests

        [Test]
        public async Task EntriesOfOneSectorCostOneRequestTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 5000, 20);
            var probe = new BlockDeviceProbe(await image.CreateDeviceAsync());
            var table = image.CreateTable(probe);

            for (uint cluster = 2; cluster < 256; cluster++)
                await table.GetAsync(cluster, CancellationToken.None);

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Read, 1, 1) }));
        }

        [Test]
        public async Task LeastRecentlyUsedSectorLeavesTheWindowTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 5000, 20);
            var probe = new BlockDeviceProbe(await image.CreateDeviceAsync());
            var table = image.CreateTable(probe);

            for (uint sector = 0; sector < 9; sector++)
                await table.GetAsync(sector * 256 + 2, CancellationToken.None);
            probe.Clear();

            await table.GetAsync(8 * 256 + 2, CancellationToken.None);
            await table.GetAsync(2, CancellationToken.None);

            Assert.That(probe.Requests, Is.EqualTo(new[] { new BlockDeviceRequest(BlockDeviceOperation.Read, 1, 1) }));
        }

        #endregion

        #region Write Tests

        [TestCase(FatKind.Fat12)]
        [TestCase(FatKind.Fat16)]
        [TestCase(FatKind.Fat32)]
        public async Task WrittenEntriesReadBackAfterFlushTest(FatKind kind)
        {
            var image = new FatTableImage(kind, 3000, 24);
            var device = await image.CreateDeviceAsync();
            var table = image.CreateTable(device);
            uint[] clusters = { 2, 3, 340, 341, 342, 682, 683, 2999, 3001 };

            foreach (uint cluster in clusters)
                await table.SetAsync(cluster, cluster + 7, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            var reread = image.CreateTable(device);
            Assert.That(await Values(reread, clusters), Is.EqualTo(clusters.Select(c => c + 7).ToArray()));
        }

        [TestCase(340u)]
        [TestCase(341u)]
        [TestCase(342u)]
        public async Task Fat12WriteKeepsNeighbouringNibblesTest(uint cluster)
        {
            var image = new FatTableImage(FatKind.Fat12, 1000, 3).Set(cluster - 1, 0xABC).Set(cluster, 0x123).Set(cluster + 1, 0xDEF);
            var device = await image.CreateDeviceAsync();
            var table = image.CreateTable(device);

            await table.SetAsync(cluster, 0x456, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            Assert.That(await Values(image.CreateTable(device), cluster - 1, cluster, cluster + 1), Is.EqualTo(new uint[] { 0xABC, 0x456, 0xDEF }));
        }

        [Test]
        public async Task Fat32WriteKeepsReservedHighBitsTest()
        {
            var image = new FatTableImage(FatKind.Fat32, 70000, 560).Set(2, 0xF0000005).Set(3, 0x10000000);
            var device = await image.CreateDeviceAsync();
            var table = image.CreateTable(device);

            await table.SetAsync(2, 9, CancellationToken.None);
            await table.SetAsync(3, table.EndOfChainMark, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            var sector = new byte[FatTableImage.SECTOR_SIZE];
            await device.ReadAsync(FatTableImage.TABLE_OFFSET, sector);
            Assert.That(BitConverter.ToUInt32(sector, 8), Is.EqualTo(0xF0000009));
            Assert.That(BitConverter.ToUInt32(sector, 12), Is.EqualTo(0x1FFFFFFF));
        }

        [Test]
        public async Task ChangesWaitForFlushTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8);
            var probe = new BlockDeviceProbe(await image.CreateDeviceAsync());
            var table = image.CreateTable(probe);

            await table.SetAsync(7, 0x42, CancellationToken.None);
            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
            Assert.That(table.IsDirty, Is.True);

            await table.FlushAsync(CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);
            Assert.That(probe.Of(BlockDeviceOperation.Write).Count(), Is.EqualTo(1));
            Assert.That(table.IsDirty, Is.False);
        }

        [Test]
        public async Task MirroredTableWritesEveryCopyTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8, fatCount: 3);
            var device = await image.CreateDeviceAsync();
            var probe = new BlockDeviceProbe(device);
            var table = image.CreateTable(probe);

            await table.SetAsync(7, 0x42, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            Assert.That(probe.Of(BlockDeviceOperation.Write).Select(request => request.Sector), Is.EqualTo(new long[] { 1, 9, 17 }));
            var copy = new byte[FatTableImage.SECTOR_SIZE];
            await device.ReadAsync(17, copy);
            Assert.That(BitConverter.ToUInt16(copy, 14), Is.EqualTo(0x42));
        }

        [Test]
        public async Task UnmirroredTableWritesOnlyItsActiveCopyTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8, fatCount: 2, isMirrored: false, activeFat: 1);
            var probe = new BlockDeviceProbe(await image.CreateDeviceAsync(copy: 1));
            var table = image.CreateTable(probe);

            await table.SetAsync(7, 0x42, CancellationToken.None);
            await table.FlushAsync(CancellationToken.None);

            Assert.That(probe.Of(BlockDeviceOperation.Write).Select(request => request.Sector), Is.EqualTo(new long[] { 9 }));
        }

        [Test]
        public async Task ChangedSectorIsWrittenWhenItLeavesTheWindowTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 5000, 20);
            var device = await image.CreateDeviceAsync();
            var probe = new BlockDeviceProbe(device);
            var table = image.CreateTable(probe);

            await table.SetAsync(2, 0x1234, CancellationToken.None);
            for (uint sector = 1; sector < 9; sector++)
                await table.GetAsync(sector * 256 + 2, CancellationToken.None);

            Assert.That(probe.Of(BlockDeviceOperation.Write).Select(request => request.Sector), Is.EqualTo(new long[] { 1 }));
            Assert.That(await Values(image.CreateTable(device), 2), Is.EqualTo(new uint[] { 0x1234 }));
        }

        [Test]
        public async Task FailedWriteKeepsTheChangeTest()
        {
            var image = new FatTableImage(FatKind.Fat16, 1000, 8);
            var device = await image.CreateDeviceAsync();
            var probe = new BlockDeviceProbe(device);
            var table = image.CreateTable(probe);
            await table.SetAsync(7, 0x42, CancellationToken.None);

            probe.FailingWrites = 1;
            Assert.ThrowsAsync<IOException>(async () => await table.FlushAsync(CancellationToken.None));
            Assert.That(table.IsDirty, Is.True);

            await table.FlushAsync(CancellationToken.None);
            Assert.That(await Values(image.CreateTable(device), 7), Is.EqualTo(new uint[] { 0x42 }));
        }

        #endregion

        #region Tools

        private static async Task<uint[]> Values(FatTable table, params uint[] clusters)
        {
            var values = new uint[clusters.Length];
            for (int i = 0; i < clusters.Length; i++)
                values[i] = (await table.GetAsync(clusters[i], CancellationToken.None)).Value;
            return values;
        }

        #endregion
    }
}
