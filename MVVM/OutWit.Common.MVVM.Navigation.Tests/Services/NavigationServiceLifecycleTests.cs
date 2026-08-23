using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class NavigationServiceLifecycleTests
    {
        #region Constants

        private const string FIRST = "first";
        private const string SECOND = "second";
        private const string TRANSIENT = "transient";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;
        private CallLog m_log = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            GlobalGuard.AllowTo = _ => true;
            GlobalGuard.AllowFrom = _ => true;
            TransientViewModel.CanNavigateTo = true;

            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<AwareViewModel>(FIRST);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
                nav.AddRoute<TransientViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.AddGuard<GlobalGuard>();
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_log = m_provider.GetRequiredService<CallLog>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Order Tests

        [Test]
        public async Task CallOrderFollowsSpecificationTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            m_log.Clear();

            await m_navigation.NavigateAsync(SECOND);

            Assert.That(m_log.Entries, Is.EqualTo(new[]
            {
                "Global.From",
                "Aware.CanFrom",
                "Global.To",
                "AwareSecond.Created",
                "AwareSecond.CanTo",
                "Aware.From",
                "AwareSecond.To"
            }));
        }

        [Test]
        public async Task FirstNavigationHasNoCurrentToAskTest()
        {
            await m_navigation.NavigateAsync(FIRST);

            Assert.That(m_log.Entries, Is.EqualTo(new[]
            {
                "Global.From",
                "Global.To",
                "Aware.Created",
                "Aware.CanTo",
                "Aware.To"
            }));
        }

        [Test]
        public async Task ContextDescribesTargetOnBothSidesTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;
            NavigationContext? fromContext = null;
            first.CanNavigateFrom = context => { fromContext = context; return true; };

            await m_navigation.NavigateAsync(SECOND, new NavigationParameters(("id", 3)));
            var second = (AwareViewModel)m_navigation.Outlet().Content!;

            Assert.That(fromContext!.RouteKey, Is.EqualTo(SECOND));
            Assert.That(fromContext.Parameters.Get<int>("id"), Is.EqualTo(3));
            Assert.That(fromContext.Kind, Is.EqualTo(NavigationKind.New));
            Assert.That(fromContext.Outlet, Is.EqualTo(NavigationOutlets.MAIN));
            Assert.That(second.LastContext!.RouteKey, Is.EqualTo(SECOND));
            Assert.That(second.LastContext.Route.ViewModelType, Is.EqualTo(typeof(AwareSecondViewModel)));
        }

        #endregion

        #region Guard Tests

        [Test]
        public async Task CurrentViewModelRefusingToLeaveReturnsRejectedTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;
            first.CanNavigateFrom = _ => false;
            m_log.Clear();

            var result = await m_navigation.NavigateAsync(SECOND);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(first.NavigatedFromCount, Is.EqualTo(0));
            Assert.That(m_log.Count("AwareSecond.Created"), Is.EqualTo(0));
        }

        [Test]
        public async Task GlobalGuardRefusingToLeaveReturnsRejectedBeforeAskingViewModelTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            GlobalGuard.AllowFrom = _ => false;
            m_log.Clear();

            var result = await m_navigation.NavigateAsync(SECOND);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_log.Entries, Is.EqualTo(new[] { "Global.From" }));
        }

        [Test]
        public async Task GlobalGuardRefusingToEnterReturnsRejectedWithoutCreatingTargetTest()
        {
            GlobalGuard.AllowTo = context => context.RouteKey != SECOND;

            var first = await m_navigation.NavigateAsync(FIRST);
            var second = await m_navigation.NavigateAsync(SECOND);

            Assert.That(first.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(second.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_log.Count("AwareSecond.Created"), Is.EqualTo(0));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareViewModel>());
        }

        [Test]
        public async Task GlobalGuardSeesRouteMetadataTest()
        {
            object? metadata = null;
            GlobalGuard.AllowTo = context => { metadata = context.Route.Metadata; return true; };

            var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<PlainViewModel>("gated", metadata: "Feature.X");
                nav.AddGuard<GlobalGuard>();
            });

            await provider.GetRequiredService<INavigationService>().NavigateAsync("gated");

            Assert.That(metadata, Is.EqualTo("Feature.X"));
        }

        [Test]
        public async Task GuardInstanceRegisteredThroughBuilderIsAskedTest()
        {
            var asked = 0;
            var guard = new DelegateGuard(_ => { asked++; return true; });

            var provider = NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<PlainViewModel>("plain");
                nav.AddGuard(guard);
            });

            await provider.GetRequiredService<INavigationService>().NavigateAsync("plain");

            Assert.That(asked, Is.EqualTo(2));
        }

        [Test]
        public async Task TargetRefusingToEnterReturnsRejectedTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = m_navigation.Outlet().Content;
            TransientViewModel.CanNavigateTo = false;

            var result = await m_navigation.NavigateAsync(TRANSIENT);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
        }

        [Test]
        public async Task TransientTargetRefusingToEnterIsDisposedTest()
        {
            TransientViewModel.CanNavigateTo = false;

            await m_navigation.NavigateAsync(TRANSIENT);

            Assert.That(m_log.Count("Transient#" + ExtractLastTransientId() + ".Disposed"), Is.EqualTo(1));
        }

        [Test]
        public async Task CanNavigateAsksGuardsWithoutNavigatingTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;
            first.CanNavigateFrom = _ => false;

            var can = await m_navigation.CanNavigateAsync(SECOND);

            Assert.That(can, Is.False);
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(m_log.Count("AwareSecond.Created"), Is.EqualTo(0));

            first.CanNavigateFrom = _ => true;

            Assert.That(await m_navigation.CanNavigateAsync(SECOND), Is.True);
            Assert.That(await m_navigation.CanNavigateAsync("missing"), Is.False);
        }

        #endregion

        #region Failure Tests

        [Test]
        public async Task ExceptionInOnNavigatedToReturnsFailedWithScreenShownTest()
        {
            NavigationResult? failed = null;
            var navigated = 0;
            m_navigation.NavigationFailed += (_, result) => failed = result;
            m_navigation.Navigated += (_, _) => navigated++;

            // the instance is created by the navigation, so the failure is configured by
            // navigating once, setting the throw, and refreshing
            await m_navigation.NavigateAsync(FIRST);
            var target = (AwareViewModel)m_navigation.Outlet().Content!;
            target.ThrowOnNavigatedTo = new InvalidOperationException("boom");

            var result = await m_navigation.RefreshAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Failed));
            Assert.That(result.Error, Is.InstanceOf<InvalidOperationException>());
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(target));
            Assert.That(failed, Is.SameAs(result));
            Assert.That(navigated, Is.EqualTo(2));
        }

        [Test]
        public async Task ExceptionInOnNavigatedFromStillCommitsTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = (AwareViewModel)m_navigation.Outlet().Content!;
            first.ThrowOnNavigatedFrom = new InvalidOperationException("leaving failed");

            var result = await m_navigation.NavigateAsync(SECOND);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Failed));
            Assert.That(result.Error, Is.SameAs(first.ThrowOnNavigatedFrom));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareSecondViewModel>());
        }

        #endregion

        #region Tools

        private string ExtractLastTransientId()
        {
            foreach (var entry in m_log.Entries)
            {
                if (entry.StartsWith("Transient#") && entry.EndsWith(".Created"))
                    return entry.Substring("Transient#".Length, entry.Length - "Transient#".Length - ".Created".Length);
            }

            return "?";
        }

        #endregion

        #region Classes

        private sealed class DelegateGuard : INavigationGuard
        {
            private readonly Func<NavigationContext, bool> m_allow;

            public DelegateGuard(Func<NavigationContext, bool> allow)
            {
                m_allow = allow;
            }

            public Task<bool> CanNavigateToAsync(NavigationContext context, System.Threading.CancellationToken cancellation)
            {
                return Task.FromResult(m_allow(context));
            }

            public Task<bool> CanNavigateFromAsync(NavigationContext context, System.Threading.CancellationToken cancellation)
            {
                return Task.FromResult(m_allow(context));
            }
        }

        #endregion
    }
}
