using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    /// <summary>
    /// Navigating from inside a navigation. Redirecting from OnNavigatedToAsync is a real
    /// pattern ("this screen decided you belong elsewhere") and must work; navigating from a
    /// guard cannot work — the guard holds the outlet — and must fail loudly rather than hang.
    /// Every test here is time-boxed: a regression would be a deadlock, not a wrong value.
    /// </summary>
    [TestFixture]
    public class NavigationServiceReentrancyTests
    {
        #region Constants

        private const string REDIRECTING = "redirecting";
        private const string GUARD = "guard";
        private const string FIRST = "first";
        private const string SECOND = "second";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddOutlet("Side");
                nav.AddRoute<RedirectingViewModel>(REDIRECTING);
                nav.AddRoute<ReentrantGuardViewModel>(GUARD);
                nav.AddRoute<AwareViewModel>(FIRST);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            RedirectingViewModel.Reset(m_navigation);
            ReentrantGuardViewModel.Reset(m_navigation);
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Redirect Tests

        [Test, CancelAfter(10000)]
        public async Task AwaitedRedirectFromOnNavigatedToReachesTheTargetTest()
        {
            RedirectingViewModel.RedirectTo = FIRST;

            var result = await m_navigation.NavigateAsync(REDIRECTING);

            // the redirect succeeds and the outlet ends up on its target; the navigation that
            // redirected reports Cancelled, because by the time it finished the outlet had
            // moved on — the same answer any superseded navigation gives
            Assert.That(RedirectingViewModel.InnerResult!.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareViewModel>());
            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Cancelled));
        }

        [Test, CancelAfter(10000)]
        public async Task RedirectIntoAnotherOutletWorksTest()
        {
            RedirectingViewModel.RedirectTo = FIRST;
            RedirectingViewModel.RedirectOutlet = "Side";

            var result = await m_navigation.NavigateAsync(REDIRECTING);

            // a different outlet, so nothing supersedes this navigation
            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<RedirectingViewModel>());
            Assert.That(m_navigation.Outlet("Side").Content, Is.InstanceOf<AwareViewModel>());
        }

        [Test, CancelAfter(10000)]
        public async Task SelfRedirectTerminatesInsteadOfLoopingTest()
        {
            // a screen that redirects to itself: the second request is Unchanged, so the chain
            // stops instead of recursing until the stack gives out
            RedirectingViewModel.RedirectTo = REDIRECTING;
            RedirectingViewModel.NextRedirectTo = REDIRECTING;

            var result = await m_navigation.NavigateAsync(REDIRECTING);

            Assert.That(RedirectingViewModel.InnerResult!.Status, Is.EqualTo(NavigationStatus.Unchanged));
            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<RedirectingViewModel>());
        }

        #endregion

        #region Guard Tests

        [Test, CancelAfter(10000)]
        public async Task NavigatingFromAGuardOfTheSameOutletFailsInsteadOfHangingTest()
        {
            await m_navigation.NavigateAsync(GUARD);
            ReentrantGuardViewModel.NavigateTo = FIRST;

            var result = await m_navigation.NavigateAsync(SECOND);

            Assert.That(ReentrantGuardViewModel.InnerResult!.Status, Is.EqualTo(NavigationStatus.Failed));
            Assert.That(ReentrantGuardViewModel.InnerResult.Error, Is.InstanceOf<System.InvalidOperationException>());
            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareSecondViewModel>());
        }

        [Test, CancelAfter(10000)]
        public async Task NavigatingFromAGuardIntoAnotherOutletWorksTest()
        {
            await m_navigation.NavigateAsync(GUARD);
            ReentrantGuardViewModel.NavigateTo = FIRST;
            ReentrantGuardViewModel.NavigateOutlet = "Side";

            var result = await m_navigation.NavigateAsync(SECOND);

            Assert.That(ReentrantGuardViewModel.InnerResult!.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet("Side").Content, Is.InstanceOf<AwareViewModel>());
        }

        [Test, CancelAfter(10000)]
        public async Task ParallelNavigationsIntoDifferentOutletsDoNotSeeEachOthersGateTest()
        {
            var first = m_navigation.NavigateAsync(FIRST);
            var second = m_navigation.NavigateAsync(SECOND, outlet: "Side");

            await Task.WhenAll(first, second);

            Assert.That(first.Result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(second.Result.Status, Is.EqualTo(NavigationStatus.Success));
        }

        #endregion

        #region Classes

        /// <summary>
        /// Redirects from OnNavigatedToAsync and awaits the result — the pattern that used to
        /// deadlock while the outlet was held for the whole of OnNavigatedTo.
        /// </summary>
        public sealed class RedirectingViewModel : INavigationAware
        {
            public static void Reset(INavigationService navigation)
            {
                Navigation = navigation;
                RedirectTo = null;
                NextRedirectTo = null;
                RedirectOutlet = null;
                InnerResult = null;
            }

            public static INavigationService? Navigation { get; private set; }

            public static string? RedirectTo { get; set; }

            public static string? NextRedirectTo { get; set; }

            public static string? RedirectOutlet { get; set; }

            public static NavigationResult? InnerResult { get; private set; }

            public async Task OnNavigatedToAsync(NavigationContext context, CancellationToken cancellation)
            {
                if (RedirectTo == null)
                    return;

                var target = RedirectTo;
                RedirectTo = NextRedirectTo;
                NextRedirectTo = null;

                InnerResult = await Navigation!.NavigateAsync(target, outlet: RedirectOutlet);
            }

            public Task OnNavigatedFromAsync(NavigationContext context, CancellationToken cancellation)
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Navigates from CanNavigateFromAsync — inside the outlet's slot.
        /// </summary>
        public sealed class ReentrantGuardViewModel : INavigationGuard
        {
            public static void Reset(INavigationService navigation)
            {
                Navigation = navigation;
                NavigateTo = null;
                NavigateOutlet = null;
                InnerResult = null;
            }

            public static INavigationService? Navigation { get; private set; }

            public static string? NavigateTo { get; set; }

            public static string? NavigateOutlet { get; set; }

            public static NavigationResult? InnerResult { get; private set; }

            public Task<bool> CanNavigateToAsync(NavigationContext context, CancellationToken cancellation)
            {
                return Task.FromResult(true);
            }

            public async Task<bool> CanNavigateFromAsync(NavigationContext context, CancellationToken cancellation)
            {
                if (NavigateTo == null)
                    return true;

                var target = NavigateTo;
                NavigateTo = null;

                InnerResult = await Navigation!.NavigateAsync(target, outlet: NavigateOutlet);

                return true;
            }
        }

        #endregion
    }
}
