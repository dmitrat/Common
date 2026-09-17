using OutWit.Common.Fat.Boot;
using OutWit.Common.Fat.Tests.Utils;
using OutWit.Common.NUnit;
using OutWit.Common.Utils;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatDiskLayoutTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(Sample());
        }

        [Test]
        public void EmptyLayoutsAreEqualTest()
        {
            var first = new FatDiskLayout();
            var second = new FatDiskLayout();

            Assert.That(first, Was.EqualTo(second));
            Assert.That(first, Was.Not.EqualTo(Sample()));
        }

        [Test]
        public void ListsAreComparedByContentTest()
        {
            var sample = Sample();
            var rebuilt = sample.With(x => x.Partitions, sample.Partitions.Select(p => p.Clone()).ToList());

            Assert.That(rebuilt, Was.EqualTo(sample));
        }

        [Test]
        public void ToStringNamesTableTest()
        {
            Assert.That(Sample().ToString(), Does.Contain("PartitionTable: Mbr").And.Contain("DiskSignature: 4F575446"));
        }

        #endregion

        #region Tools

        private static FatDiskLayout Sample()
        {
            var fat = new MbrPartitionEntry { Index = 0, Type = 0x0C, FirstSector = 2048, SectorCount = 100000 };
            var linux = new MbrPartitionEntry { Index = 1, Type = 0x83, FirstSector = 102048, SectorCount = 5000 };

            return new FatDiskLayout
            {
                PartitionTable = PartitionTableKind.Mbr,
                DiskSignature = 0x4F575446,
                Partitions = new[] { fat, linux },
                Volumes = new[]
                {
                    new FatVolumeLocation
                    {
                        Partition = fat,
                        FirstSector = 2048,
                        SectorCount = 100000,
                        Volume = new FatVolumeInfo { Kind = FatKind.Fat32, SectorSize = 512, SectorsPerCluster = 1, TotalSectors = 100000 }
                    },
                    new FatVolumeLocation
                    {
                        FirstSector = 0,
                        SectorCount = 10,
                        Volume = new FatVolumeInfo { Kind = FatKind.Fat12 }
                    }
                },
                Problems = new[]
                {
                    new FatPartitionProblem { Partition = linux, Kind = FatErrorKind.Corrupt, Message = "broken" },
                    new FatPartitionProblem { Partition = fat, Kind = FatErrorKind.Unsupported, Message = "odd" }
                }
            };
        }

        #endregion
    }
}
