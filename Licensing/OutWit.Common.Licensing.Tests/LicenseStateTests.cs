using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// The mode table and the sentences, tested on the state alone — no store,
    /// no keys, no machine. Every arm of the table in the design is one test
    /// here, because a mode nothing can reach is a mode nobody can trust.
    /// </summary>
    [TestFixture]
    public sealed class LicenseStateTests
    {
        private static readonly DateTime NOW = new(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        #region Mode Tests

        [Test]
        public void ValidLicenceIsLicensedTest()
        {
            var state = State(LicenseValidationResult.Valid(Payload(expires: NOW.AddDays(30))));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(state.CanRun, Is.True);
            });
        }

        [Test]
        public void ValidDemoIsDemoTest()
        {
            var state = State(
                LicenseValidationResult.Valid(Payload(expires: NOW.AddDays(10))),
                isDemo: true,
                demoExpiresUtc: NOW.AddDays(10));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Demo));
                Assert.That(state.CanRun, Is.True);
            });
        }

        [Test]
        public void ExpiredInsideGraceIsGraceAndStillRunsTest()
        {
            // The whole point of the window: a lapse discovered at 3am on a
            // Sunday makes noise instead of stopping a production cluster.
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-3))),
                grace: TimeSpan.FromDays(14));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Grace));
                Assert.That(state.CanRun, Is.True);
                Assert.That(state.GraceExpiresUtc, Is.EqualTo(NOW.AddDays(11)));
            });
        }

        [Test]
        public void ExpiredPastGraceIsRestrictedTest()
        {
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-20))),
                grace: TimeSpan.FromDays(14));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(state.CanRun, Is.False);
            });
        }

        [Test]
        public void ExpiredWithNoGraceIsRestrictedTest()
        {
            // The default, and what every build that has not opted in gets. It
            // is what keeps this whole addition behaviourally invisible to a
            // product that has not asked for a grace.
            var state = State(LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-1))));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(state.GraceExpiresUtc, Is.Null);
            });
        }

        [Test]
        public void WrongVersionIsNeverGracedTest()
        {
            // A scope mismatch is not a lapse, and no amount of waiting fixes
            // one. Grace is a time concept; this is not a time problem.
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.WrongVersion, Payload(expires: NOW.AddYears(5))),
                grace: TimeSpan.FromDays(14));

            Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
        }

        [Test]
        public void DemoIsNeverGracedTest()
        {
            // Renewal grace answers "the term ended and no new document has
            // arrived". A demo has no document to renew, so a grace on one would
            // just be a longer demo decided in the wrong place.
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-1))),
                isDemo: true,
                grace: TimeSpan.FromDays(14),
                demoExpiresUtc: NOW.AddDays(-1));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(state.GraceExpiresUtc, Is.Null);
            });
        }

        [Test]
        public void SuspectClockRestrictsEvenAPerfectLicenceTest()
        {
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.ClockTampered, Payload(expires: NOW.AddYears(5))),
                isClockSuspect: true,
                clockBehindBy: TimeSpan.FromDays(2400));

            Assert.Multiple(() =>
            {
                Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(state.CanRun, Is.False);
                Assert.That(state.Payload, Is.Not.Null,
                    "The licence must survive the clock verdict, or the panel can only say 'clock tampered'.");
            });
        }

        [Test]
        public void MissingIsRestrictedTest()
        {
            var state = State(LicenseValidationResult.Failure(LicenseStatus.Missing));

            Assert.That(state.Mode, Is.EqualTo(LicenseMode.Restricted));
        }

        #endregion

        #region Clock Tests

        [Test]
        public void DemoDaysAreCountedFromTheInjectedClockTest()
        {
            // The regression. Describe() used to read DateTime.UtcNow directly,
            // so a harness travelling through time reported a demo whose day
            // count disagreed with the state it was describing.
            var state = State(
                LicenseValidationResult.Valid(Payload(expires: NOW.AddDays(10))),
                isDemo: true,
                demoExpiresUtc: NOW.AddDays(10));

            Assert.Multiple(() =>
            {
                Assert.That(state.DaysRemaining, Is.EqualTo(10));
                Assert.That(state.Describe(), Does.Contain("10 day"));
            });
        }

        [Test]
        public void SuspectClockDescribesTheObservationAndNamesTheLicenceTest()
        {
            // The legitimate causes — a restored VM, a dead RTC, a container
            // with no NTP — are all more common than the illegitimate one, so
            // the sentence reports what was seen, not what was intended.
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.ClockTampered, Payload(expires: NOW.AddYears(5))),
                isClockSuspect: true,
                clockBehindBy: TimeSpan.FromDays(2400));

            var described = state.Describe();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Contain("2400 day"));
                Assert.That(described, Does.Contain("ACME GmbH"));
                Assert.That(described, Does.Not.Contain("tamper").IgnoreCase);
                Assert.That(described, Does.Not.Contain("moved backwards"));
            });
        }

        #endregion

        #region Description Tests

        [Test]
        public void GraceNamesTheDateItEndsTest()
        {
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-3))),
                grace: TimeSpan.FromDays(14));

            var described = state.Describe();

            Assert.Multiple(() =>
            {
                Assert.That(described, Does.Contain("ACME GmbH"));
                Assert.That(described, Does.Contain("2030-06-12"), "The exact date the window closes.");
            });
        }

        [Test]
        public void GracePolicyIsDisclosedEvenWhenThereIsNoneTest()
        {
            // A grace nobody knows about produces exactly the support call it
            // was meant to prevent, one fortnight later.
            var none = State(LicenseValidationResult.Valid(Payload(expires: NOW.AddDays(30))));
            var some = State(LicenseValidationResult.Valid(Payload(expires: NOW.AddDays(30))), grace: TimeSpan.FromDays(14));

            Assert.Multiple(() =>
            {
                Assert.That(none.DescribeGracePolicy(), Does.Contain("No renewal grace"));
                Assert.That(some.DescribeGracePolicy(), Does.Contain("14 day"));
            });
        }

        [Test]
        public void UnlimitedHasNoDayCountTest()
        {
            var state = State(LicenseValidationResult.Valid(Payload(expires: null)));

            Assert.Multiple(() =>
            {
                Assert.That(state.ExpiresUtc, Is.Null);
                Assert.That(state.DaysRemaining, Is.Null);
            });
        }

        [Test]
        public void DaysRemainingGoesNegativeAfterExpiryTest()
        {
            var state = State(
                LicenseValidationResult.Failure(LicenseStatus.Expired, Payload(expires: NOW.AddDays(-3))),
                grace: TimeSpan.FromDays(14));

            Assert.That(state.DaysRemaining, Is.EqualTo(-3), "A grace banner counts against this.");
        }

        #endregion

        #region Tools

        private static LicenseState State(
            LicenseValidationResult result,
            bool isDemo = false,
            TimeSpan grace = default,
            DateTime? demoExpiresUtc = null,
            bool isClockSuspect = false,
            TimeSpan? clockBehindBy = null)
        {
            return new LicenseState(
                result,
                isDemo,
                "TST-0000-0000",
                Array.Empty<string>(),
                NOW,
                grace,
                demoExpiresUtc,
                isClockSuspect,
                clockBehindBy);
        }

        private static LicensePayload Payload(DateTime? expires)
        {
            return new LicensePayload
            {
                Id = "licence-1",
                Product = LicenseTestContext.PRODUCT,
                Edition = "Enterprise",
                IssuedUtc = NOW.AddYears(-1),
                NotBeforeUtc = NOW.AddYears(-1),
                ExpiresUtc = expires,
                Customer = new LicenseCustomer { Id = "acme", Name = "ACME GmbH" },
                Features = new List<string> { "sso" }
            };
        }

        #endregion
    }
}
