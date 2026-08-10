using System;
using System.Threading;
using System.Threading.Tasks;
using OutWit.Common.Licensing.MVVM.Tests.Mock;
using OutWit.Common.Licensing.MVVM.ViewModels;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Storage;

namespace OutWit.Common.Licensing.MVVM.Tests
{
    /// <summary>
    /// Which thread the panel updates itself on.
    /// <para>
    /// Found by running the harness rather than by reading the code, and worth
    /// recording in full because the reasoning that hid it is plausible. A panel
    /// awaiting the gateway does come back to the thread that asked — so the
    /// command path looks safe. But <c>ILicenseService.StateChanged</c> is not
    /// awaited by anybody: the runtime raises it from wherever its own
    /// re-evaluation happened to finish, and every await inside the library
    /// suppresses the synchronization context by design. So the snapshot arrives
    /// on a thread-pool thread, and it arrives that way on <b>every install</b>,
    /// not only when a periodic reload is switched on.
    /// </para>
    /// <para>
    /// The visible symptom was a licence that installed correctly and a screen
    /// that showed half of it: four properties updated, then the collections
    /// faulted, and the notification layer's reflection re-wrapped the whole
    /// thing as "Exception has been thrown by the target of an invocation".
    /// </para>
    /// <para>
    /// Conclusion: for any consumer with a UI thread the dispatcher seam is
    /// <b>required</b>, not decorative.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class ThreadAffinityTests
    {
        private LicenseGatewayMock m_gateway = null!;

        [SetUp]
        public void SetUp()
        {
            m_gateway = new LicenseGatewayMock(LicenseSnapshot.Empty());
        }

        #region Push Tests

        [Test]
        public async Task SnapshotPushedFromAnotherThreadIsMarshalledTest()
        {
            var dispatcher = new LicenseDispatcherMock();

            using var panel = new LicensePanelViewModelStandalone(m_gateway, dispatcher: dispatcher);

            await Task.Run(() => m_gateway.Push(Licensed()));

            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.Marshalled, Is.EqualTo(1),
                    "The push was applied directly; on a real UI thread that is the fault.");
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Licensed));
            });
        }

        [Test]
        public async Task WithoutADispatcherThePushLandsWhereverItArrivedTest()
        {
            // Not a bug in the panel — it is the honest consequence of not
            // supplying the seam, and it is what a Blazor or service consumer
            // with no UI thread actually wants. It is recorded here so that the
            // requirement on a desktop consumer is a documented fact rather than
            // something rediscovered by watching a window fail.
            using var panel = new LicensePanelViewModelStandalone(m_gateway);

            var appliedOn = 0;
            panel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(panel.Mode))
                    appliedOn = Thread.CurrentThread.ManagedThreadId;
            };

            var owningThread = Thread.CurrentThread.ManagedThreadId;

            await Task.Run(() => m_gateway.Push(Licensed()));

            Assert.That(appliedOn, Is.Not.EqualTo(owningThread));
        }

        [Test]
        public void WorkAlreadyOnTheRightThreadIsNotMarshalledTest()
        {
            var dispatcher = new LicenseDispatcherMock();

            using var panel = new LicensePanelViewModelStandalone(m_gateway, dispatcher: dispatcher);

            m_gateway.Push(Licensed());

            Assert.Multiple(() =>
            {
                Assert.That(dispatcher.Marshalled, Is.Zero);
                Assert.That(panel.Mode, Is.EqualTo(LicenseMode.Licensed));
            });
        }

        #endregion

        #region Service Tests

        [Test]
        public async Task TheRuntimeReallyDoesRaiseFromAPoolThreadTest()
        {
            // The premise of this whole fixture, asserted rather than assumed:
            // a reload started on this thread finishes somewhere else, and the
            // event goes out from there.
            var options = new LicensingOptions()
                .ForProduct("TestProduct", new Version(1, 0))
                .WithBinding(new LicenseBindingProviderSlow())
                .WithStore(new LicenseStoreMemory())
                .WithDemo(TimeSpan.FromDays(30));

            using var service = new LicenseService(options);

            var raisedOn = 0;
            service.StateChanged += (_, _) => raisedOn = Thread.CurrentThread.ManagedThreadId;

            // Read before the await, not after: with no synchronization context
            // the test itself resumes on whichever pool thread finished the
            // reload, so comparing afterwards compares a thread with itself.
            var startedOn = Thread.CurrentThread.ManagedThreadId;

            await service.ReloadAsync();

            Assert.That(raisedOn, Is.Not.Zero.And.Not.EqualTo(startedOn));
        }

        #endregion

        #region Tools

        private static LicenseSnapshot Licensed()
        {
            return new LicenseSnapshot
            {
                Mode = LicenseMode.Licensed,
                Status = Validation.LicenseStatus.Valid,
                Description = "Licensed to ACME GmbH.",
                CanRun = true,
                DaysRemaining = 365
            };
        }

        #endregion
    }
}
