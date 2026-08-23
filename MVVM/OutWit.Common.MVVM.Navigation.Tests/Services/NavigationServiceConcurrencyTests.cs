using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class NavigationServiceConcurrencyTests
    {
        #region Constants

        private const string FIRST = "first";
        private const string SECOND = "second";
        private const string THIRD = "third";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;
        private CallLog m_log = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<AwareViewModel>(FIRST);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
                nav.AddRoute<AwareThirdViewModel>(THIRD);
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_log = m_provider.GetRequiredService<CallLog>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Preemption Tests

        [Test]
        public async Task NewerNavigationDisplacesOlderOneBeforePointOfNoReturnTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;

            // the second navigation will stall at its own CanNavigateTo — before the point of no return
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var toSecond = NavigateWithStalledTargetAsync(SECOND, gate);
            Assert.That(toSecond.IsCompleted, Is.False);

            var toThird = m_navigation.NavigateAsync(THIRD);
            gate.SetResult(true);

            var secondResult = await toSecond;
            var thirdResult = await toThird;

            Assert.That(secondResult.Status, Is.EqualTo(NavigationStatus.Cancelled));
            Assert.That(thirdResult.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareThirdViewModel>());
            Assert.That(m_log.Count("AwareSecond.To"), Is.EqualTo(0));
            Assert.That(first.NavigatedFromCount, Is.EqualTo(1));
        }

        [Test]
        public async Task SlowScreenDoesNotBlockTheNextNavigationTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;

            // the second navigation commits and then stalls in OnNavigatedTo — the outlet is
            // already showing it, and its slot is free
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var toSecond = NavigateWithStalledOnNavigatedToAsync(SECOND, gate);
            Assert.That(toSecond.IsCompleted, Is.False);
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareSecondViewModel>());

            // a third navigation does not wait for the second screen to finish loading
            var thirdResult = await m_navigation.NavigateAsync(THIRD);

            Assert.That(thirdResult.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareThirdViewModel>());

            // and the screen that was left behind had its loading cancelled
            gate.SetResult(true);
            var secondResult = await toSecond;

            Assert.That(secondResult.Status, Is.EqualTo(NavigationStatus.Cancelled));
            Assert.That(first.NavigatedFromCount, Is.EqualTo(1));
            Assert.That(m_log.Count("AwareSecond.From"), Is.EqualTo(1));
        }

        [Test]
        public async Task NavigatedIsRaisedAtCommitNotAfterLoadingTest()
        {
            var raised = 0;
            m_navigation.Navigated += (_, _) => raised++;

            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = NavigateWithStalledOnNavigatedToAsync(FIRST, gate);

            Assert.That(raised, Is.EqualTo(1));
            Assert.That(pending.IsCompleted, Is.False);

            gate.SetResult(true);
            await pending;

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public async Task IsNavigatingIsTrueWhileNavigationIsInFlightTest()
        {
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var outlet = m_navigation.Outlet();
            Assert.That(outlet.IsNavigating, Is.False);

            var pending = NavigateWithStalledOnNavigatedToAsync(FIRST, gate);

            Assert.That(outlet.IsNavigating, Is.True);

            gate.SetResult(true);
            await pending;

            Assert.That(outlet.IsNavigating, Is.False);
        }

        #endregion

        #region Cancellation Tests

        [Test]
        public async Task CancellationBeforePointOfNoReturnLeavesContentTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = m_navigation.Outlet().Content;

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var result = await m_navigation.NavigateAsync(SECOND, cancellation: cancellation.Token);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Cancelled));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(m_log.Count("AwareSecond.Created"), Is.EqualTo(0));
        }

        [Test]
        public async Task CancellationWhileTargetGuardRunsLeavesContentTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = m_navigation.Outlet().Content;

            using var cancellation = new CancellationTokenSource();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var pending = NavigateWithStalledTargetAsync(SECOND, gate, cancellation.Token);
            cancellation.Cancel();
            gate.SetResult(true);

            var result = await pending;

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Cancelled));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(m_log.Count("AwareSecond.To"), Is.EqualTo(0));
        }

        [Test]
        public async Task CancellationAfterPointOfNoReturnCommitsAndReturnsCancelledTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var navigated = 0;
            m_navigation.Navigated += (_, _) => navigated++;

            using var cancellation = new CancellationTokenSource();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            var pending = NavigateWithStalledOnNavigatedToAsync(SECOND, gate, cancellation.Token);
            cancellation.Cancel();
            gate.SetResult(true);

            var result = await pending;

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Cancelled));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareSecondViewModel>());
            Assert.That(navigated, Is.EqualTo(1));
        }

        #endregion

        #region Dispatcher Tests

        [Test]
        public async Task NavigateFromForeignThreadHopsThroughDispatcherOnceTest()
        {
            var dispatcher = new CountingDispatcher { HasAccess = false };
            var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>(FIRST), dispatcher: dispatcher);
            var navigation = provider.GetRequiredService<INavigationService>();

            var result = await navigation.NavigateAsync(FIRST);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(dispatcher.Invocations, Is.EqualTo(1));
            Assert.That(navigation.Outlet().Content, Is.InstanceOf<PlainViewModel>());
        }

        [Test]
        public async Task NavigateFromUiThreadDoesNotHopTest()
        {
            var dispatcher = new CountingDispatcher { HasAccess = true };
            var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>(FIRST), dispatcher: dispatcher);
            var navigation = provider.GetRequiredService<INavigationService>();

            await navigation.NavigateAsync(FIRST);

            Assert.That(dispatcher.Invocations, Is.EqualTo(0));
        }

        #endregion

        #region Tools

        private Task<NavigationResult> NavigateWithStalledTargetAsync(string route, TaskCompletionSource<bool> gate, CancellationToken cancellation = default)
        {
            // the target is created by the navigation itself, so the gate is planted the
            // moment the instance appears in the log — AwareViewModel reads it on CanNavigateTo
            StalledTargetGate.Install(m_provider, gate, stallOnNavigatedTo: false);
            return m_navigation.NavigateAsync(route, cancellation: cancellation);
        }

        private Task<NavigationResult> NavigateWithStalledOnNavigatedToAsync(string route, TaskCompletionSource<bool> gate, CancellationToken cancellation = default)
        {
            StalledTargetGate.Install(m_provider, gate, stallOnNavigatedTo: true);
            return m_navigation.NavigateAsync(route, cancellation: cancellation);
        }

        #endregion
    }
}
