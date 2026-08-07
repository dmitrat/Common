using OutWit.Common.Abstract;
using OutWit.Common.Platform.Models.MachineIdentity;

namespace OutWit.Common.Platform.Tests.Identity
{
    /// <summary>
    /// MachineFactor is a ModelBase, so value comparison and cloning have to
    /// behave the way the rest of the ecosystem assumes.
    /// </summary>
    [TestFixture]
    public sealed class MachineFactorTests
    {
        #region Is Tests

        [Test]
        public void EqualValuesAreTest()
        {
            var first = Factor(MachineFactorKeys.MACHINE_ID, "abc");
            var second = Factor(MachineFactorKeys.MACHINE_ID, "abc");

            Assert.Multiple(() =>
            {
                Assert.That(first.Is(second), Is.True);
                Assert.That(first.Equals(second), Is.False, "Reference identity must stay reference identity.");
            });
        }

        [Test]
        public void DifferentValueIsNotTest()
        {
            var first = Factor(MachineFactorKeys.MACHINE_ID, "abc");
            var second = Factor(MachineFactorKeys.MACHINE_ID, "def");

            Assert.That(first.Is(second), Is.False);
        }

        [Test]
        public void DifferentKeyIsNotTest()
        {
            var first = Factor(MachineFactorKeys.MACHINE_ID, "abc");
            var second = Factor(MachineFactorKeys.MACHINE_NAME, "abc");

            Assert.That(first.Is(second), Is.False,
                "The same value under a different key is a different observation.");
        }

        [Test]
        public void OtherModelIsNotTest()
        {
            Assert.That(Factor(MachineFactorKeys.MACHINE_ID, "abc").Is(new OtherModel()), Is.False);
        }

        #endregion

        #region Clone Tests

        [Test]
        public void CloneIsEqualByValueAndDistinctByReferenceTest()
        {
            var original = Factor(MachineFactorKeys.PRIMARY_MAC, "AA:BB:CC:DD:EE:FF");
            var clone = original.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(original.Is(clone), Is.True);
                Assert.That(ReferenceEquals(original, clone), Is.False);
                Assert.That(clone.Key, Is.EqualTo(original.Key));
                Assert.That(clone.Value, Is.EqualTo(original.Value));
            });
        }

        #endregion

        #region Tools

        private static MachineFactor Factor(string key, string value)
        {
            return new MachineFactor { Key = key, Value = value };
        }

        private sealed class OtherModel : ModelBase
        {
            public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE) => false;
            public override ModelBase Clone() => new OtherModel();
        }

        #endregion
    }
}
