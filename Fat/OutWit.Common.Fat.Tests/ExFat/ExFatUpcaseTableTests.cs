using System.Buffers.Binary;
using OutWit.Common.Fat.ExFat;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.ExFat
{
    [TestFixture]
    public class ExFatUpcaseTableTests
    {
        #region Parse Tests

        [Test]
        public void CompressedRunsMapCharactersToThemselvesTest()
        {
            var table = ExFatUpcaseTable.Parse(ExFatSetBuilder.UpcaseTable("ABC"));

            Assert.That(new[] { table.ToUpper('a'), table.ToUpper('b'), table.ToUpper('c') }, Is.EqualTo("ABC".ToCharArray()));
            Assert.That(table.ToUpper('A'), Is.EqualTo('A'));
            Assert.That(table.ToUpper('\0'), Is.EqualTo('\0'));
        }

        [Test]
        public void CharactersPastTheTableAreThemselvesTest()
        {
            var table = ExFatUpcaseTable.Parse(ExFatSetBuilder.UpcaseTable("ABC"));

            Assert.That(table.ToUpper('d'), Is.EqualTo('d'));
            Assert.That(table.ToUpper('я'), Is.EqualTo('я'));
            Assert.That(table.ToUpper('￿'), Is.EqualTo('￿'));
        }

        [Test]
        public void UncompressedTableIsReadAsItStandsTest()
        {
            var data = new byte[0x10000 * 2];
            for (int i = 0; i < 0x10000; i++)
                BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(i * 2), (ushort)(i == 'q' ? 'Q' : i));

            var table = ExFatUpcaseTable.Parse(data);

            Assert.That(table.ToUpper('q'), Is.EqualTo('Q'));
            Assert.That(table.ToUpper('w'), Is.EqualTo('w'));
            Assert.That(table.ToUpper('￿'), Is.EqualTo('￿'));
        }

        [Test]
        public void TableThatMapsLettersElsewhereIsFollowedTest()
        {
            var table = ExFatUpcaseTable.Parse(ExFatSetBuilder.UpcaseTable("ABCDEFGHIJKLMNOPQRSTUVWYYZ"));

            Assert.That(table.NamesEqual("x.bin", "Y.BIN"), Is.True);
            Assert.That(table.NamesEqual("x.bin", "X.BIN"), Is.False);
            Assert.That(table.NamesEqual("x.bin", "x.bin"), Is.True);
            Assert.That(table.NamesEqual("x.bin", "x.bin "), Is.False);
        }

        [Test]
        public void TrailingOddByteIsIgnoredTest()
        {
            var data = ExFatSetBuilder.UpcaseTable("B").Append((byte)0x41).ToArray();

            Assert.That(ExFatUpcaseTable.Parse(data).ToUpper('a'), Is.EqualTo('B'));
        }

        #endregion

        #region Hash Tests

        [TestCase("random.bin", 0x7F79)]
        [TestCase("frag", 48174)]
        [TestCase("RANDOM.BIN", 0x7F79)]
        public void NameHashIsOverTheUpperCaseTest(string name, int hash)
        {
            var table = ExFatUpcaseTable.Parse(ExFatSetBuilder.UpcaseTable("ABCDEFGHIJKLMNOPQRSTUVWXYZ"));

            Assert.That(table.HashOf(name), Is.EqualTo(hash));
        }

        [Test]
        public void ChecksumMatchesTheSpecificationsRotationTest()
        {
            var data = Enumerable.Range(0, 1000).Select(i => (byte)(i * 7)).ToArray();

            Assert.That(ExFatChecksum.OfUpcaseTable(data), Is.EqualTo(ExFatSetBuilder.UpcaseChecksum(data)));
        }

        [Test]
        public void EntrySetChecksumSkipsItsOwnBytesTest()
        {
            var set = new ExFatSetBuilder { Name = "checksum.bin" }.Build();
            var changed = (byte[])set.Clone();
            changed[2] ^= 0xFF;
            changed[3] ^= 0xFF;

            Assert.That(ExFatChecksum.OfEntrySet(set), Is.EqualTo(ExFatSetBuilder.SetChecksum(set)));
            Assert.That(ExFatChecksum.OfEntrySet(changed), Is.EqualTo(ExFatChecksum.OfEntrySet(set)));
        }

        #endregion
    }
}
