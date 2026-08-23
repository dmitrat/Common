using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class NavigationServiceHistoryTests
    {
        #region Constants

        private const string FIRST = "first";
        private const string SECOND = "second";
        private const string THIRD = "third";
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
            TransientViewModel.CanNavigateTo = true;
            m_provider = Build(NavigationOptions.DEFAULT_HISTORY_DEPTH);
            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_log = m_provider.GetRequiredService<CallLog>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Back And Forward Tests

        [Test]
        public async Task GoBackReturnsPreviousRouteAndParametersTest()
        {
            var parameters = new NavigationParameters(("id", 1));
            await m_navigation.NavigateAsync(FIRST, parameters);
            await m_navigation.NavigateAsync(SECOND, new NavigationParameters(("id", 2)));
            var outlet = m_navigation.Outlet();
            Assert.That(outlet.CanGoBack, Is.True);
            Assert.That(outlet.CanGoForward, Is.False);

            var result = await m_navigation.GoBackAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(outlet.Content, Is.InstanceOf<AwareViewModel>());
            Assert.That(outlet.Parameters.Is(parameters), Is.True);
            Assert.That(outlet.CanGoBack, Is.False);
            Assert.That(outlet.CanGoForward, Is.True);
            Assert.That(((AwareViewModel)outlet.Content!).LastContext!.Kind, Is.EqualTo(NavigationKind.Back));
        }

        [Test]
        public async Task GoForwardAfterGoBackReturnsNextEntryTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            await m_navigation.NavigateAsync(SECOND);
            await m_navigation.GoBackAsync();

            var result = await m_navigation.GoForwardAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareSecondViewModel>());
            Assert.That(((AwareViewModel)m_navigation.Outlet().Content!).LastContext!.Kind, Is.EqualTo(NavigationKind.Forward));
            Assert.That(m_navigation.Outlet().CanGoForward, Is.False);
        }

        [Test]
        public async Task GoBackWithNothingBehindReturnsRejectedTest()
        {
            await m_navigation.NavigateAsync(FIRST);

            var result = await m_navigation.GoBackAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareViewModel>());
        }

        [Test]
        public async Task GoForwardWithNothingAheadReturnsRejectedTest()
        {
            await m_navigation.NavigateAsync(FIRST);

            var result = await m_navigation.GoForwardAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
        }

        [Test]
        public async Task NavigateAfterGoBackTruncatesForwardHistoryTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            await m_navigation.NavigateAsync(SECOND);
            await m_navigation.NavigateAsync(THIRD);
            await m_navigation.GoBackAsync();
            await m_navigation.GoBackAsync();
            var outlet = m_navigation.Outlet();
            Assert.That(outlet.Content, Is.InstanceOf<AwareViewModel>());

            await m_navigation.NavigateAsync(TRANSIENT);

            Assert.That(outlet.History, Has.Count.EqualTo(2));
            Assert.That(outlet.History[0].RouteKey, Is.EqualTo(FIRST));
            Assert.That(outlet.History[1].RouteKey, Is.EqualTo(TRANSIENT));
            Assert.That(outlet.HistoryIndex, Is.EqualTo(1));
            Assert.That(outlet.CanGoForward, Is.False);
        }

        [Test]
        public async Task GoBackToTransientRouteCreatesNewInstanceWithOldParametersTest()
        {
            var parameters = new NavigationParameters(("id", 1));
            await m_navigation.NavigateAsync(TRANSIENT, parameters);
            var first = (TransientViewModel)m_navigation.Outlet().Content!;
            await m_navigation.NavigateAsync(FIRST);

            var result = await m_navigation.GoBackAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<TransientViewModel>());
            Assert.That(m_navigation.Outlet().Content, Is.Not.SameAs(first));
            Assert.That(m_navigation.Outlet().Parameters.Is(parameters), Is.True);
        }

        [Test]
        public async Task GoBackToCachedRouteShowsSameInstanceTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            var first = m_navigation.Outlet().Content;
            await m_navigation.NavigateAsync(SECOND);

            await m_navigation.GoBackAsync();

            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
        }

        #endregion

        #region Depth Tests

        [Test]
        public async Task HistoryDepthIsRespectedTest()
        {
            using var provider = Build(2);
            var navigation = provider.GetRequiredService<INavigationService>();

            await navigation.NavigateAsync(FIRST);
            await navigation.NavigateAsync(SECOND);
            await navigation.NavigateAsync(THIRD);
            var outlet = navigation.Outlet();

            Assert.That(outlet.History, Has.Count.EqualTo(2));
            Assert.That(outlet.History[0].RouteKey, Is.EqualTo(SECOND));
            Assert.That(outlet.History[1].RouteKey, Is.EqualTo(THIRD));
            Assert.That(outlet.HistoryIndex, Is.EqualTo(1));
            Assert.That(outlet.CanGoBack, Is.True);

            await navigation.GoBackAsync();

            Assert.That(outlet.CanGoBack, Is.False);
        }

        [Test]
        public async Task HistoryDepthZeroDisablesJournalTest()
        {
            using var provider = Build(0);
            var navigation = provider.GetRequiredService<INavigationService>();

            await navigation.NavigateAsync(FIRST);
            await navigation.NavigateAsync(SECOND);
            var outlet = navigation.Outlet();

            Assert.That(outlet.History, Is.Empty);
            Assert.That(outlet.HistoryIndex, Is.EqualTo(-1));
            Assert.That(outlet.CanGoBack, Is.False);

            var result = await navigation.GoBackAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(outlet.Content, Is.InstanceOf<AwareSecondViewModel>());
        }

        [Test]
        public async Task ClearHistoryEmptiesJournalAndKeepsContentTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            await m_navigation.NavigateAsync(SECOND);
            var content = m_navigation.Outlet().Content;

            m_navigation.ClearHistory();
            var outlet = m_navigation.Outlet();

            Assert.That(outlet.History, Is.Empty);
            Assert.That(outlet.CanGoBack, Is.False);
            Assert.That(outlet.Content, Is.SameAs(content));
            Assert.That((await m_navigation.GoBackAsync()).Status, Is.EqualTo(NavigationStatus.Rejected));
        }

        [Test]
        public async Task UnchangedNavigationDoesNotWriteHistoryTest()
        {
            await m_navigation.NavigateAsync(FIRST);
            await m_navigation.NavigateAsync(FIRST);

            Assert.That(m_navigation.Outlet().History, Has.Count.EqualTo(1));
        }

        #endregion

        #region Refresh Tests

        [Test]
        public async Task RefreshCallsFromAndToOnCachedInstanceTest()
        {
            await m_navigation.NavigateAsync(FIRST, new NavigationParameters(("id", 1)));
            var first = (AwareViewModel)m_navigation.Outlet().Content!;

            var result = await m_navigation.RefreshAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.SameAs(first));
            Assert.That(first.NavigatedFromCount, Is.EqualTo(1));
            Assert.That(first.NavigatedToCount, Is.EqualTo(2));
            Assert.That(first.LastContext!.Kind, Is.EqualTo(NavigationKind.Refresh));
            Assert.That(first.LastContext.Parameters.Get<int>("id"), Is.EqualTo(1));
            Assert.That(m_navigation.Outlet().History, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task RefreshOnTransientCreatesNewInstanceTest()
        {
            await m_navigation.NavigateAsync(TRANSIENT);
            var first = (TransientViewModel)m_navigation.Outlet().Content!;

            var result = await m_navigation.RefreshAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().Content, Is.Not.SameAs(first));
            Assert.That(first.IsDisposed, Is.True);
        }

        [Test]
        public async Task RefreshOnEmptyOutletReturnsRejectedTest()
        {
            var result = await m_navigation.RefreshAsync();

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Rejected));
        }

        #endregion

        #region Tools

        private static ServiceProvider Build(int historyDepth)
        {
            return NavigationTestHost.Build(nav =>
            {
                nav.AddRoute<AwareViewModel>(FIRST);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
                nav.AddRoute<AwareThirdViewModel>(THIRD);
                nav.AddRoute<TransientViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.HistoryDepth = historyDepth;
            });
        }

        #endregion
    }
}
