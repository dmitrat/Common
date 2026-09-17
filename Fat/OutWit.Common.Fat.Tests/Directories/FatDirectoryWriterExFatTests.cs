using System.Buffers.Binary;
using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.Fat.Utils;
using OutWit.Common.Fat.Volumes;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatDirectoryWriterExFatTests
    {
        #region Constants

        private const int CLUSTER = 512;

        /// <summary>The first free cluster of a blank volume of 512-byte clusters.</summary>
        private const uint FIRST_FREE = 16;

        #endregion

        #region Set Tests

        [Test]
        public async Task AddedSetCarriesItsHashAndChecksumTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.ExFat);
            var writer = FatDirectoryWriter.Open(core, names.Open(names.Root));

            var item = await AddAsync(writer, "Some File Name.txt");

            var set = await writer.ReadTemplateAsync(item, CancellationToken.None);
            var upcase = await core.ExFat!.GetUpcaseTableAsync(CancellationToken.None);
            Assert.That(set, Has.Length.EqualTo(4 * ExFatEntry.SIZE));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(set.AsSpan(ExFatEntry.SIZE + ExFatEntry.NAME_HASH)), Is.EqualTo(upcase.HashOf("Some File Name.txt")));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(set.AsSpan(ExFatEntry.SET_CHECKSUM)), Is.EqualTo(ExFatSetBuilder.SetChecksum(set)));
            Assert.That((item.FirstSlot, item.LastSlot), Is.EqualTo((2L, 5L)), "after the bitmap and the up-case table");
            Assert.That((await names.Open(names.Root).FindAsync("SOME FILE NAME.TXT", CancellationToken.None))?.Key, Is.EqualTo(item.Key));
        }

        [Test]
        public async Task NameTakenInAnyCaseIsRefusedTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.ExFat);
            var writer = FatDirectoryWriter.Open(core, names.Open(names.Root));
            await AddAsync(writer, "Über straße.txt");

            var error = Assert.ThrowsAsync<FatException>(async () => await AddAsync(writer, "ÜBER STRAßE.TXT"));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
        }

        [Test]
        public async Task RemovedSetKeepsItsBytesTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.ExFat);
            var root = names.Open(names.Root);
            var writer = FatDirectoryWriter.Open(core, root);
            var item = await AddAsync(writer, "gone.txt");
            var before = await writer.ReadTemplateAsync(item, CancellationToken.None);

            var removed = await writer.RemoveAsync(item, CancellationToken.None);
            var after = await writer.ReadTemplateAsync(item, CancellationToken.None);

            Assert.That(removed, Is.EqualTo(before));
            Assert.That(after.Where((_, i) => i % ExFatEntry.SIZE == 0), Is.EqualTo(new byte[] { 0x05, 0x40, 0x41 }));
            Assert.That(after.Where((_, i) => i % ExFatEntry.SIZE != 0), Is.EqualTo(before.Where((_, i) => i % ExFatEntry.SIZE != 0)));
            Assert.That(await root.FindAsync("gone.txt", CancellationToken.None), Is.Null);

            await writer.RestoreAsync(item, removed, CancellationToken.None);
            Assert.That(await root.FindAsync("gone.txt", CancellationToken.None), Is.Not.Null);
        }

        [Test]
        public async Task UpdateRewritesTheStreamTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.ExFat);
            var root = names.Open(names.Root);
            var writer = FatDirectoryWriter.Open(core, root);
            var item = await AddAsync(writer, "data.bin");

            var updated = await writer.UpdateAsync(item, new FatFileState(40, 1000, 500, true), true, CancellationToken.None);

            var found = (await root.FindAsync("data.bin", CancellationToken.None))!;
            Assert.That((found.Entry.FirstCluster, found.Entry.Length, found.ValidLength, found.IsContiguous), Is.EqualTo((40u, 1000L, 500L, true)));
            Assert.That(found.Entry.Attributes, Is.EqualTo(FatAttributes.Archive));
            Assert.That(found.Entry.Modified, Is.EqualTo(TestVolumes.NOW));
            Assert.That(found.Entry.Modified!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That((updated.Key, updated.Parent), Is.EqualTo((item.Key, root.Item)));
        }

        #endregion

        #region Growth Tests

        [Test]
        public async Task NewDirectoryIsOneContiguousClusterTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;

            await volume.CreateDirectoryAsync("/sub");

            var item = (await volume.GetItemAsync("/sub", CancellationToken.None))!;
            Assert.That((item.Entry.FirstCluster, item.DataLength, item.ValidLength, item.IsContiguous), Is.EqualTo((FIRST_FREE, (long)CLUSTER, (long)CLUSTER, true)));
        }

        [Test]
        public async Task DirectoryGrowsInPlaceTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await volume.CreateDirectoryAsync("/sub");

            for (int i = 0; i < 40; i++)
                await (await volume.CreateAsync($"/sub/file {i:D2}.txt")).DisposeAsync();

            var item = (await volume.GetItemAsync("/sub", CancellationToken.None))!;
            Assert.That((item.DataLength, item.IsContiguous), Is.EqualTo((8L * CLUSTER, true)));
            Assert.That((await volume.Core.Table.GetAsync(FIRST_FREE, CancellationToken.None)).Kind, Is.EqualTo(FatTableEntryKind.Free));
            Assert.That(await volume.EnumerateAsync("/sub").CountAsync(), Is.EqualTo(40));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task DirectoryThatCannotGrowInPlaceGetsAChainTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;
            await volume.CreateDirectoryAsync("/sub");
            await volume.WriteAllBytesAsync("/blocker.bin", new byte[CLUSTER]);

            for (int i = 0; i < 20; i++)
                await (await volume.CreateAsync($"/sub/file {i:D2}.txt")).DisposeAsync();

            var item = (await volume.GetItemAsync("/sub", CancellationToken.None))!;
            var runs = await volume.GetClusterRunsAsync("/sub", CancellationToken.None);
            Assert.That((item.DataLength, item.IsContiguous), Is.EqualTo((4L * CLUSTER, false)));
            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE, 1), new ClusterRun(1, FIRST_FREE + 2, 3) }));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task RootGrowsByItsChainTest()
        {
            var (_, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            await using var _ = volume;

            for (int i = 0; i < 10; i++)
                await (await volume.CreateAsync($"/file {i}.txt")).DisposeAsync();

            var runs = await volume.GetClusterRunsAsync("/", CancellationToken.None);
            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, FIRST_FREE - 1, 2) }));
            Assert.That(await volume.EnumerateAsync().CountAsync(), Is.EqualTo(10));
            await VolumeConsistency.AssertAsync(volume);
        }

        [Test]
        public async Task DirectoryWithoutClustersGetsThemTest()
        {
            var (disk, volume) = await TestVolumes.BlankAsync(FatKind.ExFat);
            var root = volume.Core.OpenDirectory((await volume.GetItemAsync("/", CancellationToken.None))!);
            var writer = FatDirectoryWriter.Open(volume.Core, root);
            await writer.AddAsync("empty", writer.NewTemplate(FatAttributes.Directory, 0, 0, false), null, CancellationToken.None);

            await (await volume.CreateAsync("/empty/inside.txt")).DisposeAsync();
            await volume.DisposeAsync();

            await using var reopened = await TestVolumes.RemountAsync(disk);
            var item = (await reopened.GetItemAsync("/empty", CancellationToken.None))!;
            Assert.That((item.Entry.FirstCluster, item.DataLength, item.IsContiguous), Is.EqualTo((FIRST_FREE, (long)CLUSTER, true)));
            Assert.That(await reopened.ExistsAsync("/empty/inside.txt"), Is.True);
            await VolumeConsistency.AssertAsync(reopened);
        }

        #endregion

        #region Tools

        private static ValueTask<DirectoryItem> AddAsync(FatDirectoryWriter writer, string name)
        {
            return writer.AddAsync(name, writer.NewTemplate(FatAttributes.Archive, 0, 0, false), null, CancellationToken.None);
        }

        #endregion
    }
}
