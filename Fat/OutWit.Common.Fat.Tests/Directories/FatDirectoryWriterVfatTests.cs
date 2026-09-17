using OutWit.Common.Fat.Allocation;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Files;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class FatDirectoryWriterVfatTests
    {
        #region Add Tests

        [Test]
        public async Task EntriesTakeSlotsInOrderTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var writer = Writer(core, names, out var root);

            var first = await AddAsync(writer, "first.txt");
            var second = await AddAsync(writer, "Second file with a long name.txt");
            var third = await AddAsync(writer, "THIRD");

            Assert.That((first.FirstSlot, first.LastSlot), Is.EqualTo((0L, 0L)));
            Assert.That((second.FirstSlot, second.LastSlot), Is.EqualTo((1L, 4L)));
            Assert.That((third.FirstSlot, third.LastSlot), Is.EqualTo((5L, 5L)));
            Assert.That(second.Entry.ShortName, Is.EqualTo("SECOND~1.TXT"));
            Assert.That(await NamesAsync(root), Is.EqualTo(new[] { "first.txt", "Second file with a long name.txt", "THIRD" }));
        }

        [Test]
        public async Task NameTakenInAnyCaseOrAsAnAliasIsRefusedTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var writer = Writer(core, names, out _);
            await AddAsync(writer, "Long File Name.txt");

            var byCase = Assert.ThrowsAsync<FatException>(async () => await AddAsync(writer, "LONG FILE NAME.TXT"));
            var byAlias = Assert.ThrowsAsync<FatException>(async () => await AddAsync(writer, "longfi~1.txt"));

            Assert.That(byCase!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
            Assert.That(byAlias!.Kind, Is.EqualTo(FatErrorKind.AlreadyExists));
        }

        [Test]
        public async Task DeletedSlotsAreReusedWhenTheyFitTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var writer = Writer(core, names, out var root);
            await AddAsync(writer, "a.txt");
            var removed = await AddAsync(writer, "Long name, two slots.txt");
            await AddAsync(writer, "b.txt");
            await writer.RemoveAsync(removed, CancellationToken.None);

            var tooLong = await AddAsync(writer, "A name that needs three slots at least.txt");
            var fits = await AddAsync(writer, "Short enough name.txt");

            Assert.That(tooLong.FirstSlot, Is.EqualTo(removed.LastSlot + 2));
            Assert.That((fits.FirstSlot, fits.LastSlot), Is.EqualTo((removed.FirstSlot, removed.LastSlot)));
            Assert.That(await NamesAsync(root), Is.EqualTo(new[] { "a.txt", "Short enough name.txt", "b.txt", "A name that needs three slots at least.txt" }));
        }

        [Test]
        public async Task EndMarkerIsKeptAfterNewEntriesTest()
        {
            var (probe, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var junk = Enumerable.Repeat((byte)'!', 16 * 32).ToArray();
            junk[0] = 0;
            await probe.WriteAsync(core.Info.RootDirectorySector!.Value, junk);
            var writer = Writer(core, names, out var root);

            await AddAsync(writer, "only.txt");

            Assert.That(await NamesAsync(root), Is.EqualTo(new[] { "only.txt" }));
        }

        #endregion

        #region Growth Tests

        [Test]
        public async Task FullFixedRootRefusesMoreTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat12, rootEntries: 16);
            var writer = Writer(core, names, out _);
            for (int i = 0; i < 14; i++)
                await AddAsync(writer, $"FILE{i:D2}.TXT");

            var tooLong = Assert.ThrowsAsync<FatException>(async () => await AddAsync(writer, "A long name.txt"));
            await AddAsync(writer, "FILE14.TXT");
            await AddAsync(writer, "FILE15.TXT");
            var full = Assert.ThrowsAsync<FatException>(async () => await AddAsync(writer, "FILE16.TXT"));

            Assert.That(tooLong!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(full!.Kind, Is.EqualTo(FatErrorKind.NoSpace));
            Assert.That(full.Message, Does.Contain("root directory is full"));
        }

        [Test]
        public async Task SubdirectoryGrowsByZeroedClustersTest()
        {
            var (probe, core, names) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var editor = new FatNamespaceEditor(core, names);
            var entry = await editor.CreateDirectoryAsync("/sub", CancellationToken.None);
            var junk = Enumerable.Repeat((byte)'!', 32 * 512).ToArray();
            await probe.WriteAsync(core.ClusterSector(entry.FirstCluster + 1), junk);

            var item = (await names.FindAsync(new[] { "sub" }, CancellationToken.None))!;
            var directory = names.Open(item);
            var writer = FatDirectoryWriter.Open(core, directory);
            for (int i = 0; i < 20; i++)
                await AddAsync(writer, $"FILE{i:D2}.TXT");

            var runs = await new ClusterChain(core.Table, entry.FirstCluster).ReadAllAsync(CancellationToken.None);
            Assert.That(runs, Is.EqualTo(new[] { new ClusterRun(0, entry.FirstCluster, 2) }));
            Assert.That(await NamesAsync(directory), Has.Count.EqualTo(20));
        }

        #endregion

        #region Update Tests

        [Test]
        public async Task UpdateReturnsTheEntryAsItNowReadsTest()
        {
            var (_, core, names) = await TestVolumes.CoreAsync(FatKind.Fat32);
            var writer = Writer(core, names, out var root);
            var item = await AddAsync(writer, "Some long name.bin");

            var updated = await writer.UpdateAsync(item, new FatFileState(0, 1234, 1234, false), false, CancellationToken.None);

            Assert.That(updated.Entry.Name, Is.EqualTo("Some long name.bin"));
            Assert.That(updated.Entry.Length, Is.EqualTo(1234));
            Assert.That(updated.Key, Is.EqualTo(item.Key));
            Assert.That((await root.FindAsync("some long name.bin", CancellationToken.None))!.Entry.Length, Is.EqualTo(1234));
        }

        [Test]
        public async Task ParentChangeNeedsADotDotEntryTest()
        {
            var (_, core, _) = await TestVolumes.CoreAsync(FatKind.Fat16);
            var fake = new DirectoryItem(new FatDirectoryEntry { Path = "/fake", Attributes = FatAttributes.Directory, FirstCluster = 5 }, 0, 0, 0);
            var parent = new DirectoryItem(new FatDirectoryEntry { Path = "/other", Attributes = FatAttributes.Directory, FirstCluster = 3 }, 0, 1, 1);

            var error = Assert.ThrowsAsync<FatException>(async () =>
                await FatDirectoryWriter.Open(core, core.OpenDirectory(fake)).SetParentAsync(parent, CancellationToken.None));

            Assert.That(error!.Kind, Is.EqualTo(FatErrorKind.Corrupt));
        }

        #endregion

        #region Tools

        private static FatDirectoryWriter Writer(FatVolumeCore core, FatNamespace names, out FatDirectory root)
        {
            root = names.Open(names.Root);
            return FatDirectoryWriter.Open(core, root);
        }

        private static ValueTask<DirectoryItem> AddAsync(FatDirectoryWriter writer, string name)
        {
            return writer.AddAsync(name, writer.NewTemplate(FatAttributes.Archive, 0, 0, false), null, CancellationToken.None);
        }

        private static async Task<List<string>> NamesAsync(FatDirectory directory)
        {
            var names = new List<string>();
            await foreach (var item in directory.EnumerateAsync(CancellationToken.None))
                names.Add(item.Entry.Name);
            return names;
        }

        #endregion
    }
}
