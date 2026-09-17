using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    /// <summary>
    /// The volume flags and the percentage in use of an exFAT boot sector, as changes set
    /// and clear them.
    /// </summary>
    [TestFixture]
    public class ExFatDirtyFlagTests
    {
        #region Constants

        private const int VOLUME_FLAGS = 106;

        private const int PERCENT_IN_USE = 112;

        private const byte DIRTY = 0x02;

        #endregion

        #region Flag Tests

        [Test]
        public async Task FirstChangeMarksTheVolumeDirtyTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            var clean = await BootAsync(disk);

            await using (var stream = await volume.OpenAsync("/a.bin", FileMode.CreateNew, FileAccess.Write))
            {
                var dirty = await BootAsync(disk);
                Assert.That((dirty[VOLUME_FLAGS] & DIRTY, dirty[PERCENT_IN_USE]), Is.EqualTo((DIRTY, 0xFF)));
                await stream.WriteAsync(new byte[1000]);
            }

            var afterClose = await BootAsync(disk);
            await volume.FlushAsync();
            var afterFlush = await BootAsync(disk);

            Assert.That((clean[VOLUME_FLAGS] & DIRTY, clean[PERCENT_IN_USE]), Is.EqualTo((0, 0)));
            Assert.That(afterClose[VOLUME_FLAGS] & DIRTY, Is.EqualTo(DIRTY), "a stream's flush is not the volume's");
            Assert.That((afterFlush[VOLUME_FLAGS] & DIRTY, afterFlush[PERCENT_IN_USE]), Is.EqualTo((0, 0xFF)));
        }

        [Test]
        public async Task CountedVolumeRecordsItsPercentageTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat, clusters: 200);
            await using var _ = volume;
            await volume.CountFreeClustersAsync();

            await volume.WriteAllBytesAsync("/a.bin", new byte[36 * 512]);
            await volume.FlushAsync();

            var boot = await BootAsync(disk);
            Assert.That(boot[PERCENT_IN_USE], Is.EqualTo((14 + 36) * 100 / 200));
            Assert.That(boot[VOLUME_FLAGS] & DIRTY, Is.Zero);
        }

        [Test]
        public async Task ReadingLeavesTheBootSectorAloneTest()
        {
            var disk = await BlankVolumeExFat.CreateAsync(300);
            var probe = new BlockDeviceProbe(disk);
            await using (var volume = await FatVolume.MountAsync(probe, TestVolumes.Options(null)))
            {
                await volume.EnumerateAsync().CountAsync();
                await volume.CountFreeClustersAsync();
                Assert.That(await volume.ExistsAsync("/nothing"), Is.False);
            }

            Assert.That(probe.Of(BlockDeviceOperation.Write), Is.Empty);
        }

        [Test]
        public async Task VolumeFoundDirtyStaysDirtyTest()
        {
            var disk = await BlankVolumeExFat.CreateAsync(300);
            var boot = await BootAsync(disk);
            boot[VOLUME_FLAGS] |= DIRTY;
            await disk.WriteAsync(0, boot);

            await using (var volume = await FatVolume.MountAsync(disk, TestVolumes.Options(null)))
                await volume.CreateDirectoryAsync("/after");

            Assert.That((await BootAsync(disk))[VOLUME_FLAGS] & DIRTY, Is.EqualTo(DIRTY));
            await using var reopened = await TestVolumes.RemountAsync(disk);
            Assert.That(await reopened.ExistsAsync("/after"), Is.True);
        }

        #endregion

        #region Tools

        private static async Task<byte[]> BootAsync(IBlockDevice disk)
        {
            var sector = new byte[disk.SectorSize];
            await disk.ReadAsync(0, sector);
            return sector;
        }

        #endregion
    }
}
