using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests.Validation
{
    /// <summary>
    /// Every arm of <see cref="LicenseStatus"/> has to be reachable and has to
    /// be reached for the right reason — a validator that answers "invalid" to
    /// several different problems is what turns licence questions into
    /// investigations.
    /// </summary>
    [TestFixture]
    public sealed class LicenseValidatorTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private LicenseTestContext m_context = null!;
        private LicenseValidator m_validator = null!;

        [SetUp]
        public void SetUp()
        {
            m_context = new LicenseTestContext();
            m_validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
        }

        #region Happy Path Tests

        [Test]
        public void WellFormedLicenceIsValidTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload());

            var result = m_validator.Validate(token, null, NOW);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(LicenseStatus.Valid));
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.Payload, Is.Not.Null);
                Assert.That(result.Payload!.Customer!.Name, Is.EqualTo("ACME GmbH"));
            });
        }

        [Test]
        public void UnlimitedLicenceNeverExpiresTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(unlimited: true));

            var result = m_validator.Validate(token, null, NOW.AddYears(100));

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(LicenseStatus.Valid));
                Assert.That(result.Payload!.IsUnlimited, Is.True);
            });
        }

        #endregion

        #region Structural Tests

        [Test]
        public void MissingTokenReportsMissingTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(m_validator.Validate(null, null, NOW).Status, Is.EqualTo(LicenseStatus.Missing));
                Assert.That(m_validator.Validate("   ", null, NOW).Status, Is.EqualTo(LicenseStatus.Missing));
            });
        }

        [TestCase("not-a-token")]
        [TestCase("only.two")]
        [TestCase("a.b.c.d")]
        [TestCase("!!!.!!!.!!!")]
        public void UnparseableTokenReportsMalformedTest(string token)
        {
            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.Malformed));
        }

        [Test]
        public void UnknownKeyIdReportsUnknownKeyIdTest()
        {
            var other = new LicenseTestContext();
            other.AddKey("some-other-key", LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial, LicenseTestContext.PRODUCT);

            var token = other.Issue(LicenseTestContext.Payload(), "some-other-key");

            var result = m_validator.Validate(token, null, NOW);

            Assert.That(result.Status, Is.EqualTo(LicenseStatus.UnknownKeyId));
        }

        #endregion

        #region Signature Tests

        [Test]
        public void TamperedPayloadReportsSignatureInvalidTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload());
            var parts = token.Split('.');

            // Re-encode the payload with a raised limit and keep the original
            // signature — the exact forgery the format has to defeat.
            var payloadJson = Base64UrlTestHelper.DecodeText(parts[1])!.Replace("\"Standard\"", "\"Enterprise\"");
            var forged = $"{parts[0]}.{Base64UrlTestHelper.EncodeText(payloadJson)}.{parts[2]}";

            Assert.That(m_validator.Validate(forged, null, NOW).Status, Is.EqualTo(LicenseStatus.SignatureInvalid));
        }

        [Test]
        public void SignatureFromADifferentKeyIsRejectedTest()
        {
            // Same kid, different key material: the ring's public key must be
            // what decides, not the header's claim about which key was used.
            var impostor = new LicenseTestContext();
            var token = impostor.Issue(LicenseTestContext.Payload());

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.SignatureInvalid));
        }

        [Test]
        public void HeaderAlgorithmMustMatchTheRegisteredKeyTest()
        {
            // A header claiming ES512 while the ring registers ES256 is the
            // classic algorithm-substitution probe.
            var payload = LicenseTestContext.Payload();
            var signingInput = LicenseToken.BuildSigningInput(
                new LicenseTokenHeader { Algorithm = LicenseAlgorithm.ES512, KeyId = LicenseTestContext.KEY_ID },
                payload);

            var signature = LicenseSigner.Sign(signingInput, m_context.PrivateKey(LicenseTestContext.KEY_ID), LicenseAlgorithm.ES256);
            var token = LicenseToken.Compose(signingInput, signature);

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.SignatureInvalid));
        }

        #endregion

        #region Scope Tests

        [Test]
        public void LicenceForAnotherProductReportsWrongProductTest()
        {
            m_context.AddKey(LicenseTestContext.KEY_ID, LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial,
                LicenseTestContext.PRODUCT, "OtherProduct");

            var validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
            var token = m_context.Issue(LicenseTestContext.Payload(product: "OtherProduct"));

            Assert.That(validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.WrongProduct));
        }

        [Test]
        public void KeyNotScopedToTheProductReportsExceedsKeyPolicyTest()
        {
            // A perfect signature is not enough: the key must be allowed to
            // speak for this product line at all.
            m_context.AddKey("narrow-key", LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial, "SomethingElse");

            var validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
            var token = m_context.Issue(LicenseTestContext.Payload(), "narrow-key");

            Assert.That(validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.ExceedsKeyPolicy));
        }

        [Test]
        public void TrialKeyMayNotGrantAnUnlimitedTermTest()
        {
            m_context.AddKey("trial-key", LicenseAlgorithm.ES256, LicenseKeyPolicy.TrialOnly, LicenseTestContext.PRODUCT);

            var validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
            var token = m_context.Issue(LicenseTestContext.Payload(unlimited: true), "trial-key");

            Assert.That(validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.ExceedsKeyPolicy));
        }

        [Test]
        public void TrialKeyMayNotExceedTheTermCeilingTest()
        {
            m_context.AddKey("trial-key", LicenseAlgorithm.ES256, LicenseKeyPolicy.TrialOnly, LicenseTestContext.PRODUCT);

            var validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var token = m_context.Issue(
                LicenseTestContext.Payload(notBefore: start, expires: start.AddDays(LicenseValidator.TRIAL_MAX_DAYS + 1)),
                "trial-key");

            Assert.That(validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.ExceedsKeyPolicy));
        }

        [Test]
        public void TrialKeyWithinTheCeilingIsValidTest()
        {
            m_context.AddKey("trial-key", LicenseAlgorithm.ES256, LicenseKeyPolicy.TrialOnly, LicenseTestContext.PRODUCT);

            var validator = new LicenseValidator(m_context.Ring(), LicenseTestContext.PRODUCT, new Version(1, 5, 0));
            var start = NOW.AddDays(-10);
            var token = m_context.Issue(
                LicenseTestContext.Payload(notBefore: start, expires: start.AddDays(30)),
                "trial-key");

            Assert.That(validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.Valid));
        }

        #endregion

        #region Version Tests

        [Test]
        public void VersionOutsideTheRangeReportsWrongVersionTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(appVersionRange: ">=2.0.0"));

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.WrongVersion));
        }

        [Test]
        public void VersionInsideTheRangeIsValidTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(appVersionRange: ">=1.5.0 <2.0.0"));

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.Valid));
        }

        #endregion

        #region Term Tests

        [Test]
        public void LicenceBeforeItsStartReportsNotYetValidTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddDays(10)));

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.NotYetValid));
        }

        [Test]
        public void LicencePastItsTermReportsExpiredTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-2), expires: NOW.AddDays(-1)));

            var result = m_validator.Validate(token, null, NOW);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(LicenseStatus.Expired));
                Assert.That(result.Describe(), Does.Contain("ACME GmbH"),
                    "An expired licence must still say whose it is — that is what makes support cheap.");
            });
        }

        [Test]
        public void ExpiryIsExclusiveAtTheBoundaryTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddDays(-1), expires: NOW));

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.Expired));
        }

        #endregion

        #region Binding Tests

        [Test]
        public void MatchingHostSatisfiesTheBindingTest()
        {
            var binding = LicenseTestContext.MachineBinding(2,
                ("machine-id", "abc"), ("primary-mac", "AA:BB"), ("machine-name", "WS-1"));

            var token = m_context.Issue(LicenseTestContext.Payload(binding: binding));
            var present = LicenseTestContext.Factors(("machine-id", "abc"), ("primary-mac", "AA:BB"), ("machine-name", "WS-1"));

            Assert.That(m_validator.Validate(token, present, NOW).Status, Is.EqualTo(LicenseStatus.Valid));
        }

        [Test]
        public void PartialDriftStillSatisfiesTheThresholdTest()
        {
            // The network card was replaced. Two of three still agree, and the
            // licence must survive — this is the entire point of a threshold.
            var binding = LicenseTestContext.MachineBinding(2,
                ("machine-id", "abc"), ("primary-mac", "AA:BB"), ("machine-name", "WS-1"));

            var token = m_context.Issue(LicenseTestContext.Payload(binding: binding));
            var present = LicenseTestContext.Factors(("machine-id", "abc"), ("primary-mac", "FF:EE"), ("machine-name", "WS-1"));

            Assert.That(m_validator.Validate(token, present, NOW).Status, Is.EqualTo(LicenseStatus.Valid));
        }

        [Test]
        public void DifferentHostReportsBindingMismatchTest()
        {
            var binding = LicenseTestContext.MachineBinding(2,
                ("machine-id", "abc"), ("primary-mac", "AA:BB"), ("machine-name", "WS-1"));

            var token = m_context.Issue(LicenseTestContext.Payload(binding: binding));
            var present = LicenseTestContext.Factors(("machine-id", "zzz"), ("primary-mac", "FF:EE"), ("machine-name", "WS-9"));

            var result = m_validator.Validate(token, present, NOW);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(LicenseStatus.BindingMismatch));
                Assert.That(result.Detail, Does.Contain("0 of 2"));
            });
        }

        [Test]
        public void UnboundLicenceRunsAnywhereTest()
        {
            var token = m_context.Issue(LicenseTestContext.Payload(binding: LicenseBinding.None()));

            Assert.That(m_validator.Validate(token, null, NOW).Status, Is.EqualTo(LicenseStatus.Valid));
        }

        [Test]
        public void BindingIsCheckedBeforeTheTermTest()
        {
            // Both are wrong. "Not your licence" is the more fundamental answer
            // and sends the customer to a transfer rather than a renewal that
            // would not have helped.
            var binding = LicenseTestContext.MachineBinding(1, ("machine-id", "abc"));

            var token = m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-2), expires: NOW.AddDays(-1), binding: binding));

            var present = LicenseTestContext.Factors(("machine-id", "different"));

            Assert.That(m_validator.Validate(token, present, NOW).Status, Is.EqualTo(LicenseStatus.BindingMismatch));
        }

        #endregion

        #region Helpers

        private static class Base64UrlTestHelper
        {
            public static string? DecodeText(string value)
            {
                var padded = value.Replace('-', '+').Replace('_', '/');
                padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };

                return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            }

            public static string EncodeText(string text)
            {
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text))
                    .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            }
        }

        #endregion
    }
}
