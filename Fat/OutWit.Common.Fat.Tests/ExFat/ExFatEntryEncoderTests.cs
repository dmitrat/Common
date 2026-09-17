using System.Buffers.Binary;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatEntryEncoderTests
    {
        #region Constants

        private static readonly DateTimeOffset NOW = new(2026, 9, 16, 21, 45, 13, 370, TimeSpan.FromHours(3));

        #endregion

        #region Create Tests

        [Test]
        public void NewSetReadsBackAsWrittenTest()
        {
            var header = ExFatEntryEncoder.Create(FatAttributes.Archive, NOW, 40, 10000, isContiguous: true);
            var set = ExFatEntryEncoder.Rename(header, "A name longer than fifteen units.bin", 0x1234);

            var item = Parse(set);

            Assert.That(set, Has.Length.EqualTo(5 * ExFatEntry.SIZE));
            Assert.That(item.Entry.Name, Is.EqualTo("A name longer than fifteen units.bin"));
            Assert.That((item.Entry.FirstCluster, item.Entry.Length, item.ValidLength, item.IsContiguous), Is.EqualTo((40u, 10000L, 10000L, true)));
            Assert.That(item.Entry.Created, Is.EqualTo(new DateTime(2026, 9, 16, 18, 45, 13, 370)));
            Assert.That(item.Entry.Modified, Is.EqualTo(new DateTime(2026, 9, 16, 18, 45, 13, 370)));
            Assert.That(item.Entry.Accessed, Is.EqualTo(new DateTime(2026, 9, 16, 18, 45, 12)));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(set.AsSpan(ExFatEntry.SIZE + ExFatEntry.NAME_HASH)), Is.EqualTo(0x1234));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(set.AsSpan(ExFatEntry.SET_CHECKSUM)), Is.EqualTo(ExFatSetBuilder.SetChecksum(set)));
        }

        [Test]
        public void EmptyFileHasNoContiguityFlagTest()
        {
            var header = ExFatEntryEncoder.Create(FatAttributes.Archive, NOW, 0, 0, isContiguous: true);

            Assert.That(header[ExFatEntry.SIZE + ExFatEntry.SECONDARY_FLAGS], Is.EqualTo(ExFatEntry.ALLOCATION_POSSIBLE));
        }

        #endregion

        #region Rename Tests

        [TestCase("a", 3)]
        [TestCase("fifteen units..", 3)]
        [TestCase("sixteen units...", 4)]
        public void RenamedSetHasRoomForItsNameTest(string name, int entries)
        {
            var original = ExFatEntryEncoder.Rename(ExFatEntryEncoder.Create(FatAttributes.None, NOW, 7, 1, false), new string('x', 40), 1);

            var renamed = ExFatEntryEncoder.Rename(original, name, 2);

            Assert.That(renamed, Has.Length.EqualTo(entries * ExFatEntry.SIZE));
            Assert.That(renamed[ExFatEntry.SECONDARY_COUNT], Is.EqualTo(entries - 1));
            Assert.That(Parse(renamed).Entry.Name, Is.EqualTo(name));
            Assert.That(WithoutNameFields(renamed), Is.EqualTo(WithoutNameFields(original)));
        }

        [Test]
        public void BenignEntriesAfterTheNameAreKeptTest()
        {
            var builder = new ExFatSetBuilder { Name = "old name that is long.txt" };
            var vendor = ExFatSetBuilder.Entry(0xE0, 0x42);
            builder.Extra.Add(vendor);

            var renamed = ExFatEntryEncoder.Rename(builder.Build(), "new.txt", 5);

            Assert.That(renamed, Has.Length.EqualTo(4 * ExFatEntry.SIZE));
            Assert.That(renamed.AsSpan(3 * ExFatEntry.SIZE).ToArray(), Is.EqualTo(vendor));
            Assert.That(Parse(renamed).Entry.Name, Is.EqualTo("new.txt"));
        }

        [Test]
        public void SetThatWouldPassItsLimitIsRefusedTest()
        {
            var builder = new ExFatSetBuilder { Name = "short name.txt" };
            for (int i = 0; i < 240; i++)
                builder.Extra.Add(ExFatSetBuilder.Entry(0xE0));
            var set = builder.Build();

            var fits = ExFatEntryEncoder.Rename(set, new string('n', 210), 1);

            Assert.That(fits[ExFatEntry.SECONDARY_COUNT], Is.EqualTo(255));
            Assert.Throws<ArgumentException>(() => ExFatEntryEncoder.Rename(set, new string('n', 211), 1));
        }

        #endregion

        #region Change Tests

        [Test]
        public void StreamAndTimesAreChangedInPlaceTest()
        {
            var set = ExFatEntryEncoder.Rename(ExFatEntryEncoder.Create(FatAttributes.ReadOnly, NOW, 0, 0, false), "f", 1);
            var later = NOW.AddDays(1).ToOffset(TimeSpan.FromMinutes(-150));

            ExFatEntryEncoder.SetStream(set, 9, 5000, 1000, isContiguous: false);
            ExFatEntryEncoder.SetModified(set, later);
            ExFatEntryEncoder.AddAttributes(set, FatAttributes.Archive);
            ExFatEntryEncoder.Seal(set);

            var item = Parse(set);
            Assert.That((item.Entry.FirstCluster, item.Entry.Length, item.ValidLength, item.IsContiguous), Is.EqualTo((9u, 5000L, 1000L, false)));
            Assert.That(item.Entry.Attributes, Is.EqualTo(FatAttributes.ReadOnly | FatAttributes.Archive));
            Assert.That(item.Entry.Modified, Is.EqualTo(later.UtcDateTime));
            Assert.That(item.Entry.Created, Is.EqualTo(NOW.UtcDateTime));
            Assert.That(set[ExFatEntry.SIZE + ExFatEntry.SECONDARY_FLAGS], Is.EqualTo(ExFatEntry.ALLOCATION_POSSIBLE));
        }

        #endregion

        #region Tools

        /// <summary>
        /// The file and stream entries without what a new name changes: the secondary count,
        /// the checksum, the name's length and hash.
        /// </summary>
        private static byte[] WithoutNameFields(byte[] set)
        {
            var header = set[..(2 * ExFatEntry.SIZE)];
            foreach (int offset in new[] { 1, 2, 3, 35, 36, 37 })
                header[offset] = 0;
            return header;
        }

        private static DirectoryItem Parse(byte[] set)
        {
            var parser = new ExFatEntrySetParser("/", 2);
            DirectoryItem? item = null;
            for (int offset = 0; offset < set.Length; offset += ExFatEntry.SIZE)
                parser.Parse(set.AsSpan(offset, ExFatEntry.SIZE), out item, out _);
            return item!;
        }

        #endregion
    }
}
