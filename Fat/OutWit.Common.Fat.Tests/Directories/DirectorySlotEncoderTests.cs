using System.Buffers.Binary;
using System.Text;
using OutWit.Common.Fat.Directories;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Directories
{
    [TestFixture]
    public class DirectorySlotEncoderTests
    {
        #region Constants

        private static readonly DateTime CREATED = new(2025, 3, 4, 5, 6, 7, 890);

        private static readonly DateTime MODIFIED = new(2026, 9, 16, 22, 33, 44);

        #endregion

        #region Short Entry Tests

        [Test]
        public void ShortEntryMatchesTheSpecificationTest()
        {
            var slot = new byte[32];

            DirectorySlotEncoder.WriteShort(slot, Name("DATA    BIN"), 0x08, FatAttributes.Archive, 0x0001_0203, 4096, CREATED, MODIFIED);

            var expected = DirectorySlotBuilder.Short("DATA    BIN", FatAttributes.Archive, 0x0001_0203, 4096, 0x08,
                modifiedDate: Date(2026, 9, 16), modifiedTime: Time(22, 33, 44));
            BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(14), Time(5, 6, 6));
            BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(16), Date(2025, 3, 4));
            BinaryPrimitives.WriteUInt16LittleEndian(expected.AsSpan(18), Date(2026, 9, 16));
            expected[13] = 189;
            Assert.That(slot, Is.EqualTo(expected));
        }

        [Test]
        public void ShortEntryParsesBackTest()
        {
            var slot = new byte[32];
            DirectorySlotEncoder.WriteShort(slot, Name("DATA    BIN"), 0x08, FatAttributes.Archive | FatAttributes.Hidden, 70000, 123, CREATED, MODIFIED);

            var entry = Parse(slot);

            Assert.That(entry.Name, Is.EqualTo("data.BIN"));
            Assert.That(entry.Attributes, Is.EqualTo(FatAttributes.Archive | FatAttributes.Hidden));
            Assert.That(entry.FirstCluster, Is.EqualTo(70000));
            Assert.That(entry.Length, Is.EqualTo(123));
            Assert.That(entry.Created, Is.EqualTo(new DateTime(2025, 3, 4, 5, 6, 7, 890)));
            Assert.That(entry.Modified, Is.EqualTo(MODIFIED));
            Assert.That(entry.Accessed, Is.EqualTo(MODIFIED.Date));
        }

        [Test]
        public void FieldSettersKeepTheRestTest()
        {
            var slot = new byte[32];
            DirectorySlotEncoder.WriteShort(slot, Name("OLD     TXT"), 0, FatAttributes.ReadOnly, 5, 6, CREATED, CREATED);
            var before = slot.ToArray();

            DirectorySlotEncoder.SetFirstCluster(slot, 0x0ABC_DEF0);
            DirectorySlotEncoder.SetSize(slot, 0xFFFF_FFFF);
            DirectorySlotEncoder.SetModified(slot, MODIFIED);
            DirectorySlotEncoder.Rename(slot, Name("NEW     DAT"), 0x10);

            Assert.That(Encoding.ASCII.GetString(slot, 0, 11), Is.EqualTo("NEW     DAT"));
            Assert.That(slot[12], Is.EqualTo(0x10));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(slot.AsSpan(20)), Is.EqualTo(0x0ABC));
            Assert.That(BinaryPrimitives.ReadUInt16LittleEndian(slot.AsSpan(26)), Is.EqualTo(0xDEF0));
            Assert.That(BinaryPrimitives.ReadUInt32LittleEndian(slot.AsSpan(28)), Is.EqualTo(0xFFFF_FFFF));
            Assert.That(slot[11], Is.EqualTo(before[11]));
            Assert.That(slot.AsSpan(13, 5).SequenceEqual(before.AsSpan(13, 5)), Is.True);
        }

        #endregion

        #region Long Name Tests

        [TestCase("a")]
        [TestCase("Thirteen char")]
        [TestCase("Fourteen chars")]
        [TestCase("Twenty-six characters now")]
        [TestCase("Кириллица и 日本語 together, long enough for three slots")]
        public void LongSlotsMatchTheSpecificationTest(string name)
        {
            var shortSlot = DirectorySlotBuilder.Short("LONGNA~1TXT");
            var expected = DirectorySlotBuilder.Long(name, shortSlot);
            byte checksum = ShortName.Checksum(shortSlot);

            int count = DirectorySlotEncoder.LongSlotCount(name);
            var slots = new List<byte[]>();
            for (int ordinal = count; ordinal >= 1; ordinal--)
            {
                var slot = new byte[32];
                DirectorySlotEncoder.WriteLong(slot, name, ordinal, checksum);
                slots.Add(slot);
            }

            Assert.That(slots, Is.EqualTo(expected));
            Assert.That(checksum, Is.EqualTo(DirectorySlotBuilder.Checksum(shortSlot)));
        }

        [TestCase(1, 1)]
        [TestCase(13, 1)]
        [TestCase(14, 2)]
        [TestCase(26, 2)]
        [TestCase(27, 3)]
        [TestCase(255, 20)]
        public void LongSlotCountTest(int length, int slots)
        {
            Assert.That(DirectorySlotEncoder.LongSlotCount(new string('x', length)), Is.EqualTo(slots));
        }

        [Test]
        public void LongNameParsesBackTest()
        {
            string name = new string('n', 251) + ".txt";
            var shortSlot = new byte[32];
            DirectorySlotEncoder.WriteShort(shortSlot, Name("NNNNNN~1TXT"), 0, FatAttributes.Archive, 0, 0, CREATED, CREATED);
            byte checksum = ShortName.Checksum(shortSlot);

            var parser = new DirectoryParser("/max", hasHighCluster: false);
            for (int ordinal = DirectorySlotEncoder.LongSlotCount(name); ordinal >= 1; ordinal--)
            {
                var slot = new byte[32];
                DirectorySlotEncoder.WriteLong(slot, name, ordinal, checksum);
                parser.Parse(slot, out _, out _);
            }

            parser.Parse(shortSlot, out var item, out _);
            Assert.That(item!.Entry.Name, Is.EqualTo(name));
            Assert.That(item.Entry.Path, Is.EqualTo("/max/" + name));
            Assert.That((item.FirstSlot, item.LastSlot), Is.EqualTo((0L, 20L)));
        }

        #endregion

        #region Tools

        private static byte[] Name(string stored)
        {
            return Encoding.ASCII.GetBytes(stored);
        }

        private static FatDirectoryEntry Parse(byte[] slot)
        {
            new DirectoryParser("/", hasHighCluster: true).Parse(slot, out var item, out _);
            return item!.Entry;
        }

        private static ushort Date(int year, int month, int day)
        {
            return (ushort)((year - 1980) << 9 | month << 5 | day);
        }

        private static ushort Time(int hour, int minute, int second)
        {
            return (ushort)(hour << 11 | minute << 5 | second / 2);
        }

        #endregion
    }
}
