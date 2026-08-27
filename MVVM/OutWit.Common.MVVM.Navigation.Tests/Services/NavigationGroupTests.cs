using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    /// <summary>
    /// A section opens on the page it was left at. The group is an ordinary navigation key,
    /// so the section's own item and an external NavigateAsync get the same behaviour, and
    /// no module keeps a "last page" of its own.
    /// </summary>
    [TestFixture]
    public class NavigationGroupTests
    {
        #region Constants

        private const string BAR = "NavigationBar";
        private const string SECTION = "record-info";
        private const string OTHER_SECTION = "record-extra";
        private const string GENERAL = "general";
        private const string DIARY = "diary";
        private const string OTHER = "other";
        private const string LONELY = "lonely";
        private const string TRANSIENT = "transient";
        private const string SIDE_OUTLET = "Side";
        private const string SIDE_ROUTE = "side";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;
        private IRouteRegistry m_routes = null!;
        private IContributionRegistry m_contributions = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            TransientViewModel.CanNavigateTo = true;

            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddOutlet(SIDE_OUTLET);
                nav.AddZone(BAR);
                nav.AddRoute<AwareViewModel>(GENERAL);
                nav.AddRoute<AwareSecondViewModel>(DIARY);
                nav.AddRoute<PlainViewModel>(OTHER);
                nav.AddRoute<PlainViewModel>(LONELY);
                nav.AddRoute<TransientViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.AddRoute<AwareThirdViewModel>(SIDE_ROUTE, outlet: SIDE_OUTLET);
                nav.AddGroup(SECTION, GENERAL, new[] { GENERAL, DIARY, TRANSIENT });
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_routes = m_provider.GetRequiredService<IRouteRegistry>();
            m_contributions = m_provider.GetRequiredService<IContributionRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Resolution Tests

        [Test]
        public async Task GroupWithNothingRememberedOpensTheDefaultTest()
        {
            var result = await m_navigation.NavigateAsync(SECTION);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL));
        }

        [Test]
        public async Task GroupReopensThePageLastShownTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            var result = await m_navigation.NavigateAsync(SECTION);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
        }

        /// <summary>
        /// The journal is written for New only. Had the memory hooked into it, Back would
        /// have left the memory at the page the user just came away from.
        /// </summary>
        [Test]
        public async Task GoingBackMovesTheMemoryTest()
        {
            await m_navigation.NavigateAsync(GENERAL);
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.GoBackAsync();
            await m_navigation.NavigateAsync(LONELY);

            await m_navigation.NavigateAsync(SECTION);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL));
        }

        [Test]
        public async Task MemoryIsKeptPerOutletTest()
        {
            await m_navigation.NavigateAsync(DIARY, outlet: SIDE_OUTLET);
            await m_navigation.NavigateAsync(SIDE_ROUTE, outlet: SIDE_OUTLET);

            await m_navigation.NavigateAsync(SECTION);
            await m_navigation.NavigateAsync(SECTION, outlet: SIDE_OUTLET);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL), "Main never showed a page of the group");
            Assert.That(m_navigation.Outlet(SIDE_OUTLET).RouteKey, Is.EqualTo(DIARY));
        }

        [Test]
        public async Task RouteInTwoGroupsIsRememberedInBothTest()
        {
            m_routes.RegisterGroup(OTHER_SECTION, OTHER, new[] { OTHER, DIARY });

            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            await m_navigation.NavigateAsync(SECTION);
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));

            await m_navigation.NavigateAsync(LONELY);
            await m_navigation.NavigateAsync(OTHER_SECTION);
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
        }

        [Test]
        public async Task RememberedParametersComeBackWithThePageTest()
        {
            await m_navigation.NavigateAsync(DIARY, new NavigationParameters(("id", 7)));
            await m_navigation.NavigateAsync(LONELY);

            await m_navigation.NavigateAsync(SECTION);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
            Assert.That(m_navigation.Outlet().Parameters.Is(new NavigationParameters(("id", 7))), Is.True);
        }

        [Test]
        public async Task CallerParametersWinOverRememberedOnesTest()
        {
            await m_navigation.NavigateAsync(DIARY, new NavigationParameters(("id", 7)));
            await m_navigation.NavigateAsync(LONELY);

            await m_navigation.NavigateAsync(SECTION, new NavigationParameters(("id", 9)));

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
            Assert.That(m_navigation.Outlet().Parameters.Is(new NavigationParameters(("id", 9))), Is.True);
        }

        [Test]
        public async Task GroupNavigationToTheCurrentPageIsUnchangedTest()
        {
            await m_navigation.NavigateAsync(DIARY);

            var result = await m_navigation.NavigateAsync(SECTION);

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Unchanged));
            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task GroupWhoseDefaultIsNotRegisteredIsRouteNotFoundTest()
        {
            m_routes.RegisterGroup("empty", "missing");
            NavigationResult? failed = null;
            m_navigation.NavigationFailed += (_, result) => failed = result;

            var result = await m_navigation.NavigateAsync("empty");

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.RouteNotFound));
            Assert.That(result.RouteKey, Is.EqualTo("empty"));
            Assert.That(failed, Is.SameAs(result));
        }

        [Test]
        public async Task AddToGroupBeforeTheRouteIsRegisteredWorksTest()
        {
            m_routes.AddToGroup("late", "late-page");
            m_routes.Register<PlainViewModel>("late-page");

            var result = await m_navigation.NavigateAsync("late");

            Assert.That(result.Status, Is.EqualTo(NavigationStatus.Success));
            Assert.That(result.RouteKey, Is.EqualTo("late-page"));
        }

        [Test]
        public async Task RedeclaringTheGroupKeepsTheRememberedPageTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            m_routes.RegisterGroup(SECTION, GENERAL, new[] { GENERAL, DIARY });
            await m_navigation.NavigateAsync(SECTION);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
        }

        [Test]
        public async Task RefusedNavigationLeavesTheMemoryAloneTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);
            TransientViewModel.CanNavigateTo = false;

            var refused = await m_navigation.NavigateAsync(TRANSIENT);
            await m_navigation.NavigateAsync(SECTION);

            Assert.That(refused.Status, Is.EqualTo(NavigationStatus.Rejected));
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
        }

        [Test]
        public async Task CanNavigateResolvesTheGroupTest()
        {
            m_routes.RegisterGroup("empty", "missing");

            Assert.That(await m_navigation.CanNavigateAsync(SECTION), Is.True);
            Assert.That(await m_navigation.CanNavigateAsync("empty"), Is.False);
        }

        #endregion

        #region Forget Tests

        [Test]
        public async Task ForgetGroupReturnsToTheDefaultTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            m_navigation.ForgetGroup(SECTION);
            await m_navigation.NavigateAsync(SECTION);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL));
        }

        [Test]
        public async Task ForgetWithoutAKeyClearsEveryGroupOfTheOutletTest()
        {
            m_routes.RegisterGroup(OTHER_SECTION, OTHER, new[] { OTHER, DIARY });
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            m_navigation.ForgetGroup(outlet: NavigationOutlets.MAIN);

            await m_navigation.NavigateAsync(SECTION);
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL));

            await m_navigation.NavigateAsync(LONELY);
            await m_navigation.NavigateAsync(OTHER_SECTION);
            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(OTHER));
        }

        [Test]
        public async Task ForgetIsScopedToTheOutletTest()
        {
            await m_navigation.NavigateAsync(DIARY, outlet: SIDE_OUTLET);
            await m_navigation.NavigateAsync(SIDE_ROUTE, outlet: SIDE_OUTLET);
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            m_navigation.ForgetGroup(SECTION, NavigationOutlets.MAIN);

            await m_navigation.NavigateAsync(SECTION);
            await m_navigation.NavigateAsync(SECTION, outlet: SIDE_OUTLET);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(GENERAL));
            Assert.That(m_navigation.Outlet(SIDE_OUTLET).RouteKey, Is.EqualTo(DIARY));
        }

        /// <summary>
        /// Where a section was left is not history. Clearing the journal must not reset it.
        /// </summary>
        [Test]
        public async Task ClearHistoryDoesNotForgetTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            m_navigation.ClearHistory();
            await m_navigation.NavigateAsync(SECTION);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
        }

        [Test]
        public async Task ResolveGroupTellsWhatWouldOpenTest()
        {
            Assert.That(m_navigation.ResolveGroup(SECTION)?.RouteKey, Is.EqualTo(GENERAL));
            Assert.That(m_navigation.ResolveGroup(GENERAL), Is.Null, "a route is not a group");

            await m_navigation.NavigateAsync(DIARY, new NavigationParameters(("id", 7)));

            var resolved = m_navigation.ResolveGroup(SECTION);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved!.RouteKey, Is.EqualTo(DIARY));
            Assert.That(resolved.Parameters.Is(new NavigationParameters(("id", 7))), Is.True);
            Assert.That(m_navigation.ResolveGroup(SECTION, SIDE_OUTLET)?.RouteKey, Is.EqualTo(GENERAL), "Side has its own memory");
        }

        #endregion

        #region Result Tests

        [Test]
        public async Task ResultReportsBothTheRequestedAndTheActualKeyTest()
        {
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            var viaGroup = await m_navigation.NavigateAsync(SECTION);
            var direct = await m_navigation.NavigateAsync(OTHER);

            Assert.That(viaGroup.RequestedKey, Is.EqualTo(SECTION));
            Assert.That(viaGroup.RouteKey, Is.EqualTo(DIARY));
            Assert.That(direct.RequestedKey, Is.EqualTo(OTHER));
            Assert.That(direct.RouteKey, Is.EqualTo(OTHER));
        }

        [Test]
        public async Task NavigatedEventCarriesTheRequestedKeyTest()
        {
            NavigationResult? seen = null;
            m_navigation.Navigated += (_, result) => seen = result;

            await m_navigation.NavigateAsync(SECTION);

            Assert.That(seen, Is.Not.Null);
            Assert.That(seen!.RequestedKey, Is.EqualTo(SECTION));
            Assert.That(seen.RouteKey, Is.EqualTo(GENERAL));
        }

        #endregion

        #region Selection Tests

        [Test]
        public async Task SectionIsSelectedForAnyPageOfItsGroupTest()
        {
            var section = Item("section", SECTION);
            var lonely = Item("lonely", LONELY);
            m_contributions.Add(section);
            m_contributions.Add(lonely);

            await m_navigation.NavigateAsync(GENERAL);
            Assert.That(section.IsSelected, Is.True);

            await m_navigation.NavigateAsync(DIARY);
            Assert.That(section.IsSelected, Is.True);
            Assert.That(m_contributions.Zone(BAR).Selected, Is.SameAs(section));

            await m_navigation.NavigateAsync(LONELY);
            Assert.That(section.IsSelected, Is.False);
            Assert.That(lonely.IsSelected, Is.True);
        }

        [Test]
        public async Task SectionIsNotSelectedForAPageShownInAnotherOutletTest()
        {
            var section = Item("section", SECTION);
            m_contributions.Add(section);

            await m_navigation.NavigateAsync(DIARY, outlet: SIDE_OUTLET);

            Assert.That(section.IsSelected, Is.False, "the section lives in Main; Side showing a member page is not it");
        }

        [Test]
        public async Task SectionSelectionStillRespectsParametersTest()
        {
            var generic = Item("generic", SECTION);
            var specific = Item("specific", SECTION, new NavigationParameters(("id", 1)));
            m_contributions.Add(generic);
            m_contributions.Add(specific);

            await m_navigation.NavigateAsync(DIARY, new NavigationParameters(("id", 2)));

            Assert.That(generic.IsSelected, Is.True);
            Assert.That(specific.IsSelected, Is.False);

            await m_navigation.NavigateAsync(DIARY, new NavigationParameters(("id", 1)));

            Assert.That(generic.IsSelected, Is.True);
            Assert.That(specific.IsSelected, Is.True);
        }

        [Test]
        public async Task SectionCommandOpensTheRememberedPageTest()
        {
            var section = Item("section", SECTION);
            m_contributions.Add(section);
            await m_navigation.NavigateAsync(DIARY);
            await m_navigation.NavigateAsync(LONELY);

            section.Command!.Execute(null);
            await WaitForRouteAsync(DIARY);

            Assert.That(m_navigation.Outlet().RouteKey, Is.EqualTo(DIARY));
            Assert.That(section.IsSelected, Is.True);
        }

        #endregion

        #region Tools

        private static ContributionItem Item(string key, string routeKey, NavigationParameters? parameters = null)
        {
            return new ContributionItem
            {
                Zone = BAR,
                Key = key,
                RouteKey = routeKey,
                Parameters = parameters,
                Header = key
            };
        }

        private async Task WaitForRouteAsync(string routeKey)
        {
            for (var i = 0; i < 200 && m_navigation.Outlet().RouteKey != routeKey; i++)
                await Task.Delay(10);
        }

        #endregion
    }
}
