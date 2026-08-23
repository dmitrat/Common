using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class NavigationServiceTests
    {
        #region Constants

        private const string PLAIN = "plain";
        private const string AWARE = "aware";
        private const string SECOND = "second";
        private const string TRANSIENT = "transient";
        private const string THROWING = "throwing";
        private const string SIDE_OUTLET = "Side";
        private const string SIDE_ROUTE = "side";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;
        private CallLog m_log = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            TransientViewModel.CanNavigateTo = true;

            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddOutlet(SIDE_OUTLET);
                nav.AddRoute<PlainViewModel>(PLAIN);
                nav.AddRoute<AwareViewModel>(AWARE);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
                nav.AddRoute<TransientViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.AddRoute<ThrowingViewModel>(THROWING);
                nav.AddRoute<AwareThirdViewModel>(SIDE_ROUTE, outlet: SIDE_OUTLET);
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_log = m_provider.GetRequiredService<CallLog>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Route And Outlet Tests

        [Test]
        public async Task NavigateToUnknownRouteReturnsRouteNotFoundTest()
        {
            NavigationResult? failed = null;
            m_navigation.NavigationFailed += (_, result) => failed = result;

            var result = await m_navigation.NavigateAsync("missing");

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.RouteNotFound));
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(m_navigation.Outlet().Content, Is.Null);
            Assert.That(failed, Is.SameAs(result));
        }

        [Test]
        public async Task NavigateToUnknownOutletReturnsOutletNotFoundTest()
        {
            var result = await m_navigation.NavigateAsync(PLAIN, outlet: "nowhere");

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.OutletNotFound));
            Assert.That(result.Outlet, Is.EqualTo("nowhere"));
            Assert.That(m_navigation.Outlet().Content, Is.Null);
        }

        [Test]
        public async Task NavigateUsesRouteDefaultOutletWhenOutletIsNullTest()
        {
            var result = await m_navigation.NavigateAsync(SIDE_ROUTE);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(result.Outlet, Is.EqualTo(SIDE_OUTLET));
            Assert.That(m_navigation.Outlet(SIDE_OUTLET).Content, Is.InstanceOf<AwareThirdViewModel>());
            Assert.That(m_navigation.Outlet().Content, Is.Null);
        }

        [Test]
        public async Task ExplicitOutletOverridesRouteDefaultTest()
        {
            var result = await m_navigation.NavigateAsync(SIDE_ROUTE, outlet: NavigationOutlets.MAIN);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareThirdViewModel>());
        }

        [Test]
        public void OutletThrowsForUnknownNameTest()
        {
            Assert.That(() => m_navigation.Outlet("nowhere"), Throws.InvalidOperationException);
            Assert.That(m_navigation.HasOutlet("nowhere"), Is.False);
            Assert.That(m_navigation.HasOutlet(SIDE_OUTLET), Is.True);
        }

        [Test]
        public async Task AddOutletAtRunTimeIsIdempotentTest()
        {
            var first = m_navigation.AddOutlet("Tab1");
            var second = m_navigation.AddOutlet("Tab1");

            Assert.That(second, Is.SameAs(first));
            Assert.That(m_navigation.HasOutlet("Tab1"), Is.True);
            Assert.That(m_navigation.Outlets, Has.Count.EqualTo(3));

            var result = await m_navigation.NavigateAsync(PLAIN, outlet: "Tab1");

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(first.Content, Is.InstanceOf<PlainViewModel>());
        }

        [Test]
        public async Task NavigateGenericUsesFirstRouteForTypeTest()
        {
            var result = await m_navigation.NavigateAsync<AwareViewModel>();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(result.RouteKey, Is.EqualTo(AWARE));
        }

        [Test]
        public async Task NavigateGenericToUnregisteredTypeReturnsRouteNotFoundTest()
        {
            var result = await m_navigation.NavigateAsync<CallLog>();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.RouteNotFound));
        }

        [Test]
        public async Task OutletExposesRouteAndParametersTest()
        {
            var parameters = new NavigationParameters(("id", 7));

            await m_navigation.NavigateAsync(AWARE, parameters);
            var outlet = m_navigation.Outlet();

            Assert.That(outlet.RouteKey, Is.EqualTo(AWARE));
            Assert.That(outlet.Route!.ViewModelType, Is.EqualTo(typeof(AwareViewModel)));
            Assert.That(outlet.Parameters.Is(parameters), Is.True);
        }

        #endregion

        #region Creation Tests

        [Test]
        public async Task ViewModelIsCreatedWithDependenciesFromContainerTest()
        {
            await m_navigation.NavigateAsync(PLAIN);

            var viewModel = (PlainViewModel)m_navigation.Outlet().Content!;

            Assert.That(viewModel.Log, Is.SameAs(m_log));
        }

        [Test]
        public async Task CachedRouteReusesViewModelTest()
        {
            await m_navigation.NavigateAsync(AWARE);
            var first = m_navigation.Outlet().Content;

            await m_navigation.NavigateAsync(PLAIN);
            await m_navigation.NavigateAsync(AWARE);

            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(m_log.Count("Aware.Created"), Is.EqualTo(1));
        }

        [Test]
        public async Task NavigateToSameRouteWithSameParametersReturnsUnchangedTest()
        {
            var parameters = new NavigationParameters(("id", 1));
            var navigated = 0;
            m_navigation.Navigated += (_, _) => navigated++;

            var first = await m_navigation.NavigateAsync(AWARE, parameters);
            var second = await m_navigation.NavigateAsync(AWARE, new NavigationParameters(("id", 1)));

            Assert.That(first.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(second.Status, Is.EqualTo(NavigationStatus.Unchanged));
            Assert.That(second.IsSuccess, Is.True);
            Assert.That(m_log.Count("Aware.To"), Is.EqualTo(1));
            Assert.That(navigated, Is.EqualTo(1));
        }

        [Test]
        public async Task CachedRouteWithDifferentParametersReusesInstanceAndCallsOnNavigatedToAgainTest()
        {
            await m_navigation.NavigateAsync(AWARE, new NavigationParameters(("id", 1)));
            var first = (AwareViewModel)m_navigation.Outlet().Content!;

            var result = await m_navigation.NavigateAsync(AWARE, new NavigationParameters(("id", 2)));

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(first.NavigatedToCount, Is.EqualTo(2));
            Assert.That(first.NavigatedFromCount, Is.EqualTo(1));
            Assert.That(first.LastContext!.Parameters.Get<int>("id"), Is.EqualTo(2));
        }

        [Test]
        public async Task TransientRouteCreatesNewInstanceAndDisposesPreviousTest()
        {
            await m_navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 1)));
            var first = (TransientViewModel)m_navigation.Outlet().Content!;

            await m_navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 2)));
            var second = (TransientViewModel)m_navigation.Outlet().Content!;

            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first.IsDisposed, Is.True);
            Assert.That(first.Dependency.IsDisposed, Is.True);
            Assert.That(second.IsDisposed, Is.False);
            Assert.That(second.Dependency.IsDisposed, Is.False);
        }

        [Test]
        public async Task TransientRouteGetsItsOwnScopePerNavigationTest()
        {
            await m_navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 1)));
            var first = (TransientViewModel)m_navigation.Outlet().Content!;

            await m_navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 2)));
            var second = (TransientViewModel)m_navigation.Outlet().Content!;

            Assert.That(second.Dependency, Is.Not.SameAs(first.Dependency));
        }

        [Test]
        public async Task TransientRouteSameParametersIsNotUnchangedTest()
        {
            await m_navigation.NavigateAsync(TRANSIENT);
            var first = m_navigation.Outlet().Content;

            var result = await m_navigation.NavigateAsync(TRANSIENT);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.Not.SameAs(first));
        }

        [Test]
        public async Task LeavingTransientRouteDisposesItTest()
        {
            await m_navigation.NavigateAsync(TRANSIENT);
            var transient = (TransientViewModel)m_navigation.Outlet().Content!;

            await m_navigation.NavigateAsync(PLAIN);

            Assert.That(transient.IsDisposed, Is.True);
        }

        [Test]
        public async Task CachedViewModelIsNotDisposedWhenLeftTest()
        {
            await m_navigation.NavigateAsync(AWARE);
            var aware = (AwareViewModel)m_navigation.Outlet().Content!;

            await m_navigation.NavigateAsync(PLAIN);

            Assert.That(aware.IsDisposed, Is.False);
        }

        [Test]
        public async Task ExceptionInViewModelConstructorReturnsFailedAndContentUnchangedTest()
        {
            await m_navigation.NavigateAsync(PLAIN);
            var plain = m_navigation.Outlet().Content;
            NavigationResult? failed = null;
            m_navigation.NavigationFailed += (_, result) => failed = result;

            var result = await m_navigation.NavigateAsync(THROWING);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Failed));
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(plain));
            Assert.That(failed, Is.SameAs(result));
        }

        #endregion

        #region Evict Tests

        [Test]
        public async Task EvictDisposesCachedViewModelTest()
        {
            await m_navigation.NavigateAsync(AWARE);
            var aware = (AwareViewModel)m_navigation.Outlet().Content!;
            await m_navigation.NavigateAsync(PLAIN);

            var evicted = await m_navigation.EvictAsync(AWARE);

            Assert.That(evicted, Is.True);
            Assert.That(aware.IsDisposed, Is.True);

            await m_navigation.NavigateAsync(AWARE);

            Assert.That(m_navigation.Outlet().Content, Is.Not.SameAs(aware));
        }

        [Test]
        public async Task EvictDoesNotTouchCurrentViewModelTest()
        {
            await m_navigation.NavigateAsync(AWARE);
            var aware = (AwareViewModel)m_navigation.Outlet().Content!;

            var evicted = await m_navigation.EvictAsync(AWARE);

            Assert.That(evicted, Is.False);
            Assert.That(aware.IsDisposed, Is.False);
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(aware));
        }

        [Test]
        public async Task EvictReturnsFalseWhenNothingIsCachedTest()
        {
            Assert.That(await m_navigation.EvictAsync(AWARE), Is.False);
            Assert.That(await m_navigation.EvictAsync("missing"), Is.False);
        }

        #endregion

        #region Event Tests

        [Test]
        public async Task NavigatedIsRaisedWithOutletAndResultTest()
        {
            INavigationOutlet? outlet = null;
            NavigationResult? result = null;
            m_navigation.Navigated += (o, r) => { outlet = o; result = r; };

            await m_navigation.NavigateAsync(PLAIN);

            Assert.That(outlet, Is.SameAs(m_navigation.Outlet()));
            Assert.That(result!.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(result.RouteKey, Is.EqualTo(PLAIN));
        }

        [Test]
        public async Task NavigationFailedIsNotRaisedOnRejectedTest()
        {
            await m_navigation.NavigateAsync(AWARE);
            ((AwareViewModel)m_navigation.Outlet().Content!).CanNavigateFrom = _ => false;
            var failed = 0;
            m_navigation.NavigationFailed += (_, _) => failed++;

            var result = await m_navigation.NavigateAsync(PLAIN);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(failed, Is.EqualTo(0));
        }

        #endregion
    }
}
