using System;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests.Validation
{
    /// <summary>
    /// The <c>appVer</c> range: the safety valve that makes an unlimited term
    /// reasonable, and — because the verifier fails open on a typo — the one
    /// thing only the issuing side can get wrong on a customer's behalf.
    /// </summary>
    [TestFixture]
    public sealed class LicenseVersionRangeTests
    {
        #region Matching Tests

        [Test]
        public void EmptyRangeCoversEverythingTest()
        {
            var range = LicenseVersionRange.Parse(string.Empty);

            Assert.Multiple(() =>
            {
                Assert.That(range.IsUnbounded, Is.True);
                Assert.That(range.Matches(new Version(1, 0)), Is.True);
                Assert.That(range.Matches(new Version(99, 0)), Is.True);
                Assert.That(range.Matches(null), Is.True);
            });
        }

        [Test]
        public void BoundedRangeRefusesAProductThatCannotStateItsVersionTest()
        {
            // Deliberate: a product that does not know its own version cannot
            // claim to be inside a bounded one. It also makes ForProduct's
            // version argument effectively mandatory, which is the point.
            var range = LicenseVersionRange.Parse(">=1.5.0 <2.0.0");

            Assert.That(range.Matches(null), Is.False);
        }

        [Test]
        public void EveryClauseMustHoldTest()
        {
            var range = LicenseVersionRange.Parse(">=1.5.0 <2.0.0");

            Assert.Multiple(() =>
            {
                Assert.That(range.Matches(new Version(1, 5, 0)), Is.True);
                Assert.That(range.Matches(new Version(1, 9, 9)), Is.True);
                Assert.That(range.Matches(new Version(1, 4, 9)), Is.False);
                Assert.That(range.Matches(new Version(2, 0, 0)), Is.False);
            });
        }

        [Test]
        public void ShortVersionIsPaddedRatherThanTreatedAsSmallerTest()
        {
            // System.Version leaves unspecified components at -1, so 1.5 sorts
            // below 1.5.0. Left alone that refuses a perfectly good licence.
            var range = LicenseVersionRange.Parse(">=1.5.0");

            Assert.That(range.Matches(new Version(1, 5)), Is.True);
        }

        #endregion

        #region Fail-Open Tests

        [Test]
        public void AWhollyMalformedRangeStillCoversEverythingTest()
        {
            // A customer must never be dead because of a typo they cannot see
            // or fix. The cost of that decision is that the issuing form is the
            // only place the typo can be caught — hence the reporting below.
            var range = LicenseVersionRange.Parse("nonsense");

            Assert.Multiple(() =>
            {
                Assert.That(range.Matches(new Version(1, 0)), Is.True);
                Assert.That(range.IsUnbounded, Is.True);
            });
        }

        [Test]
        public void DroppedClausesAreReportedTest()
        {
            var range = LicenseVersionRange.Parse(">=1.5.0 <2.x !=nope", out var rejected);

            Assert.Multiple(() =>
            {
                Assert.That(rejected, Is.EqualTo(new[] { "<2.x", "!=nope" }));
                Assert.That(range.Clauses, Is.EqualTo(new[] { ">=1.5.0" }));
            });
        }

        [Test]
        public void NothingIsRejectedFromACleanRangeTest()
        {
            LicenseVersionRange.Parse(">=1.5.0 <2.0.0", out var rejected);

            Assert.That(rejected, Is.Empty);
        }

        #endregion

        #region Upper Bound Tests

        [Test]
        public void AFloorAloneIsNotACeilingTest()
        {
            // The distinction the whole Unlimited rule rests on: ">=1.5.0" has a
            // clause, so it is not unbounded, and it still covers every major
            // version ever to be written.
            var range = LicenseVersionRange.Parse(">=1.5.0");

            Assert.Multiple(() =>
            {
                Assert.That(range.IsUnbounded, Is.False);
                Assert.That(range.HasUpperBound, Is.False);
            });
        }

        [TestCase(">=1.5.0 <2.0.0", true)]
        [TestCase("<=1.9.9", true)]
        [TestCase("=1.5.0", true)]
        [TestCase("1.5.0", true)]
        [TestCase(">1.5.0", false)]
        [TestCase("!=1.6.0", false)]
        [TestCase("", false)]
        public void UpperBoundIsDetectedTest(string range, bool expected)
        {
            Assert.That(LicenseVersionRange.Parse(range).HasUpperBound, Is.EqualTo(expected));
        }

        [Test]
        public void TheTrapRangeLosesItsCeilingSilentlyTest()
        {
            // ">=1.5.0 <2.x" is the exact shape the design names: it looks
            // bounded, parses to a floor alone, and pairs with an unlimited term
            // to grant every future major version for nothing.
            var range = LicenseVersionRange.Parse(">=1.5.0 <2.x", out var rejected);

            Assert.Multiple(() =>
            {
                Assert.That(rejected, Is.EqualTo(new[] { "<2.x" }));
                Assert.That(range.HasUpperBound, Is.False);
                Assert.That(range.Matches(new Version(9, 0, 0)), Is.True);
            });
        }

        #endregion

        #region Description Tests

        [Test]
        public void AnEmptyRangeSaysWhatThatMeansTest()
        {
            Assert.That(LicenseVersionRange.Parse(string.Empty).Describe(),
                Does.Contain("do not exist yet"));
        }

        [Test]
        public void ClausesAreDescribedAsTypedTest()
        {
            // Described from the text as written, not from the padded value, so
            // "<2.0.0" does not read back as "below 2.0.0.0".
            var described = LicenseVersionRange.Parse(">=1.5.0 <2.0.0").Describe();

            Assert.Multiple(() =>
            {
                Assert.That(described, Is.EqualTo("1.5.0 or later, below 2.0.0."));
                Assert.That(described, Does.Not.Contain("0.0.0.0"));
            });
        }

        [TestCase("<=1.9.9", "1.9.9 or earlier.")]
        [TestCase(">1.5.0", "after 1.5.0.")]
        [TestCase("!=1.6.0", "except 1.6.0.")]
        [TestCase("=1.5.0", "exactly 1.5.0.")]
        [TestCase("1.5.0", "exactly 1.5.0.")]
        public void EveryOperatorHasWordsTest(string range, string expected)
        {
            Assert.That(LicenseVersionRange.Parse(range).Describe(), Is.EqualTo(expected));
        }

        [Test]
        public void ARangeReadsBackAsItWasWrittenTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(LicenseVersionRange.Parse(">=1.5.0 <2.0.0").ToString(), Is.EqualTo(">=1.5.0 <2.0.0"));
                Assert.That(LicenseVersionRange.Parse(string.Empty).ToString(), Is.EqualTo("(any version)"));
            });
        }

        #endregion
    }
}
