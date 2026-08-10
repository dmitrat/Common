using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Tests.Crypto
{
    /// <summary>
    /// The wire form, and the two things a support tool needs from it: what the
    /// token actually says, and what a human reads in a log line.
    /// </summary>
    [TestFixture]
    public sealed class LicenseTokenTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        #region Raw Json Tests

        [Test]
        public void ParsedTokenCarriesTheJsonItWasBuiltFromTest()
        {
            var token = LicenseToken.Parse(Issue())!;

            Assert.Multiple(() =>
            {
                Assert.That(token.PayloadJson, Does.Contain("\"product\":\"TestProduct\""));
                Assert.That(token.HeaderJson, Does.Contain("\"kid\":\"test-key-1\""));
            });
        }

        [Test]
        public void FieldsThisBuildDoesNotKnowSurviveInTheRawJsonTest()
        {
            // The reason the raw JSON is carried rather than re-serialised from
            // the typed payload. A newer issuer may add fields; round-tripping
            // through this build's model would drop exactly the fields somebody
            // inspecting an unfamiliar licence most needs to see.
            var payload = "{\"jti\":\"abc\",\"product\":\"TestProduct\",\"somethingNewer\":{\"tier\":3}}";
            var token = LicenseToken.Parse($"{Encode("{\"alg\":\"ES256\",\"kid\":\"k\"}")}.{Encode(payload)}.{Encode("sig")}");

            Assert.Multiple(() =>
            {
                Assert.That(token, Is.Not.Null);
                Assert.That(token!.PayloadJson, Does.Contain("somethingNewer"));
                Assert.That(token.Payload.Id, Is.EqualTo("abc"), "The known fields still parse.");
            });
        }

        [Test]
        public void MalformedTokenParsesToNothingTest()
        {
            Assert.Multiple(() =>
            {
                Assert.That(LicenseToken.Parse("one.two"), Is.Null);
                Assert.That(LicenseToken.Parse("not a token"), Is.Null);
                Assert.That(LicenseToken.Parse(null), Is.Null);
            });
        }

        #endregion

        #region Description Tests

        [Test]
        public void APayloadDescribesItselfInALogLineTest()
        {
            // It used to print its own type name: ModelBase falls back to
            // object.ToString() when nothing is marked, so every log line and
            // every support tool that interpolated a licence said
            // "OutWit.Common.Licensing.Abstract.LicensePayload" and nothing else.
            var described = Payload().ToString();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Not.Contain("LicensePayload"));
                Assert.That(described, Does.Contain("TestProduct"));
                Assert.That(described, Does.Contain("Enterprise"));
                Assert.That(described, Does.Contain("2027-06-01"));
            });
        }

        [Test]
        public void AHeaderDescribesItselfTest()
        {
            var described = new LicenseTokenHeader { Algorithm = LicenseAlgorithm.ES256, KeyId = "test-key-1" }.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Contain("test-key-1"));
                Assert.That(described, Does.Contain("ES256"));
            });
        }

        [Test]
        public void ACustomerDescribesItselfTest()
        {
            var described = new LicenseCustomer { Id = "acme", Name = "ACME GmbH" }.ToString();

            Assert.That(described, Does.Contain("ACME GmbH"));
        }

        #endregion

        #region Tools

        private static string Issue()
        {
            var context = new LicenseTestContext();

            return context.Issue(LicenseTestContext.Payload(notBefore: NOW, expires: NOW.AddYears(1)));
        }

        private static LicensePayload Payload()
        {
            return new LicensePayload
            {
                Id = "licence-1",
                Product = "TestProduct",
                Edition = "Enterprise",
                NotBeforeUtc = NOW,
                ExpiresUtc = NOW.AddYears(1),
                Customer = new LicenseCustomer { Id = "acme", Name = "ACME GmbH" },
                Limits = new Dictionary<string, long> { ["maxNodes"] = 50 }
            };
        }

        private static string Encode(string text)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        #endregion
    }
}
