using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class DirectoryParserTests
    {
        #region Short Entry Tests

        [Test]
        public void ShortEntryIsParsedTest()
        {
            var slot = DirectorySlotBuilder.Short("README  TXT", FatAttributes.Archive | FatAttributes.ReadOnly, 0x12345678, 1234);
            slot[13] = 150;
            slot[14] = 0x00; slot[15] = 0x50;
            slot[16] = 0x21; slot[17] = 0x2A;
            slot[18] = 0x5D; slot[19] = 0x58;

            var entries = Parse("/docs", hasHighCluster: true, slot);

            Assert.That(entries.Single(), Was.EqualTo(new FatDirectoryEntry
            {
                Path = "/docs/README.TXT",
                Name = "README.TXT",
                ShortName = "README.TXT",
                Attributes = FatAttributes.Archive | FatAttributes.ReadOnly,
                Length = 1234,
                FirstCluster = 0x12345678,
                Created = new DateTime(2001, 1, 1, 10, 0, 1, 500),
                Modified = new DateTime(2024, 2, 29, 12, 34, 56),
                Accessed = new DateTime(2024, 2, 29)
            }));
        }

        [Test]
        public void HighClusterCountsOnlyOnFat32Test()
        {
            var slot = DirectorySlotBuilder.Short("DATA    BIN", firstCluster: 0x00070009);

            Assert.That(Parse("/", hasHighCluster: false, slot).Single().FirstCluster, Is.EqualTo(9));
            Assert.That(Parse("/", hasHighCluster: true, slot).Single().FirstCluster, Is.EqualTo(0x00070009));
        }

        [Test]
        public void DirectoryHasNoLengthTest()
        {
            var slot = DirectorySlotBuilder.Short("SUB        ", FatAttributes.Directory, 5, size: 999);

            var entry = Parse("/", false, slot).Single();

            Assert.That(entry.IsDirectory, Is.True);
            Assert.That(entry.Length, Is.Zero);
            Assert.That(entry.Path, Is.EqualTo("/SUB"));
        }

        [Test]
        public void CaseFlagsShapeTheNameWithoutLongNameTest()
        {
            var entry = Parse("/", false, DirectorySlotBuilder.Short("LOWER   TXT", caseFlags: 0x18)).Single();

            Assert.That(entry.Name, Is.EqualTo("lower.txt"));
            Assert.That(entry.ShortName, Is.EqualTo("LOWER.TXT"));
            Assert.That(entry.Path, Is.EqualTo("/lower.txt"));
        }

        [Test]
        public void DotDeletedBlankAndDoubleKindSlotsAreSkippedTest()
        {
            var deleted = DirectorySlotBuilder.Short("GONE    TXT");
            deleted[0] = 0xE5;

            var entries = Parse("/sub", false,
                DirectorySlotBuilder.Short(".          ", FatAttributes.Directory, 5),
                DirectorySlotBuilder.Short("..         ", FatAttributes.Directory, 0),
                deleted,
                DirectorySlotBuilder.Short("           "),
                DirectorySlotBuilder.Short("ODD        ", FatAttributes.Directory | FatAttributes.VolumeLabel),
                DirectorySlotBuilder.Short("KEPT    TXT"));

            Assert.That(entries.Select(e => e.Name), Is.EqualTo(new[] { "KEPT.TXT" }));
        }

        [Test]
        public void EndMarkerStopsTheDirectoryTest()
        {
            var parser = new DirectoryParser("/", false);

            var kind = parser.Parse(new byte[32], out var entry, out var label);

            Assert.That(kind, Is.EqualTo(DirectorySlotKind.End));
            Assert.That(entry, Is.Null);
            Assert.That(label, Is.Null);
        }

        [Test]
        public void LabelIsReportedApartTest()
        {
            var parser = new DirectoryParser("/", false);

            var kind = parser.Parse(DirectorySlotBuilder.Short("FAT32 4K   ", FatAttributes.VolumeLabel), out var entry, out var label);

            Assert.That(kind, Is.EqualTo(DirectorySlotKind.Label));
            Assert.That(label, Is.EqualTo("FAT32 4K"));
            Assert.That(entry, Is.Null);
        }

        #endregion

        #region Long Name Tests

        [TestCase("Long File Name 1.txt")]
        [TestCase("A name that is quite a bit longer than thirteen characters.txt")]
        [TestCase("Thirteen.txt!")]
        [TestCase("Twenty-six characters, ok!")]
        [TestCase("emoji \U0001F600.txt")]
        [TestCase("Привет мир.txt")]
        public void LongNameIsAssembledTest(string longName)
        {
            var shortSlot = DirectorySlotBuilder.Short("LONGFI~1TXT");

            var entry = Parse("/", false, WithLongName(longName, shortSlot)).Single();

            Assert.That(entry.Name, Is.EqualTo(longName));
            Assert.That(entry.ShortName, Is.EqualTo("LONGFI~1.TXT"));
            Assert.That(entry.Path, Is.EqualTo("/" + longName));
        }

        [Test]
        public void LongNameOfMaximumLengthIsAssembledTest()
        {
            string longName = new string('n', 251) + ".txt";

            var entry = Parse("/", false, WithLongName(longName, DirectorySlotBuilder.Short("NNNNNN~1TXT"))).Single();

            Assert.That(entry.Name, Is.EqualTo(longName));
        }

        [Test]
        public void WrongChecksumFallsBackToShortNameTest()
        {
            var slots = WithLongName("Mixed Case Name.txt", DirectorySlotBuilder.Short("MIXEDC~1TXT"));
            slots[^1] = DirectorySlotBuilder.Short("MIXEDC~2TXT");

            Assert.That(Parse("/", false, slots).Single().Name, Is.EqualTo("MIXEDC~2.TXT"));
        }

        [Test]
        public void MissingSlotFallsBackToShortNameTest()
        {
            var slots = WithLongName("A name that is quite a bit longer than thirteen characters.txt", DirectorySlotBuilder.Short("ANAMET~1TXT"));
            slots.RemoveAt(2);

            Assert.That(Parse("/", false, slots).Single().Name, Is.EqualTo("ANAMET~1.TXT"));
        }

        [Test]
        public void DeletedSlotInBetweenBreaksTheNameTest()
        {
            var slots = WithLongName("Long File Name 1.txt", DirectorySlotBuilder.Short("LONGFI~1TXT"));
            var deleted = DirectorySlotBuilder.Short("OLD     TXT");
            deleted[0] = 0xE5;
            slots.Insert(slots.Count - 1, deleted);

            Assert.That(Parse("/", false, slots).Single().Name, Is.EqualTo("LONGFI~1.TXT"));
        }

        [Test]
        public void OrphanLongNameDoesNotReachTheNextEntryTest()
        {
            var orphan = WithLongName("Orphan name.txt", DirectorySlotBuilder.Short("ORPHAN~1TXT"));
            orphan.RemoveAt(orphan.Count - 1);
            var label = DirectorySlotBuilder.Short("LABEL      ", FatAttributes.VolumeLabel);
            var next = DirectorySlotBuilder.Short("ORPHAN~1TXT");

            var slots = orphan.Concat(new[] { label, next }).ToList();

            Assert.That(Parse("/", false, slots).Single().Name, Is.EqualTo("ORPHAN~1.TXT"));
        }

        [Test]
        public void StrayMiddleSlotIsIgnoredTest()
        {
            var slots = WithLongName("A name that is quite a bit longer than thirteen characters.txt", DirectorySlotBuilder.Short("ANAMET~1TXT"));
            var stray = slots[2];
            var shortSlot = DirectorySlotBuilder.Short("PLAIN   TXT");

            Assert.That(Parse("/", false, stray, shortSlot).Single().Name, Is.EqualTo("PLAIN.TXT"));
        }

        [TestCase(".")]
        [TestCase("..")]
        [TestCase("a/b.txt")]
        [TestCase("a\\b.txt")]
        [TestCase("tab\there.txt")]
        public void NameThatCannotBeAPathComponentFallsBackTest(string longName)
        {
            var entry = Parse("/", false, WithLongName(longName, DirectorySlotBuilder.Short("ODDNAM~1TXT"))).Single();

            Assert.That(entry.Name, Is.EqualTo("ODDNAM~1.TXT"));
            Assert.That(entry.Path, Is.EqualTo("/ODDNAM~1.TXT"));
        }

        [Test]
        public void TooManySlotsAreRejectedTest()
        {
            var shortSlot = DirectorySlotBuilder.Short("HUGE~1  TXT");
            var slots = WithLongName(new string('x', 13 * 21), shortSlot);

            Assert.That(Parse("/", false, slots).Single().Name, Is.EqualTo("HUGE~1.TXT"));
        }

        #endregion

        #region Broken Long Name Tests

        [Test]
        public void WholeLongNamesAreNotCountedTest()
        {
            var slots = WithLongName("First long name.txt", DirectorySlotBuilder.Short("FIRSTL~1TXT"))
                .Concat(WithLongName("Second long name.txt", DirectorySlotBuilder.Short("SECOND~1TXT")));

            Assert.That(CountBroken(slots), Is.Zero);
        }

        [Test]
        public void NameWithoutItsFirstSlotCountsOnceTest()
        {
            var slots = WithLongName(new string('n', 13 * 5), DirectorySlotBuilder.Short("NNNNNN~1TXT"));
            slots.RemoveAt(0);

            Assert.That(CountBroken(slots), Is.EqualTo(1));
        }

        [Test]
        public void NameOfTooManySlotsCountsOnceTest()
        {
            var slots = WithLongName(new string('x', 13 * 21), DirectorySlotBuilder.Short("HUGE~1  TXT"));

            Assert.That(CountBroken(slots), Is.EqualTo(1));
        }

        [Test]
        public void EachBrokenNameCountsOnceTest()
        {
            var wrongChecksum = WithLongName("Mixed Case Name.txt", DirectorySlotBuilder.Short("MIXEDC~1TXT"));
            wrongChecksum[^1] = DirectorySlotBuilder.Short("MIXEDC~2TXT");
            var missingSlot = WithLongName("A name that is quite a bit longer than thirteen characters.txt", DirectorySlotBuilder.Short("ANAMET~1TXT"));
            missingSlot.RemoveAt(2);
            var unusable = WithLongName("..", DirectorySlotBuilder.Short("ODDNAM~1TXT"));
            var cutShort = WithLongName("Orphan name.txt", DirectorySlotBuilder.Short("ORPHAN~1TXT"));
            cutShort.RemoveAt(cutShort.Count - 1);
            var label = DirectorySlotBuilder.Short("LABEL      ", FatAttributes.VolumeLabel);

            var slots = wrongChecksum.Concat(missingSlot).Concat(unusable).Concat(cutShort).Append(label);

            Assert.That(CountBroken(slots), Is.EqualTo(4));
        }

        #endregion

        #region Tools

        private static int CountBroken(IEnumerable<byte[]> slots)
        {
            var parser = new DirectoryParser("/", false);
            foreach (var slot in slots)
                parser.Parse(slot, out _, out _);
            return parser.BrokenLongNames;
        }

        private static List<byte[]> WithLongName(string longName, byte[] shortSlot)
        {
            var slots = DirectorySlotBuilder.Long(longName, shortSlot);
            slots.Add(shortSlot);
            return slots;
        }

        private static List<FatDirectoryEntry> Parse(string directory, bool hasHighCluster, params byte[][] slots)
        {
            return Parse(directory, hasHighCluster, (IEnumerable<byte[]>)slots);
        }

        private static List<FatDirectoryEntry> Parse(string directory, bool hasHighCluster, IEnumerable<byte[]> slots)
        {
            var parser = new DirectoryParser(directory, hasHighCluster);
            var entries = new List<FatDirectoryEntry>();
            foreach (var slot in slots)
            {
                if (parser.Parse(slot, out var item, out _) == DirectorySlotKind.Entry && item != null)
                    entries.Add(item.Entry);
            }

            return entries;
        }

        #endregion
    }
}
