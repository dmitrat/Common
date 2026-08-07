using System;
using System.Linq;
using OutWit.Common.Licensing.Fingerprint;

namespace OutWit.Common.Licensing.Tests.Fingerprint
{
    /// <summary>
    /// The fingerprint is read aloud on support calls and typed back into a
    /// form, so the properties that matter are stability, readability and
    /// catching a typo before it becomes a wrong licence.
    /// </summary>
    [TestFixture]
    public sealed class FingerprintCodecTests
    {
        #region Shape Tests

        [Test]
        public void CodeHasPrefixAndFourGroupsTest()
        {
            var code = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc")));

            var parts = code.Split('-');

            Assert.Multiple(() =>
            {
                Assert.That(parts[0], Is.EqualTo("WSW"));
                Assert.That(parts, Has.Length.EqualTo(5));
                Assert.That(parts.Skip(1), Is.All.Length.EqualTo(4));
            });
        }

        [Test]
        public void CodeAvoidsConfusableLettersTest()
        {
            // Crockford omits I, L, O and U precisely so a code read aloud
            // cannot be heard or written as a different one.
            for (var index = 0; index < 200; index++)
            {
                var code = FingerprintCodec.Encode(null, LicenseTestContext.Factors(("machine-id", $"host-{index}")));
                var body = code.Replace("-", string.Empty);

                Assert.That(body.Take(15), Has.None.AnyOf('I', 'L', 'O', 'U'),
                    $"Code '{code}' contains a confusable character.");
            }
        }

        [Test]
        public void EmptyFactorsStillProduceAWellFormedCodeTest()
        {
            var code = FingerprintCodec.Encode("SRV", null);

            Assert.That(FingerprintCodec.IsWellFormed(code), Is.True);
        }

        #endregion

        #region Stability Tests

        [Test]
        public void SameFactorsProduceSameCodeTest()
        {
            var first = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc"), ("primary-mac", "AA:BB")));
            var second = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc"), ("primary-mac", "AA:BB")));

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void FactorOrderDoesNotChangeTheCodeTest()
        {
            var forward = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc"), ("primary-mac", "AA:BB")));
            var reversed = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("primary-mac", "AA:BB"), ("machine-id", "abc")));

            Assert.That(forward, Is.EqualTo(reversed),
                "A code that depended on collection order would change between runs on one machine.");
        }

        [Test]
        public void DifferentFactorsProduceDifferentCodesTest()
        {
            var first = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc")));
            var second = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "xyz")));

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void CodesDoNotCollideAcrossManyHostsTest()
        {
            var codes = Enumerable
                .Range(0, 5000)
                .Select(index => FingerprintCodec.Encode(null, LicenseTestContext.Factors(("machine-id", $"host-{index}"))))
                .ToList();

            Assert.That(codes.Distinct().Count(), Is.EqualTo(codes.Count));
        }

        #endregion

        #region Check Symbol Tests

        [Test]
        public void GeneratedCodeIsWellFormedTest()
        {
            var code = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc")));

            Assert.That(FingerprintCodec.IsWellFormed(code), Is.True);
        }

        [Test]
        public void SingleCharacterTypoIsRejectedTest()
        {
            var code = FingerprintCodec.Encode(null, LicenseTestContext.Factors(("machine-id", "abc")));
            var body = code.Replace("-", string.Empty);

            var caught = 0;
            var attempted = 0;

            for (var position = 0; position < 15; position++)
            {
                foreach (var replacement in "23456789ABCDEFGHJKMNPQRSTVWXYZ")
                {
                    if (body[position] == replacement)
                        continue;

                    var mutated = body.Substring(0, position) + replacement + body.Substring(position + 1);

                    attempted++;
                    if (!FingerprintCodec.IsWellFormed(mutated))
                        caught++;
                }
            }

            // Modulo-37 catches every single-symbol substitution; the assertion
            // is exact rather than statistical so a regression cannot hide.
            Assert.That(caught, Is.EqualTo(attempted));
        }

        [Test]
        public void GarbageIsNotWellFormedTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(FingerprintCodec.IsWellFormed(null), Is.False);
                Assert.That(FingerprintCodec.IsWellFormed(""), Is.False);
                Assert.That(FingerprintCodec.IsWellFormed("hello"), Is.False);
                Assert.That(FingerprintCodec.IsWellFormed("!!!!-!!!!"), Is.False);
            });
        }

        #endregion

        #region Normalisation Tests

        [Test]
        public void ConfusableCharactersAreFoldedOnInputTest()
        {
            // Somebody reads "0" aloud and the other end writes "O". Crockford
            // folding is what makes that not become a support ticket.
            var code = FingerprintCodec.Encode(null, LicenseTestContext.Factors(("machine-id", "abc")));
            var body = code.Replace("-", string.Empty);

            var mistyped = body.Replace('0', 'O').Replace('1', 'I');

            Assert.That(FingerprintCodec.IsWellFormed(mistyped), Is.True);
        }

        [Test]
        public void FormattingAndCaseAreIgnoredTest()
        {
            var code = FingerprintCodec.Encode("WSW", LicenseTestContext.Factors(("machine-id", "abc")));

            Assert.Multiple(() =>
            {
                Assert.That(FingerprintCodec.IsWellFormed(code.ToLowerInvariant()), Is.True);
                Assert.That(FingerprintCodec.IsWellFormed(code.Replace("-", " ")), Is.True);
                Assert.That(FingerprintCodec.IsWellFormed(code.Replace("-", string.Empty)), Is.True);
            });
        }

        #endregion
    }
}
