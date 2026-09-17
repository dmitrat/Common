using OutWit.Common.Fat.Devices;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class ShortNameBuilderTests
    {
        #region Constants

        private static readonly IEnumerable<string> IMAGES = ReferenceImages.FatNames;

        private static readonly string CONTROL = "a" + (char)1 + "b";

        private static readonly string[] INVALID_NAMES =
        {
            "", ".", "..", "name.", "name ", "a/b", @"a\b", "a:b", "a*b", "a?b", "a\"b", "a<b", "a>b", "a|b", CONTROL,
            new string('n', 256)
        };

        private static readonly string[] VALID_NAMES = { new string('n', 255), " leading", "a.b.c", "+plus", "ünïcode", "日本" };

        #endregion

        #region Validation Tests

        [TestCaseSource(nameof(INVALID_NAMES))]
        public void InvalidNameIsRejectedTest(string name)
        {
            Assert.Throws<ArgumentException>(() => ShortNameBuilder.Validate(name));
        }

        [TestCaseSource(nameof(VALID_NAMES))]
        public void ValidNameIsAcceptedTest(string name)
        {
            Assert.DoesNotThrow(() => ShortNameBuilder.Validate(name));
        }

        #endregion

        #region Fit Tests

        [TestCase("README.TXT", "README  TXT", 0x00)]
        [TestCase("readme.txt", "README  TXT", 0x18)]
        [TestCase("readme.TXT", "README  TXT", 0x08)]
        [TestCase("README.txt", "README  TXT", 0x10)]
        [TestCase("NOEXT", "NOEXT      ", 0x00)]
        [TestCase("noext", "NOEXT      ", 0x08)]
        [TestCase("12345678.123", "12345678123", 0x00)]
        [TestCase("A-B_C~1.X", "A-B_C~1 X  ", 0x00)]
        [TestCase("{x}.$$$", "{X}     $$$", 0x08)]
        public void EightDotThreeNameFitsTest(string name, string stored, int caseFlags)
        {
            var shortName = new byte[11];

            bool fits = ShortNameBuilder.TryFit(name, shortName, out byte flags);

            Assert.That(fits, Is.True);
            Assert.That(System.Text.Encoding.ASCII.GetString(shortName), Is.EqualTo(stored));
            Assert.That(flags, Is.EqualTo(caseFlags));
            Assert.That(ShortName.Decode(shortName, flags), Is.EqualTo(name));
        }

        [TestCase("ReadMe.txt")]
        [TestCase("readme.Txt")]
        [TestCase("123456789.txt")]
        [TestCase("name.text")]
        [TestCase("a.b.c")]
        [TestCase(".hidden")]
        [TestCase("a b.txt")]
        [TestCase("a+b.txt")]
        [TestCase("ı.txt")]
        [TestCase("é.txt")]
        [TestCase("K.txt")]
        public void OtherNameNeedsALongNameTest(string name)
        {
            Assert.That(ShortNameBuilder.TryFit(name, new byte[11], out _), Is.False);
        }

        #endregion

        #region Alias Tests

        [TestCase("A name that is quite a bit longer than thirteen characters.txt", "ANAMET~1.TXT")]
        [TestCase("Mixed Case Name.txt", "MIXEDC~1.TXT")]
        [TestCase("a.b.c.d.txt", "ABCD~1.TXT")]
        [TestCase("cluster-plus-one.bin", "CLUSTE~1.BIN")]
        [TestCase("Empty Directory", "EMPTYD~1")]
        [TestCase("entry-000.dat", "ENTRY-~1.DAT")]
        [TestCase("file.tar.gz", "FILETA~1.GZ")]
        [TestCase("x.verylongext", "X~1.VER")]
        [TestCase(".bashrc", "BASHRC~1")]
        [TestCase("a b", "AB~1")]
        [TestCase("+++", "___~1")]
        [TestCase("   .txt", "_~1.TXT")]
        [TestCase("ReadMe.txt", "README.TXT")]
        [TestCase("Makefile", "MAKEFILE")]
        [TestCase("Привет мир.txt", "______~1.TXT")]
        [TestCase("日本語のファイル.txt", "______~1.TXT")]
        [TestCase("Ünïcödé.txt", "_N_C_D~1.TXT")]
        [TestCase("ı.txt", "_~1.TXT")]
        public void AliasIsMadeTest(string name, string alias)
        {
            Assert.That(Generate(name), Is.EqualTo(alias));
        }

        [TestCase("ReadMe.txt", "README~1.TXT", "README.TXT")]
        [TestCase("Long File Name 3.txt", "LONGFI~3.TXT", "LONGFI~1.TXT", "LONGFI~2.TXT")]
        [TestCase("Long File Name 9.txt", "LONGFI~2.TXT", "LONGFI~1.TXT", "LONGFI~3.TXT")]
        [TestCase("a b", "AB~2", "AB~1")]
        [TestCase("a b.c", "AB~1.C", "AB~1")]
        public void SmallestFreeTailIsTakenTest(string name, string alias, params string[] taken)
        {
            Assert.That(Generate(name, taken), Is.EqualTo(alias));
        }

        [TestCase(10, "ENTRY~10.DAT")]
        [TestCase(100, "ENTR~100.DAT")]
        [TestCase(1000, "ENT~1000.DAT")]
        public void LongerTailShortensTheBaseTest(int tail, string alias)
        {
            var taken = Enumerable.Range(1, tail - 1).Select(n => n < 10 ? $"ENTRY-~{n}.DAT" : n < 100 ? $"ENTRY~{n}.DAT" : $"ENTR~{n}.DAT");

            Assert.That(Generate("entry-000.dat", taken.ToArray()), Is.EqualTo(alias));
        }

        /// <remarks>
        /// Entries are replayed in the order they lie in their directories, which is the
        /// order mtools created them in, each against the names before it. Names outside
        /// ASCII are left out: mtools puts code-page letters in their aliases, and this
        /// library does not. A numeric tail is compared by its presence, not its number:
        /// mtools passes over free numbers and hands them out later, while this library
        /// takes the smallest free one.
        /// </remarks>
        [TestCaseSource(nameof(IMAGES))]
        public async Task AliasesMatchThoseMtoolsMadeTest(string name)
        {
            var image = ReferenceImages.Get(name);
            await using var disk = await ReferenceImages.OpenAsync(image);
            var names = await OpenAsync(disk, image);

            int compared = 0;
            var pending = new Stack<DirectoryItem>();
            pending.Push(names.Root);
            while (pending.Count > 0)
            {
                var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                await foreach (var item in names.Open(pending.Pop()).EnumerateAsync(CancellationToken.None))
                {
                    if (item.Entry.Name.All(char.IsAscii))
                    {
                        var shortName = new byte[11];
                        if (!ShortNameBuilder.TryFit(item.Entry.Name, shortName, out _))
                            ShortNameBuilder.Generate(item.Entry.Name, taken, shortName);
                        AssertSameShape(ShortName.Decode(shortName), item.Entry.ShortName, item.Entry.Path);
                        compared++;
                    }

                    taken.Add(item.Entry.Name);
                    taken.Add(item.Entry.ShortName);
                    if (item.Entry.IsDirectory)
                        pending.Push(item);
                }
            }

            Assert.That(compared, Is.GreaterThan(300));
        }

        #endregion

        #region Tools

        private static void AssertSameShape(string ours, string theirs, string path)
        {
            var (ourStem, ourTail, ourExtension) = Shape(ours);
            var (theirStem, theirTail, theirExtension) = Shape(theirs.ToUpperInvariant());
            Assert.That(ourExtension, Is.EqualTo(theirExtension), path);
            Assert.That(ourTail != null, Is.EqualTo(theirTail != null), path);
            if (ourTail == null)
                Assert.That(ourStem, Is.EqualTo(theirStem), path);
            else
                Assert.That(ourStem.StartsWith(theirStem, StringComparison.Ordinal) || theirStem.StartsWith(ourStem, StringComparison.Ordinal), Is.True,
                    $"{path}: {ours} and {theirs} come from different bases");
        }

        private static (string Stem, string? Tail, string Extension) Shape(string alias)
        {
            int dot = alias.IndexOf('.');
            string stem = dot < 0 ? alias : alias[..dot];
            string extension = dot < 0 ? string.Empty : alias[(dot + 1)..];
            int tilde = stem.LastIndexOf('~');
            if (tilde < 0 || tilde == stem.Length - 1 || !stem[(tilde + 1)..].All(char.IsAsciiDigit))
                return (stem, null, extension);
            return (stem[..tilde], stem[(tilde + 1)..], extension);
        }

        private static string Generate(string name, params string[] taken)
        {
            var shortName = new byte[11];
            ShortNameBuilder.Generate(name, new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase), shortName);
            return ShortName.Decode(shortName);
        }

        private static async Task<FatNamespace> OpenAsync(IBlockDevice disk, ReferenceImage image)
        {
            var layout = await FatDetector.DetectDiskAsync(disk);
            var location = layout.Volumes[0];
            IBlockDevice device = image.Partitions.Count > 0
                ? new BlockDevicePartition(disk, location.FirstSector, location.SectorCount)
                : disk;
            var core = new FatVolumeCore(device, location.Volume);
            var root = new FatDirectoryEntry { Attributes = FatAttributes.Directory, FirstCluster = location.Volume.RootCluster ?? 0 };
            return new FatNamespace(core, root);
        }

        #endregion
    }
}
