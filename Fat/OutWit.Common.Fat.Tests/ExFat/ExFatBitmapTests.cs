using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatBitmapTests
    {
        #region Bit Tests

        [Test]
        public async Task ChangesReachTheDeviceOnFlushTest()
        {
            var (probe, bitmap, sector) = await CreateAsync(300);

            await bitmap.SetAsync(50, true, CancellationToken.None);
            bool before = await StoredBitAsync(probe, sector, 50);
            await bitmap.FlushAsync(CancellationToken.None);

            Assert.That(before, Is.False);
            Assert.That(await StoredBitAsync(probe, sector, 50), Is.True);
            Assert.That(await bitmap.IsUsedAsync(50, CancellationToken.None), Is.True);
            Assert.That(bitmap.IsDirty, Is.False);
        }

        [Test]
        public async Task SettingABitToWhatItIsChangesNothingTest()
        {
            var (probe, bitmap, _) = await CreateAsync(300);
            probe.Clear();

            await bitmap.SetAsync(2, true, CancellationToken.None);
            await bitmap.SetAsync(200, false, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);

            Assert.That(bitmap.IsDirty, Is.False);
            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        [TestCase(1u)]
        [TestCase(302u)]
        public async Task ClusterOffTheVolumeIsRefusedTest(uint cluster)
        {
            var (_, bitmap, _) = await CreateAsync(300);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await bitmap.SetAsync(cluster, true, CancellationToken.None));
        }

        [Test]
        public async Task ChangedSectorIsWrittenWhenItMakesRoomTest()
        {
            var (probe, bitmap, sector) = await CreateAsync(50000);

            await bitmap.SetAsync(20000, true, CancellationToken.None);
            for (int sector5To12 = 5; sector5To12 <= 12; sector5To12++)
                await bitmap.IsUsedAsync((uint)(2 + sector5To12 * 4096), CancellationToken.None);

            Assert.That(await StoredBitAsync(probe, sector, 20000), Is.True);
        }

        #endregion

        #region Search Tests

        [Test]
        public async Task SearchStartsWhereItIsToldAndComesRoundTest()
        {
            var (_, bitmap, _) = await CreateAsync(300);
            for (uint cluster = 16; cluster <= 301; cluster++)
                await bitmap.SetAsync(cluster, cluster != 20, CancellationToken.None);

            var found = await bitmap.FindFreeAsync(100, CancellationToken.None);
            await bitmap.SetAsync(20, true, CancellationToken.None);
            var none = await bitmap.FindFreeAsync(100, CancellationToken.None);

            Assert.That(found, Is.EqualTo(20));
            Assert.That(none, Is.Null);
        }

        [Test]
        public async Task SearchPassesOverFullSectorsTest()
        {
            var (probe, bitmap, _) = await CreateAsync(50000);
            for (uint cluster = 2; cluster < 30000; cluster++)
                await bitmap.SetAsync(cluster, true, CancellationToken.None);
            await bitmap.FlushAsync(CancellationToken.None);

            Assert.That(await bitmap.FindFreeAsync(2, CancellationToken.None), Is.EqualTo(30000));
            Assert.That(await bitmap.FindFreeAsync(49990, CancellationToken.None), Is.EqualTo(49990));
            Assert.That(await bitmap.FindFreeAsync(50001, CancellationToken.None), Is.EqualTo(50001));
        }

        [Test]
        public async Task CountTakesHeldChangesTest()
        {
            var (_, bitmap, _) = await CreateAsync(300);
            long before = await bitmap.CountFreeAsync(CancellationToken.None);

            await bitmap.SetAsync(100, true, CancellationToken.None);
            await bitmap.SetAsync(301, true, CancellationToken.None);
            await bitmap.SetAsync(3, false, CancellationToken.None);

            Assert.That(await bitmap.CountFreeAsync(CancellationToken.None), Is.EqualTo(before - 1));
        }

        #endregion

        #region Tools

        /// <summary>
        /// A bitmap of a blank volume, and the device sector its data starts at.
        /// </summary>
        private static async Task<(BlockDeviceProbe Probe, ExFatBitmap Bitmap, long Sector)> CreateAsync(long clusters)
        {
            var (probe, core, _) = await TestVolumes.CoreAsync(FatKind.ExFat, clusters);
            var entry = await core.ExFat!.GetBitmapAsync(CancellationToken.None);
            return (probe, new ExFatBitmap(core, entry), core.ClusterSector(entry.FirstCluster));
        }

        private static async Task<bool> StoredBitAsync(IBlockDevice device, long firstSector, uint cluster)
        {
            long bit = cluster - 2;
            var sector = new byte[device.SectorSize];
            await device.ReadAsync(firstSector + bit / 8 / device.SectorSize, sector);
            return (sector[bit / 8 % device.SectorSize] & (1 << (int)(bit % 8))) != 0;
        }

        #endregion
    }
}
