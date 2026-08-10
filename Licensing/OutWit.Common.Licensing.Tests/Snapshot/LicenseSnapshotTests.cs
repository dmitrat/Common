using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.Tests.Snapshot
{
    /// <summary>
    /// The projection three codebases will bind to instead of reaching into
    /// <c>State.Payload</c> for themselves.
    /// </summary>
    [TestFixture]
    public sealed class LicenseSnapshotTests
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

        #region Projection Tests

        [Test]
        public async Task SnapshotCarriesWhatAPanelShowsTest()
        {
            var service = await CreateAsync();
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1),
                expires: NOW.AddDays(90),
                edition: "Enterprise")));

            var snapshot = service.Snapshot;

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(snapshot.Status, Is.EqualTo(LicenseStatus.Valid));
                Assert.That(snapshot.Edition, Is.EqualTo("Enterprise"));
                Assert.That(snapshot.CustomerName, Is.EqualTo("ACME GmbH"));
                Assert.That(snapshot.Product, Is.EqualTo(LicenseTestContext.PRODUCT));
                Assert.That(snapshot.ProductVersion, Is.EqualTo("1.5.0"));
                Assert.That(snapshot.DaysRemaining, Is.EqualTo(90));
                Assert.That(snapshot.CanRun, Is.True);
                Assert.That(snapshot.LicenseId, Is.Not.Empty);
                Assert.That(snapshot.Fingerprint, Is.Not.Empty);
                Assert.That(snapshot.Description, Does.Contain("ACME GmbH"));
                Assert.That(snapshot.GracePolicy, Is.Not.Empty);
            });
        }

        [Test]
        public async Task SnapshotIsRebuiltWithTheStateTest()
        {
            var service = await CreateAsync();
            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1), expires: NOW.AddDays(1))));

            Assert.That(service.Snapshot.Mode, Is.EqualTo(LicenseMode.Licensed));

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.That(service.Snapshot.Mode, Is.EqualTo(LicenseMode.Restricted),
                "A snapshot that lagged its state would be a panel showing the previous answer.");
        }

        #endregion

        #region Grant Tests

        [Test]
        public async Task EveryDeclaredKeyGetsALineWhetherGrantedOrNotTest()
        {
            // A capability the customer paid for and did not get has to be
            // visible as a "no" beside its own description. Listing only what
            // the licence carries would render that case as an empty space.
            var service = await CreateAsync(declares: vocabulary => vocabulary
                .Feature("format.nas", "Nastran decks")
                .Feature("integration.prepomax", "Open in PrePoMax")
                .Limit("maxVariants", "Variants per run", 64));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1),
                expires: NOW.AddYears(1),
                features: new[] { "format.nas" },
                limits: new Dictionary<string, long> { ["maxVariants"] = 8 })));

            var grants = service.Snapshot.Grants;

            Assert.Multiple(() =>
            {
                Assert.That(grants, Has.Count.EqualTo(3));

                var nastran = grants.Single(grant => grant.Key == "format.nas");
                Assert.That(nastran.IsGranted, Is.True);
                Assert.That(nastran.Description, Is.EqualTo("Nastran decks"));

                var prepomax = grants.Single(grant => grant.Key == "integration.prepomax");
                Assert.That(prepomax.IsGranted, Is.False);
                Assert.That(prepomax.DisplayValue, Is.EqualTo("no"));

                var variants = grants.Single(grant => grant.Key == "maxVariants");
                Assert.That(variants.Kind, Is.EqualTo(LicenseGrantKind.Limit));
                Assert.That(variants.Value, Is.EqualTo(8));
            });
        }

        [Test]
        public async Task GrantsAgreeWithTheServiceThatGatesOnThemTest()
        {
            // The snapshot reads through the service's own accessors on purpose.
            // A second reading of the payload would be free to disagree, and the
            // disagreement would show up as a panel promising something the
            // product refuses to do.
            var service = await CreateAsync(declares: vocabulary => vocabulary
                .Feature("sso")
                .Limit("maxNodes", @default: 4));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1),
                expires: NOW.AddDays(1),
                features: new[] { "sso" },
                limits: new Dictionary<string, long> { ["maxNodes"] = 50 })));

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(service.HasFeature("sso"), Is.False);
                Assert.That(service.Snapshot.HasFeature("sso"), Is.False);
                Assert.That(service.Limit("maxNodes"), Is.EqualTo(4));
                Assert.That(service.Snapshot.Limit("maxNodes"), Is.EqualTo(4));
            });
        }

        [Test]
        public async Task UnsetLimitReadsAsUnlimitedTest()
        {
            var service = await CreateAsync(declares: vocabulary => vocabulary.Limit("maxNodes"));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1))));

            var grant = service.Snapshot.Grants.Single();

            Assert.Multiple(() =>
            {
                Assert.That(grant.IsUnlimited, Is.True);
                Assert.That(grant.DisplayValue, Is.EqualTo(LicenseGrant.UNLIMITED));
            });
        }

        #endregion

        #region Installed Tests

        [Test]
        public async Task InstalledListsEveryDocumentAndMarksTheEffectiveOneTest()
        {
            // A panel that showed only the effective licence would hide the
            // staged renewal a customer has just installed and is waiting to see.
            var service = await CreateAsync();

            var current = LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddDays(30));
            var staged = LicenseTestContext.Payload(notBefore: NOW.AddDays(30), expires: NOW.AddYears(1));

            await service.InstallAsync(m_context.Issue(current));
            await service.InstallAsync(m_context.Issue(staged));

            var installed = service.Snapshot.Installed;

            Assert.Multiple(() =>
            {
                Assert.That(installed, Has.Count.EqualTo(2));
                Assert.That(installed.Single(document => document.IsEffective).Id, Is.EqualTo(current.Id));
                Assert.That(installed.Single(document => document.Id == staged.Id).Status,
                    Is.EqualTo(LicenseStatus.NotYetValid));
                Assert.That(installed.All(document => document.KeyId == LicenseTestContext.KEY_ID), Is.True,
                    "The signing key id is what diagnoses a ring that does not carry it.");
            });
        }

        [Test]
        public async Task UnreadableDocumentIsListedRatherThanSwallowedTest()
        {
            var service = await CreateAsync();

            m_store.Save(m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1))));
            await service.ReloadAsync();

            Assert.That(service.Snapshot.Installed, Has.Count.EqualTo(1));
        }

        #endregion

        #region Model Tests

        [Test]
        public async Task SnapshotClonesToAnEqualValueTest()
        {
            var service = await CreateAsync(declares: vocabulary => vocabulary
                .Feature("sso")
                .Limit("maxNodes", @default: 4));

            await service.InstallAsync(m_context.Issue(LicenseTestContext.Payload(
                notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1), features: new[] { "sso" })));

            var snapshot = service.Snapshot;

            Assert.That(snapshot.Is(snapshot.Clone()), Is.True);
        }

        [Test]
        public void EmptySnapshotIsRestrictedTest()
        {
            var snapshot = LicenseSnapshot.Empty();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(snapshot.CanRun, Is.False);
                Assert.That(snapshot.Grants, Is.Empty);
            });
        }

        #endregion

        #region Tools

        private async Task<LicenseService> CreateAsync(Action<Vocabulary.LicenseVocabulary>? declares = null)
        {
            var options = new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(m_store)
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => m_clock);

            if (declares != null)
                options.Declares(declares);

            var service = new LicenseService(options);
            await service.ReloadAsync();

            return service;
        }

        #endregion
    }
}
