using MemoryPack;
using MessagePack;
using MessagePack.Resolvers;
using NUnit.Framework;
using OutWit.Common.Forms.Model;

namespace OutWit.Common.Forms.Tests
{
    /// <summary>
    /// The model as an OutWit model: how it compares, how it copies, how it says what it is, and
    /// what it costs to put it on a wire that is not the native one.
    /// </summary>
    [TestFixture]
    public class FormModelTests
    {
        #region Tools

        private static FormField Field()
        {
            return new FormField("Brady.Limit", "Forms.Brady.Limit", FormFieldKind.Number)
            {
                Minimum = 30,
                Maximum = 120,
                UnitKey = "Forms.Bpm",
                EnabledWhen = FormCondition.On("Brady", FormOperator.IsTrue)
            };
        }

        #endregion

        #region Tests

        /// <summary>
        /// A copy is equal to what it was copied from, and is not the same object — including the
        /// condition hanging off it, which is the part a shallow copy would share.
        /// </summary>
        [Test]
        public void ACopyIsEqualAndSeparateTest()
        {
            var field = Field();
            var copy = field.Clone();

            copy.EnabledWhen!.Key = "Something else";

            Assert.Multiple(() =>
            {
                Assert.That(copy, Is.Not.SameAs(field));
                Assert.That(field.EnabledWhen!.Key, Is.EqualTo("Brady"), "the original is untouched");
            });
        }

        /// <summary>
        /// Comparison goes all the way down, and a difference anywhere is a difference.
        /// </summary>
        [Test]
        public void ComparisonReachesTheConditionsTest()
        {
            var field = Field();
            var same = field.Clone();
            var other = field.Clone();

            other.EnabledWhen!.Operator = FormOperator.IsFalse;

            Assert.Multiple(() =>
            {
                Assert.That(field.Is(same), Is.True);
                Assert.That(field.Is(other), Is.False);
            });
        }

        /// <summary>
        /// A missing condition compares equal to a missing condition, and unequal to a present one.
        /// </summary>
        /// <remarks>
        /// The case a hand-written null check gets wrong, which is why it is not hand-written:
        /// <c>Check</c> from OutWit.Common answers it.
        /// </remarks>
        [Test]
        public void NothingComparesWithNothingTest()
        {
            var bare = new FormField("Key", "Forms.Key", FormFieldKind.Text);
            var alsoBare = new FormField("Key", "Forms.Key", FormFieldKind.Text);

            var guarded = new FormField("Key", "Forms.Key", FormFieldKind.Text)
            {
                VisibleWhen = FormCondition.On("Other", FormOperator.IsTrue)
            };

            Assert.Multiple(() =>
            {
                Assert.That(bare.Is(alsoBare), Is.True);
                Assert.That(bare.Is(guarded), Is.False);
                Assert.That(guarded.Is(bare), Is.False);
            });
        }

        /// <summary>
        /// A model says what it is without anybody writing a ToString: the properties worth
        /// reading carry <c>[ToString]</c> and ModelBase composes them.
        /// </summary>
        [Test]
        public void AModelSaysWhatItIsTest()
        {
            Assert.That(Field().ToString(), Is.EqualTo("Key: Brady.Limit, Kind: Number"));
        }

        /// <summary>
        /// A bag of values says how many it holds and not what they are: what is in a form is
        /// somebody's data as often as not, and a model that prints it by default prints it into
        /// every log that ever touches it.
        /// </summary>
        [Test]
        public void ABagOfValuesDoesNotPrintItselfTest()
        {
            var values = new FormValues().Set("Patient.Name", "Reyes");

            Assert.That(values.ToString(), Does.Not.Contain("Reyes"));
        }

        /// <summary>
        /// The native wire is MemoryPack, which is what WitRPC speaks without being asked.
        /// </summary>
        [Test]
        public void TheNativeWireIsMemoryPackTest()
        {
            var field = Field();

            var back = MemoryPackSerializer.Deserialize<FormField>(MemoryPackSerializer.Serialize(field))!;

            Assert.That(back.Is(field), Is.True);
        }

        /// <summary>
        /// And another serialiser costs nothing at this end: the model carries no attribute of any
        /// other format, so MessagePack takes it contractlessly.
        /// </summary>
        /// <remarks>
        /// This test is the claim under "Serialisation" in the package README made checkable. Welding a second
        /// serialiser's attributes into the model would make the package a private arrangement
        /// with that serialiser — which is exactly what this package must not be, since a form
        /// crossing a wire is the whole point of it.
        /// </remarks>
        [Test]
        public void AnotherSerialiserCostsNothingHereTest()
        {
            var field = Field();

            var options = ContractlessStandardResolver.Options;

            var back = MessagePackSerializer.Deserialize<FormField>(
                MessagePackSerializer.Serialize(field, options), options);

            Assert.That(back.Is(field), Is.True);
        }

        #endregion
    }
}
