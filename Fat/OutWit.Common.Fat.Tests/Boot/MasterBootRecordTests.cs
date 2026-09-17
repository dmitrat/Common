using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Boot
{
    [TestFixture]
    public class MasterBootRecordTests
    {
        #region Parse Tests

        [Test]
        public void UsedSlotsAreReadInOrderTest()
        {
            var sector = MbrBuilder.Build(0x4F575446,
                new MbrSlot(0, 0x0C, 2048, 100000, 0x80),
                new MbrSlot(2, 0x83, 102048, 5000),
                new MbrSlot(3, 0x07, 0xFFFFF000, 0xFFF));

            var record = MasterBootRecord.TryParse(sector);

            Assert.That(record, Is.Not.Null);
            Assert.That(record!.DiskSignature, Is.EqualTo(0x4F575446));
            Assert.That(record.IsGptProtective, Is.False);
            Assert.That(record.Partitions.Select(p => (p.Index, p.IsActive, p.Type, p.FirstSector, p.SectorCount)), Is.EqualTo(new[]
            {
                (0, true, (byte)0x0C, 2048L, 100000L),
                (2, false, (byte)0x83, 102048L, 5000L),
                (3, false, (byte)0x07, 0xFFFFF000L, 0xFFFL)
            }));
        }

        [Test]
        public void EmptyTableIsStillATableTest()
        {
            var record = MasterBootRecord.TryParse(MbrBuilder.Build(0));

            Assert.That(record, Is.Not.Null);
            Assert.That(record!.Partitions, Is.Empty);
        }

        [Test]
        public void MissingSignatureIsNotATableTest()
        {
            var sector = MbrBuilder.Build(0, new MbrSlot(0, 0x0C, 2048, 1000));
            sector[511] = 0;

            Assert.That(MasterBootRecord.TryParse(sector), Is.Null);
        }

        [TestCase(0x01)]
        [TestCase(0x7F)]
        [TestCase(0x81)]
        [TestCase(0xFF)]
        public void InvalidStatusByteIsNotATableTest(int status)
        {
            var sector = MbrBuilder.Build(0, new MbrSlot(1, 0x0C, 2048, 1000, (byte)status));

            Assert.That(MasterBootRecord.TryParse(sector), Is.Null);
        }

        [Test]
        public void InvalidStatusInUnusedSlotStillRejectsTableTest()
        {
            var sector = MbrBuilder.Build(0, new MbrSlot(0, 0x0C, 2048, 1000));
            sector[446 + 3 * 16] = 0x12;

            Assert.That(MasterBootRecord.TryParse(sector), Is.Null);
        }

        [Test]
        public void ProtectiveTypeMarksGuidTableTest()
        {
            var record = MasterBootRecord.TryParse(MbrBuilder.Build(0, new MbrSlot(0, 0xEE, 1, 0xFFFFFFFF)));

            Assert.That(record!.IsGptProtective, Is.True);
        }

        #endregion
    }
}
