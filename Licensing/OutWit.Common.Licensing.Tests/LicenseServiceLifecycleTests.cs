using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// What a long-lived host needs from the service and never had: something to
    /// subscribe to, a re-evaluation nobody has to ask for, and a way to take a
    /// licence back off.
    /// </summary>
    [TestFixture]
    public sealed class LicenseServiceLifecycleTests
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

        #region State Changed Tests

        [Test]
        public async Task InstallingALicenceRaisesStateChangedTest()
        {
            using var service = await CreateAsync();

            var raised = new List<LicenseState>();
            service.StateChanged += (_, state) => raised.Add(state);

            await service.InstallAsync(Issue(expires: NOW.AddYears(1)));

            Assert.Multiple(() =>
            {
                Assert.That(raised, Has.Count.EqualTo(1));
                Assert.That(raised[0].Mode, Is.EqualTo(LicenseMode.Licensed));
            });
        }

        [Test]
        public async Task ReloadThatChangesNothingIsSilentTest()
        {
            // A periodic reload that finds nothing new must not make a banner
            // redraw, or the event stops meaning anything and every consumer
            // starts filtering it for themselves.
            using var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddYears(1)));

            var raised = 0;
            service.StateChanged += (_, _) => raised++;

            await service.ReloadAsync();
            await service.ReloadAsync();

            Assert.That(raised, Is.Zero);
        }

        [Test]
        public async Task CrossingExpiryRaisesStateChangedTest()
        {
            using var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            LicenseState? seen = null;
            service.StateChanged += (_, state) => seen = state;

            m_clock = NOW.AddDays(2);
            await service.ReloadAsync();

            Assert.Multiple(() =>
            {
                Assert.That(seen, Is.Not.Null);
                Assert.That(seen!.Mode, Is.EqualTo(LicenseMode.Restricted));
            });
        }

        [Test]
        public async Task ADayPassingOnADemoRaisesStateChangedTest()
        {
            // The count on the banner is the thing that goes stale in a session
            // that runs for weeks, so it is part of what "changed" means.
            using var service = await CreateAsync(demo: TimeSpan.FromDays(30));

            var raised = 0;
            service.StateChanged += (_, _) => raised++;

            m_clock = NOW.AddDays(1);
            await service.ReloadAsync();

            Assert.That(raised, Is.EqualTo(1));
        }

        #endregion

        #region Periodic Reload Tests

        [Test]
        public async Task PeriodicReloadNoticesExpiryWithNobodyAskingTest()
        {
            // The Inventor case: a session runs for days and a draughtsman does
            // not restart their CAD host, so nothing would ever call ReloadAsync.
            var options = Options().WithPeriodicReload(TimeSpan.FromMilliseconds(50));

            using var service = new LicenseService(options);
            await service.ReloadAsync();
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Licensed));

            var expired = new TaskCompletionSource<LicenseState>();
            service.StateChanged += (_, state) => expired.TrySetResult(state);

            m_clock = NOW.AddDays(2);

            var finished = await Task.WhenAny(expired.Task, Task.Delay(TimeSpan.FromSeconds(10)));

            Assert.Multiple(() =>
            {
                Assert.That(finished, Is.SameAs(expired.Task), "The timer never fired.");
                Assert.That(expired.Task.Result.Mode, Is.EqualTo(LicenseMode.Restricted));
            });
        }

        [Test]
        public async Task NoTimerIsStartedUnlessAskedForTest()
        {
            // Off by default: a timer nobody asked for is a surprise in a
            // short-lived process, and a licence check on a schedule is a
            // decision the product makes, not the library.
            using var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddDays(1)));

            var raised = 0;
            service.StateChanged += (_, _) => raised++;

            m_clock = NOW.AddDays(2);
            await Task.Delay(200);

            Assert.That(raised, Is.Zero);
        }

        #endregion

        #region Remove Tests

        [Test]
        public async Task RemovingTheLicenceInForceReEvaluatesTest()
        {
            using var service = await CreateAsync();
            await service.InstallAsync(Issue(expires: NOW.AddYears(1)));

            var removed = await service.RemoveAsync(service.State.Payload!.Id);

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(service.State.Mode, Is.EqualTo(LicenseMode.Restricted));
                Assert.That(service.Snapshot.Installed, Is.Empty);
            });
        }

        [Test]
        public async Task RemovingASupersededDocumentLeavesTheLiveOneAloneTest()
        {
            // The only way to test a document being removed rather than
            // replaced, and the reason uninstall exists at all.
            using var service = await CreateAsync();

            var old = LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddDays(10));
            var current = LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: NOW.AddYears(1));

            await service.InstallAsync(m_context.Issue(old));
            await service.InstallAsync(m_context.Issue(current));

            var removed = await service.RemoveAsync(old.Id);

            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(service.State.Payload!.Id, Is.EqualTo(current.Id));
                Assert.That(service.Snapshot.Installed, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task RemovingSomethingThatIsNotThereIsFalseTest()
        {
            using var service = await CreateAsync();

            Assert.That(await service.RemoveAsync("no-such-licence"), Is.False);
            Assert.That(await service.RemoveAsync(string.Empty), Is.False);
        }

        #endregion

        #region Tools

        private string Issue(DateTime? expires)
        {
            return m_context.Issue(LicenseTestContext.Payload(notBefore: NOW.AddYears(-1), expires: expires));
        }

        private LicensingOptions Options()
        {
            return new LicensingOptions()
                .ForProduct(LicenseTestContext.PRODUCT, new Version(1, 5, 0))
                .WithKeyRing(m_context.Ring())
                .WithStore(m_store)
                .WithBinding(new LicenseBindingProviderNone())
                .WithClock(() => m_clock);
        }

        private async Task<LicenseService> CreateAsync(TimeSpan? demo = null)
        {
            var options = Options();

            if (demo != null)
                options.WithDemo(demo.Value);

            var service = new LicenseService(options);
            await service.ReloadAsync();

            return service;
        }

        #endregion
    }
}
