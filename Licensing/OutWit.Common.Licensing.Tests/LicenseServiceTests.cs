using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// The service is where the pieces meet: demo, selection among several
    /// installed documents, supersession, the clock guard, and the vocabulary
    /// report.
    /// </summary>
    [TestFixture]
    public sealed class LicenseServiceTests
    {
        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private LicenseTestContext m_context = null!;
        private LicenseStoreMemory m_store = null!;
        private DateTime m_clock;

        [SetUp]
        public void SetUp()
        {
            m_context = new LicenseTestContext();
            m_store = new LicenseStoreMemory();
            m_clock = NOW;
        }

        #region Demo Tests

        [Test]
        public async Task NoLicenceRunsInDemoTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            Assert.Multiple(() =>
            {
                Assert.That(service.State.IsDemo, Is.True);
                Assert.That(service.State.CanRun, Is.True);
                Assert.That(service.State.Describe(), Does.Contain("Demo"));
            });
        }

        [Test]
        public async Task DemoEndsAfterItsTermTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            m_clock = NOW.AddDays(31);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.IsDemo, Is.True);
                Assert.That(service.State.CanRun, Is.False);
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.Expired));
            });
        }

        [Test]
        public async Task DemoIsAnchoredToFirstRunNotToEachStartTest()
        {
            // Restarting the product must not restart the demo — the anchor is
            // the sidecar's first-run stamp, not this process.
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            m_clock = NOW.AddDays(20);
            await service.ReloadAsync();

            var restarted = await CreateAsync(demo: TimeSpan.FromDays(30));

            Assert.That(restarted.State.DemoExpiresUtc, Is.EqualTo(NOW.AddDays(30)));
        }

        [Test]
        public async Task DemoAppliesItsOwnLimitsTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30), configureDemo: demo => demo.Limit("maxNodes", 2));

            Assert.That(service.Limit("maxNodes"), Is.EqualTo(2));
        }

        [Test]
        public async Task ExpiredRealLicenceIsReportedInsteadOfFallingBackToDemoTest()
        {
            // Found by the harness: a customer whose purchase lapsed was told
            // "the demo period has ended", which hides the expiry date, the
            // customer name and the renewal they actually need.
            var service = await CreateAsync(demo: TimeSpan.FromDays(3650));

            m_store.Save(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-2), expires: NOW.AddDays(-1))));

            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.IsDemo, Is.False);
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.Expired));
                Assert.That(service.State.Describe(), Does.Contain("ACME GmbH"));
            });
        }

        [Test]
        public async Task DemoStillAppliesWhenNothingWasEverInstalledTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            Assert.That(service.State.IsDemo, Is.True);
        }

        [Test]
        public async Task NoDemoConfiguredReportsMissingTest()
        {
            var service = await CreateAsync(demo: null);

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.Missing));
                Assert.That(service.State.CanRun, Is.False);
            });
        }

        #endregion

        #region Install Tests

        [Test]
        public async Task InstallingAValidLicenceReplacesTheDemoTest()
        {
            var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            var result = await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload()));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(service.State.IsDemo, Is.False);
                Assert.That(service.State.CanRun, Is.True);
                Assert.That(service.State.Payload!.Customer!.Name, Is.EqualTo("ACME GmbH"));
            });
        }

        [Test]
        public async Task InstallingAnInvalidLicenceDoesNotDisplaceAWorkingOneTest()
        {
            var service = await CreateAsync(demo: null);
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload()));

            var other = new LicenseTestContext();
            var rejected = await service.InstallAsync(other.Issue(LicenseTestContext.Payload()));

            Assert.Multiple(() =>
            {
                Assert.That(rejected.Status, Is.EqualTo(LicenseStatus.SignatureInvalid));
                Assert.That(service.State.CanRun, Is.True, "A pasted mistake must not cost a working licence.");
                Assert.That(m_store.ReadTokens(), Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task RenewalCanBeInstalledBeforeTheCurrentOneEndsTest()
        {
            // The whole reason the store holds several documents: staging a
            // renewal must not require doing it exactly at expiry.
            var service = await CreateAsync(demo: null);

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddMonths(-11), expires: NOW.AddDays(20))));

            var renewal = await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddDays(20), expires: NOW.AddDays(385))));

            Assert.That(renewal.Status, Is.EqualTo(LicenseStatus.NotYetValid));

            m_clock = NOW.AddDays(25);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.CanRun, Is.True, "The staged renewal must take over with no gap.");
                Assert.That(service.State.Payload!.ExpiresUtc, Is.EqualTo(NOW.AddDays(385)));
            });
        }

        #endregion

        #region Selection Tests

        [Test]
        public async Task BestOfSeveralValidLicencesIsChosenTest()
        {
            var service = await CreateAsync(demo: null);

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddDays(-10), expires: NOW.AddDays(10))));
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddDays(-10), expires: NOW.AddDays(300))));

            Assert.That(service.State.Payload!.ExpiresUtc, Is.EqualTo(NOW.AddDays(300)));
        }

        [Test]
        public async Task UnlimitedBeatsATimeLimitedLicenceTest()
        {
            var service = await CreateAsync(demo: null);

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddDays(-10), expires: NOW.AddDays(300))));
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddDays(-10), unlimited: true)));

            Assert.That(service.State.Payload!.IsUnlimited, Is.True);
        }

        [Test]
        public async Task SupersededLicenceIsRefusedEvenThoughItVerifiesTest()
        {
            var service = await CreateAsync(demo: null);

            var old = LicenseTestContext.Payload(notBefore: NOW.AddDays(-10), unlimited: true);
            await service.InstallAsync(m_context.Issue(old));

            var replacement = LicenseTestContext.Payload(notBefore: NOW.AddDays(-1), expires: NOW.AddDays(30));
            var superseding = new Abstract.LicensePayload
            {
                Id = replacement.Id,
                IssuedUtc = replacement.IssuedUtc,
                Product = replacement.Product,
                Edition = replacement.Edition,
                NotBeforeUtc = replacement.NotBeforeUtc,
                ExpiresUtc = replacement.ExpiresUtc,
                Binding = replacement.Binding,
                Customer = replacement.Customer,
                Supersedes = new[] { old.Id }
            };

            await service.InstallAsync(m_context.Issue(superseding));

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Payload!.Id, Is.EqualTo(superseding.Id));
                Assert.That(service.State.Payload!.IsUnlimited, Is.False,
                    "The unlimited document was explicitly retired; it must not win on being unlimited.");
            });
        }

        [Test]
        public async Task MostActionableFailureIsReportedTest()
        {
            var service = await CreateAsync(demo: null);

            m_store.Save(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-2), expires: NOW.AddDays(-1))));
            m_store.Save(new LicenseTestContext().Issue(LicenseTestContext.Payload()));

            await service.ReloadAsync();

            Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.Expired),
                "'Expired' sends the customer to a renewal; a signature error next to it sends them nowhere.");
        }

        #endregion

        #region Clock Tests

        [Test]
        public async Task ClockRolledBackIsReportedAsTamperedTest()
        {
            var service = await CreateAsync(demo: null);
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload()));

            m_clock = NOW.AddDays(-10);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.Status, Is.EqualTo(LicenseStatus.ClockTampered));
                Assert.That(service.State.CanRun, Is.False);
            });
        }

        [Test]
        public async Task SmallBackwardsCorrectionIsToleratedTest()
        {
            // An NTP correction or a resumed VM snapshot moves a clock backwards
            // legitimately. Firing on those would cost more than it saves.
            var service = await CreateAsync(demo: null);
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload()));

            m_clock = NOW.AddHours(-2);
            await service.ReloadAsync();

            Assert.That(service.State.CanRun, Is.True);
        }

        #endregion

        #region Vocabulary Tests

        [Test]
        public async Task UnknownFeatureIsReportedTest()
        {
            var service = await CreateAsync(demo: null, declares: vocabulary => vocabulary
                .Feature("sso")
                .Limit("maxNodes", @default: 4));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                features: new[] { "sso", "ssoo" })));

            Assert.That(service.State.UnrecognisedKeys, Has.Exactly(1).Contains("ssoo"),
                "A typo in the issuing catalogue must be visible at first install, not three weeks later.");
        }

        [Test]
        public async Task DeclaredDefaultAppliesWhenTheLicenceIsSilentTest()
        {
            var service = await CreateAsync(demo: null, declares: vocabulary => vocabulary.Limit("maxNodes", @default: 4));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload()));

            Assert.That(service.Limit("maxNodes"), Is.EqualTo(4));
        }

        [Test]
        public async Task ProductThatDeclaresNothingReportsNothingTest()
        {
            var service = await CreateAsync(demo: null);

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(features: new[] { "anything" })));

            Assert.That(service.State.UnrecognisedKeys, Is.Empty);
        }

        #endregion

        #region Feature And Limit Tests

        [Test]
        public async Task FeaturesAndLimitsComeFromTheLicenceTest()
        {
            var service = await CreateAsync(demo: null);

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                features: new[] { "sso" },
                limits: new Dictionary<string, long> { ["maxNodes"] = 50 })));

            Assert.Multiple(() =>
            {
                Assert.That(service.HasFeature("sso"), Is.True);
                Assert.That(service.HasFeature("SSO"), Is.True, "Feature lookup must not be case-sensitive.");
                Assert.That(service.HasFeature("accounting"), Is.False);
                Assert.That(service.Limit("maxNodes"), Is.EqualTo(50));
            });
        }

        [Test]
        public async Task AnInvalidLicenceGrantsNothingTest()
        {
            var service = await CreateAsync(demo: null);

            m_store.Save(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-2), expires: NOW.AddDays(-1), features: new[] { "sso" })));

            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.State.CanRun, Is.False);
                Assert.That(service.HasFeature("sso"), Is.False,
                    "An expired licence must not keep granting what it used to.");
            });
        }

        #endregion

        #region Request Tests

        [Test]
        public async Task RequestCarriesFactorsAndFingerprintTest()
        {
            var service = await CreateAsync(demo: null,
                binding: new LicenseBindingProviderTenant("acme-prod", "install-42"));

            var request = await service.CreateRequestAsync(contact: "it@acme.example");

            Assert.Multiple(() =>
            {
                Assert.That(request.Product, Is.EqualTo(LicenseTestContext.PRODUCT));
                Assert.That(request.Factors, Has.Count.EqualTo(2));
                Assert.That(request.Fingerprint, Is.Not.Empty);
                Assert.That(request.Contact, Is.EqualTo("it@acme.example"));
                Assert.That(request.SuggestedFileName(), Does.EndWith(".owlreq"));
            });
        }

        [Test]
        public async Task RequestSurvivesARoundTripTest()
        {
            var service = await CreateAsync(demo: null, binding: new LicenseBindingProviderTenant("acme-prod"));

            var original = await service.CreateRequestAsync(contact: "it@acme.example", notes: "PO-2026-0417");
            var restored = Requests.LicenseRequest.FromJson(original.ToJson());

            Assert.That(original.Is(restored!), Is.True);
        }

        #endregion

        #region Tools

        private async Task<LicenseService> CreateAsync(
            TimeSpan? demo,
            Action<Demo.LicenseDemoOptions>? configureDemo = null,
            Action<Vocabulary.LicenseVocabulary>? declares = null,
            ILicenseBindingProvider? binding = null)
        {
            var options = new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(m_store)
                .WithBinding(binding ?? new LicenseBindingProviderNone())
                .WithClock(() => m_clock);

            if (demo != null)
                options.WithDemo(demo.Value, configureDemo);

            if (declares != null)
                options.Declares(declares);

            var service = new LicenseService(options);
            await service.ReloadAsync();

            return service;
        }

        #endregion
    }
}
