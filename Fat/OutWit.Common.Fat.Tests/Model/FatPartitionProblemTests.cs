using OutWit.Common.Fat.Model;
using OutWit.Common.Fat.Tests.Utils;

namespace OutWit.Common.Fat.Tests.Model
{
    [TestFixture]
    public class FatPartitionProblemTests
    {
        #region Model Tests

        [Test]
        public void ModelContractHoldsTest()
        {
            ModelBaseContract.AssertHolds(Sample());
        }

        [Test]
        public void ToStringExplainsTheProblemTest()
        {
            Assert.That(Sample().ToString(), Does.Contain("Kind: SectorSizeMismatch").And.Contain("Message: 4096-byte sectors"));
        }

        #endregion

        #region Tools

        private static FatPartitionProblem Sample()
        {
            return new FatPartitionProblem
            {
                Partition = new MbrPartitionEntry { Index = 2, Type = 0x0C, FirstSector = 2048, SectorCount = 4096 },
                Kind = FatErrorKind.SectorSizeMismatch,
                Message = "4096-byte sectors"
            };
        }

        #endregion
    }
}
