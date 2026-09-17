using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Exceptions;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatEntrySetParserTests
    {
        #region Constants

        private const uint DIRECTORY_CLUSTER = 7;

        #endregion

        #region Set Tests

        [Test]
        public void FileSetIsDescribedTest()
        {
            var builder = new ExFatSetBuilder
            {
                Name = "A name longer than fifteen units.bin",
                Attributes = FatAttributes.Archive | FatAttributes.ReadOnly,
                FirstCluster = 40,
                DataLength = 10000,
                ValidLength = 6000,
                IsContiguous = true,
                Created10Ms = 150,
                ModifiedOffset = 0
            };
            var directory = new DirectoryItem(new FatDirectoryEntry { Path = "/dir" }, 2, 3, 4);

            var result = Parse(ExFatSetBuilder.Join(ExFatSetBuilder.Entry(0x05), builder.Build(), ExFatSetBuilder.Entry(0)), directory);

            var item = result.Items.Single();
            Assert.That(result.Kinds, Is.EqualTo(new[]
            {
                DirectorySlotKind.Skipped, DirectorySlotKind.Skipped, DirectorySlotKind.Skipped, DirectorySlotKind.Skipped,
                DirectorySlotKind.Skipped, DirectorySlotKind.Entry, DirectorySlotKind.End
            }));
            Assert.That(item.Entry, Was.EqualTo(new FatDirectoryEntry
            {
                Path = "/dir/A name longer than fifteen units.bin",
                Name = "A name longer than fifteen units.bin",
                Attributes = FatAttributes.Archive | FatAttributes.ReadOnly,
                Length = 10000,
                FirstCluster = 40,
                Created = new DateTime(2024, 2, 29, 12, 34, 57, 500, DateTimeKind.Utc),
                Modified = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Unspecified),
                Accessed = new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc)
            }));
            Assert.That((item.DirectoryCluster, item.FirstSlot, item.LastSlot), Is.EqualTo((DIRECTORY_CLUSTER, 1L, 5L)));
            Assert.That((item.IsContiguous, item.DataLength, item.ValidLength), Is.EqualTo((true, 10000L, 6000L)));
            Assert.That(item.Parent, Is.SameAs(directory));
            Assert.That(result.Problems, Is.Empty);
        }

        [Test]
        public void DirectoryHasNoLengthOfItsOwnTest()
        {
            var builder = new ExFatSetBuilder { Name = "sub", Attributes = FatAttributes.Directory, FirstCluster = 9, DataLength = 8192 };

            var item = Parse(builder.Build()).Items.Single();

            Assert.That(item.Entry.IsDirectory, Is.True);
            Assert.That(item.Entry.Length, Is.Zero);
            Assert.That(item.DataLength, Is.EqualTo(8192));
            Assert.That(item.IsContiguous, Is.False);
        }

        [Test]
        public void ReservedAttributeBitsAreDroppedTest()
        {
            var builder = new ExFatSetBuilder { Attributes = (FatAttributes)0xFFFF };

            var item = Parse(builder.Build()).Items.Single();

            Assert.That(item.Entry.Attributes, Is.EqualTo(FatAttributes.ReadOnly | FatAttributes.Hidden | FatAttributes.System
                                                          | FatAttributes.Directory | FatAttributes.Archive));
        }

        [Test]
        public void BenignSecondaryIsPartOfTheSetTest()
        {
            var builder = new ExFatSetBuilder();
            builder.Extra.Add(ExFatSetBuilder.Entry(0xE0));
            builder.Extra.Add(ExFatSetBuilder.Entry(0xE1));

            var result = Parse(ExFatSetBuilder.Join(builder.Build(), builder.Build()));

            Assert.That(result.Items.Select(i => (i.FirstSlot, i.LastSlot)), Is.EqualTo(new[] { (0L, 4L), (5L, 9L) }));
            Assert.That(result.Problems, Is.Empty);
        }

        [Test]
        public void SetOfMoreThanEighteenSecondariesIsReadTest()
        {
            var builder = new ExFatSetBuilder { Name = new string('n', 255) };
            builder.Extra.Add(ExFatSetBuilder.Entry(0xE0));
            builder.Extra.Add(ExFatSetBuilder.Entry(0xE0));

            var result = Parse(builder.Build());

            Assert.That(result.Items.Single().LastSlot, Is.EqualTo(20));
            Assert.That(result.Problems, Is.Empty);
        }

        [Test]
        public void UnsafeCharactersBecomeUnderscoresTest()
        {
            var builder = new ExFatSetBuilder { Name = "a/b\\cd" };

            var item = Parse(builder.Build()).Items.Single();

            Assert.That(item.Entry.Name, Is.EqualTo("a_b_c_d"));
            Assert.That(item.Entry.Path, Is.EqualTo("/dir/a_b_c_d"));
        }

        #endregion

        #region Skip Tests

        [Test]
        public void EntriesThatAreNotFilesArePassedOverTest()
        {
            var benignPrimary = ExFatSetBuilder.Entry(0xA0, second: 2);
            var slots = ExFatSetBuilder.Join(
                ExFatSetBuilder.Entry(0x40), ExFatSetBuilder.Entry(0xC1), ExFatSetBuilder.Entry(0xE0),
                benignPrimary, ExFatSetBuilder.Entry(0xC0), ExFatSetBuilder.Entry(0xC1),
                ExFatSetBuilder.Entry(0x90), new ExFatSetBuilder().Build());

            var result = Parse(slots);

            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Kinds.Take(7), Is.All.EqualTo(DirectorySlotKind.Skipped));
            Assert.That(result.Problems, Is.Empty);
        }

        [Test]
        public void BenignSetCutShortIsDroppedQuietlyTest()
        {
            var slots = ExFatSetBuilder.Join(ExFatSetBuilder.Entry(0xA0, second: 3), ExFatSetBuilder.Entry(0xC0), new ExFatSetBuilder().Build());

            var result = Parse(slots);

            Assert.That(result.Items.Single().FirstSlot, Is.EqualTo(2));
            Assert.That(result.Problems, Is.Empty);
        }

        [Test]
        public void DeletedSetIsPassedOverTest()
        {
            var set = new ExFatSetBuilder { Name = "deleted.txt" }.Build();
            for (int offset = 0; offset < set.Length; offset += ExFatSetBuilder.ENTRY)
                set[offset] &= 0x7F;

            var result = Parse(ExFatSetBuilder.Join(set, new ExFatSetBuilder().Build()));

            Assert.That(result.Items.Select(i => i.Entry.Name), Is.EqualTo(new[] { "file.txt" }));
        }

        [Test]
        public void LabelAndCriticalEntriesAreReportedTest()
        {
            var parser = new ExFatEntrySetParser("/", 5);

            var label = parser.Parse(ExFatSetBuilder.Label("EXFAT C8"), out _, out string? text);
            var bitmap = parser.Parse(ExFatSetBuilder.Bitmap(2, 448, flags: 1), out _, out _);
            var first = parser.Critical;
            var upcase = parser.Parse(ExFatSetBuilder.Upcase(3, new byte[] { 1, 2, 3 }), out _, out _);

            Assert.That((label, text), Is.EqualTo((DirectorySlotKind.Label, "EXFAT C8")));
            Assert.That((bitmap, upcase), Is.EqualTo((DirectorySlotKind.Critical, DirectorySlotKind.Critical)));
            Assert.That(first, Is.EqualTo(new ExFatRootEntry(0x81, 1, 2, 448, 0, 1)));
            Assert.That(parser.Critical, Is.EqualTo(new ExFatRootEntry(0x82, 0, 3, 3, ExFatSetBuilder.UpcaseChecksum(new byte[] { 1, 2, 3 }), 2)));
        }

        [Test]
        public void OverlongLabelIsCutTest()
        {
            var entry = ExFatSetBuilder.Label("ELEVEN CHAR");
            entry[1] = 30;

            new ExFatEntrySetParser("/", 5).Parse(entry, out _, out string? label);

            Assert.That(label, Is.EqualTo("ELEVEN CHAR"));
        }

        [Test]
        public void EmptyLabelIsNoLabelTest()
        {
            var kind = new ExFatEntrySetParser("/", 5).Parse(ExFatSetBuilder.Label(""), out _, out string? label);

            Assert.That((kind, label), Is.EqualTo((DirectorySlotKind.Skipped, (string?)null)));
        }

        #endregion

        #region Corruption Tests

        [Test]
        public void WrongChecksumIsCorruptTest()
        {
            var set = new ExFatSetBuilder().Build();
            set[40] ^= 1;

            AssertCorrupt(set, "checksum");
        }

        [Test]
        public void SetCutShortIsCorruptTest()
        {
            var set = new ExFatSetBuilder().Build();

            AssertCorrupt(ExFatSetBuilder.Join(set[..64], ExFatSetBuilder.Entry(0x41)), "ends after 2 of its 3 entries");
            AssertCorrupt(ExFatSetBuilder.Join(set[..64], ExFatSetBuilder.Entry(0)), "ends after 2 of its 3 entries");
            AssertCorrupt(set[..64], "cut short by the end of the directory after 2 of its 3 entries");
        }

        [Test]
        public void EntryThatCutsASetShortIsReadTest()
        {
            var set = new ExFatSetBuilder().Build();
            var next = new ExFatSetBuilder { Name = "next.txt" }.Build();

            var result = Parse(ExFatSetBuilder.Join(set[..64], next));

            Assert.That(result.Problems.Single().Message, Does.Contain("ends after 2 of its 3 entries"));
            Assert.That(result.Items.Single().Entry.Name, Is.EqualTo("next.txt"));
            Assert.That(result.Items.Single().FirstSlot, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SecondaryCountTooSmallIsCorruptTest(int count)
        {
            var result = Parse(new ExFatSetBuilder { SecondaryCount = count }.Build());

            Assert.That(result.Problems.First().Message, Does.Contain($"counts {count} secondary entries"));
            Assert.That(result.Items, Is.Empty);
        }

        [Test]
        public void MissingStreamExtensionIsCorruptTest()
        {
            AssertCorrupt(new ExFatSetBuilder { StreamType = 0xE0 }.Build(), "no stream extension");
        }

        [TestCase(0)]
        [TestCase(16)]
        public void NameThatDoesNotFitIsCorruptTest(int length)
        {
            AssertCorrupt(new ExFatSetBuilder { Name = "short", NameLength = length }.Build(), $"a name of {length} characters");
        }

        [Test]
        public void MissingNameEntryIsCorruptTest()
        {
            var builder = new ExFatSetBuilder { Name = "sixteen units!!!", SecondaryCount = 3 };
            var set = builder.Build();
            set[3 * ExFatSetBuilder.ENTRY] = 0xE0;
            BitConverter.TryWriteBytes(set.AsSpan(2), ExFatSetBuilder.SetChecksum(set));

            AssertCorrupt(set, "entry 3 is of type 0xE0");
        }

        [Test]
        public void ExtraNameEntryIsCorruptTest()
        {
            var builder = new ExFatSetBuilder();
            builder.Extra.Add(ExFatSetBuilder.Entry(0xC1));

            AssertCorrupt(builder.Build(), "entry 3 is of type 0xC1");
        }

        [Test]
        public void UnknownCriticalSecondaryIsUnsupportedTest()
        {
            var builder = new ExFatSetBuilder();
            builder.Extra.Add(ExFatSetBuilder.Entry(0xC5));

            var problem = Parse(builder.Build()).Problems.Single();

            Assert.That(problem.Kind, Is.EqualTo(FatErrorKind.Unsupported));
            Assert.That(problem.Message, Does.Contain("0xC5"));
        }

        [TestCase(".")]
        [TestCase("..")]
        public void DotNamesAreCorruptTest(string name)
        {
            AssertCorrupt(new ExFatSetBuilder { Name = name }.Build(), $"is named '{name}'");
        }

        [Test]
        public void ValidLengthPastLengthIsCorruptTest()
        {
            AssertCorrupt(new ExFatSetBuilder { FirstCluster = 5, DataLength = 100, ValidLength = 101 }.Build(), "101 valid bytes of 100");
        }

        [Test]
        public void DataWithoutClusterIsCorruptTest()
        {
            AssertCorrupt(new ExFatSetBuilder { DataLength = 100 }.Build(), "100 bytes and no cluster");
        }

        [Test]
        public void ParserCarriesOnAfterACorruptSetTest()
        {
            var bad = new ExFatSetBuilder { Name = "bad.txt" }.Build();
            bad[40] ^= 1;

            var result = Parse(ExFatSetBuilder.Join(bad, new ExFatSetBuilder().Build()));

            Assert.That(result.Problems, Has.Count.EqualTo(1));
            Assert.That(result.Items.Single().Entry.Name, Is.EqualTo("file.txt"));
        }

        #endregion

        #region Tools

        private static ParseResult Parse(byte[] slots, DirectoryItem? directory = null)
        {
            var parser = new ExFatEntrySetParser("/dir", DIRECTORY_CLUSTER, directory);
            var result = new ParseResult();
            for (int offset = 0; offset < slots.Length; offset += ExFatSetBuilder.ENTRY)
            {
                result.Kinds.Add(parser.Parse(slots.AsSpan(offset, ExFatSetBuilder.ENTRY), out var item, out _));
                if (item != null)
                    result.Items.Add(item);
                Take(parser, result);
            }

            parser.Finish();
            Take(parser, result);
            return result;
        }

        private static void Take(ExFatEntrySetParser parser, ParseResult result)
        {
            while (parser.TakeProblem() is { } problem)
                result.Problems.Add(problem);
        }

        private static void AssertCorrupt(byte[] slots, string fragment)
        {
            var result = Parse(slots);

            Assert.That(result.Problems, Is.Not.Empty, fragment);
            Assert.That(result.Problems[0].Kind, Is.EqualTo(FatErrorKind.Corrupt));
            Assert.That(result.Problems[0].Message, Does.Contain(fragment).And.Contain("slot 0 of /dir"));
            Assert.That(result.Items, Is.Empty);
        }

        #endregion

        #region Nested Types

        private sealed class ParseResult
        {
            public List<DirectoryItem> Items { get; } = new();

            public List<DirectorySlotKind> Kinds { get; } = new();

            public List<FatException> Problems { get; } = new();
        }

        #endregion
    }
}
