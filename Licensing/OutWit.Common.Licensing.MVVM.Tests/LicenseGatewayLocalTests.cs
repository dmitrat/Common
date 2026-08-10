using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Issuing;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.MVVM.ViewModels;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.MVVM.Tests
{
    /// <summary>
    /// The panel over a real licensing runtime, with real signed licences.
    /// <para>
    /// The mock proves the abstraction is honest; this proves it actually fits
    /// the library underneath it. Both are needed, and the second is the one
    /// that would have caught a gateway that quietly required something the
    /// service does not offer.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class LicenseGatewayLocalTests
    {
        private const string PRODUCT = "TestProduct";
        private const string KEY_ID = "test-key-1";

        private static readonly DateTime NOW = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        private string m_privateKey = null!;
        private LicenseKeyRing m_ring = null!;
        private LicenseStoreMemory m_store = null!;
        private DateTime m_clock;

        [SetUp]
        public void SetUp()
        {
            var (publicPem, privatePem) = LicenseSigner.GenerateKeyPair(LicenseAlgorithm.ES256);

            m_privateKey = privatePem;
            m_ring = new LicenseKeyRing(new[]
            {
                new LicenseKeyInfo
                {
                    KeyId = KEY_ID,
                    Algorithm = LicenseAlgorithm.ES256,
                    PublicKeyPem = publicPem,
                    Policy = LicenseKeyPolicy.Commercial,
                    Products = new[] { PRODUCT }
                }
            });

            m_store = new LicenseStoreMemory();
            m_clock = NOW;
        }

        #region Gateway Tests

        [Test]
        public void GatewayReportsTheSettledStateImmediatelyTest()
        {
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);

            Assert.That(gateway.Current.Mode, Is.EqualTo(LicenseMode.Restricted));
        }

        [Test]
        public async Task InstallingThroughTheGatewayLicensesTheProductTest()
        {
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);

            var outcome = await gateway.InstallAsync(Issue(expires: NOW.AddYears(1)));

            Assert.Multiple(() =>
            {
                Assert.That(outcome.IsAccepted, Is.True);
                Assert.That(outcome.Status, Is.EqualTo(LicenseStatus.Valid));
                Assert.That(outcome.Snapshot!.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(gateway.Current.Mode, Is.EqualTo(LicenseMode.Licensed));
            });
        }

        [Test]
        public async Task AStagedRenewalCountsAsAcceptedTest()
        {
            // Installing a renewal ahead of its start date is the intended way
            // to renew without an outage. A panel that called it a failure would
            // train people not to do it.
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);

            var outcome = await gateway.InstallAsync(Issue(notBefore: NOW.AddDays(30), expires: NOW.AddYears(1)));

            Assert.Multiple(() =>
            {
                Assert.That(outcome.IsAccepted, Is.True);
                Assert.That(outcome.Status, Is.EqualTo(LicenseStatus.NotYetValid));
            });
        }

        [Test]
        public async Task ARefusedLicenceIsNotStoredTest()
        {
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);

            var outcome = await gateway.InstallAsync("this is not a licence");

            Assert.Multiple(() =>
            {
                Assert.That(outcome.IsAccepted, Is.False);
                Assert.That(outcome.Status, Is.EqualTo(LicenseStatus.Malformed));
                Assert.That(gateway.Current.Installed, Is.Empty);
            });
        }

        [Test]
        public async Task ServiceStateChangeReachesTheGatewayTest()
        {
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);

            await gateway.InstallAsync(Issue(expires: NOW.AddDays(1)));

            LicenseSnapshot? seen = null;
            gateway.SnapshotChanged += (_, snapshot) => seen = snapshot;

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(seen, Is.Not.Null);
                Assert.That(seen!.Mode, Is.EqualTo(LicenseMode.Restricted));
            });
        }

        [Test]
        public async Task DisposedGatewayStopsListeningTest()
        {
            using var service = Service();
            var gateway = new LicenseGatewayLocal(service);

            await gateway.InstallAsync(Issue(expires: NOW.AddDays(1)));

            var raised = 0;
            gateway.SnapshotChanged += (_, _) => raised++;

            gateway.Dispose();

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.That(raised, Is.Zero);
        }

        #endregion

        #region Panel Tests

        [Test]
        public async Task PanelDrivesTheWholeLoopOverARealServiceTest()
        {
            using var service = Service(demo: TimeSpan.FromDays(30), declares: true);
            using var gateway = new LicenseGatewayLocal(service);
            using var panel = new LicensePanelViewModelStandalone(gateway);

            Assert.Multiple(() =>
            {
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Demo));
                Assert.That(panel.Severity, Is.EqualTo(LicenseSeverity.Info));
                Assert.That(panel.Fingerprint, Is.Not.Empty);
            });

            panel.PastedToken = Issue(expires: NOW.AddYears(1), features: new[] { "sso" });
            Assert.That(panel.CanInstall, Is.True);

            await panel.InstallAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Licensed));
                Assert.That(panel.Customer, Is.EqualTo("ACME GmbH"));
                Assert.That(panel.PastedToken, Is.Empty);
                Assert.That(panel.Installed, Has.Count.EqualTo(1));
                Assert.That(panel.Grants, Has.Count.EqualTo(2));
            });

            panel.SelectedDocument = panel.Installed[0];
            await panel.RemoveAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.Installed, Is.Empty);
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Demo),
                    "Taking the licence off must fall back to the demo, not to nothing.");
            });
        }

        [Test]
        public async Task PanelReportsTheRequestItBuiltTest()
        {
            using var service = Service();
            using var gateway = new LicenseGatewayLocal(service);
            using var panel = new LicensePanelViewModelStandalone(gateway);

            await panel.CreateRequestAsync();

            Assert.Multiple(() =>
            {
                Assert.That(panel.RequestBlob, Does.Contain(PRODUCT));
                Assert.That(panel.RequestBlob, Does.Contain(panel.Fingerprint));
            });
        }

        #endregion

        #region Tools

        private LicenseService Service(TimeSpan? demo = null, bool declares = false)
        {
            var options = new LicensingOptions()
                .ForProduct(PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_ring)
                .WithStore(m_store)
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => m_clock);

            if (demo != null)
                options.WithDemo(demo.Value);

            if (declares)
                options.Declares(vocabulary => vocabulary
                    .Feature("sso", "Single sign-on")
                    .Limit("maxNodes", "Compute nodes", 4));

            var service = new LicenseService(options);
            service.ReloadAsync().GetAwaiter().GetResult();

            return service;
        }

        private string Issue(DateTime? notBefore = null, DateTime? expires = null, IReadOnlyList<string>? features = null)
        {
            var start = notBefore ?? NOW.AddYears(-1);

            var payload = new LicensePayload
            {
                Id = Guid.NewGuid().ToString("N"),
                Product = PRODUCT,
                Edition = "Enterprise",
                IssuedUtc = start,
                NotBeforeUtc = start,
                ExpiresUtc = expires,
                Binding = LicenseBinding.None(),
                Features = features ?? Array.Empty<string>(),
                Customer = new LicenseCustomer { Id = "acme", Name = "ACME GmbH" }
            };

            return LicenseIssuer.Issue(payload, KEY_ID, LicenseAlgorithm.ES256, m_privateKey);
        }

        #endregion
    }
}
