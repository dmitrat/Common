using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests
{
    [TestFixture]
    public class FatDirectoryEntryTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(Sample());
        }

        [Test]
        public void DirectoryIsToldByItsAttributeTest()
        {
            Assert.That(Sample().IsDirectory, Is.False);
            Assert.That(new FatDirectoryEntry { Attributes = FatAttributes.Directory | FatAttributes.Hidden }.IsDirectory, Is.True);
        }

        [Test]
        public void ToStringNamesPathAndSizeTest()
        {
            Assert.That(Sample().ToString(), Does.Contain("Path: /docs/Long File Name 1.txt").And.Contain("Length: 16"));
        }

        #endregion

        #region Tools

        private static FatDirectoryEntry Sample()
        {
            return new FatDirectoryEntry
            {
                Path = "/docs/Long File Name 1.txt",
                Name = "Long File Name 1.txt",
                ShortName = "LONGFI~1.TXT",
                Attributes = FatAttributes.Archive,
                Length = 16,
                FirstCluster = 5,
                Created = new DateTime(2024, 2, 29, 12, 0, 0, 500),
                Modified = new DateTime(2024, 2, 29, 12, 34, 56),
                Accessed = new DateTime(2024, 3, 1)
            };
        }

        #endregion
    }
}
