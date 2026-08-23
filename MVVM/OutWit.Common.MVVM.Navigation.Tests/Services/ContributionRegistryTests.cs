using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Services
{
    [TestFixture]
    public class ContributionRegistryTests
    {
        #region Constants

        private const string BAR = "NavigationBar";
        private const string MENU = "Menu";
        private const string FIRST = "first";
        private const string SECOND = "second";
        private const string SIDE_OUTLET = "Side";
        private const string SIDE_ROUTE = "side";

        #endregion

        #region Fields

        private ServiceProvider m_provider = null!;
        private INavigationService m_navigation = null!;
        private IContributionRegistry m_contributions = null!;

        #endregion

        [SetUp]
        public void Setup()
        {
            m_provider = NavigationTestHost.Build(nav =>
            {
                nav.AddOutlet(SIDE_OUTLET);
                nav.AddZone(BAR);
                nav.AddRoute<AwareViewModel>(FIRST);
                nav.AddRoute<AwareSecondViewModel>(SECOND);
                nav.AddRoute<AwareThirdViewModel>(SIDE_ROUTE, outlet: SIDE_OUTLET);
            });

            m_navigation = m_provider.GetRequiredService<INavigationService>();
            m_contributions = m_provider.GetRequiredService<IContributionRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            m_provider.Dispose();
        }

        #region Ordering Tests

        [Test]
        public void ItemsAreSortedByOrderThenInsertionTest()
        {
            m_contributions.Add(Item(BAR, "c", order: 20));
            m_contributions.Add(Item(BAR, "a", order: 10));
            m_contributions.Add(Item(BAR, "b", order: 10));
            m_contributions.Add(Item(BAR, "d", order: 5));

            var keys = m_contributions.Zone(BAR).Items.Select(item => item.Key).ToArray();

            Assert.That(keys, Is.EqualTo(new[] { "d", "a", "b", "c" }));
        }

        [Test]
        public void ParentKeyBuildsTreeEvenWhenChildArrivesFirstTest()
        {
            m_contributions.Add(Item(MENU, "export.pdf", order: 2, parentKey: "export"));
            m_contributions.Add(Item(MENU, "export.csv", order: 1, parentKey: "export"));
            m_contributions.Add(Item(MENU, "export", order: 1));

            var zone = m_contributions.Zone(MENU);

            Assert.That(zone.Items, Has.Count.EqualTo(1));
            Assert.That(zone.Items[0].Key, Is.EqualTo("export"));
            Assert.That(zone.Items[0].Children.Select(item => item.Key), Is.EqualTo(new[] { "export.csv", "export.pdf" }));
            Assert.That(zone.Find("export.pdf"), Is.Not.Null);
        }

        [Test]
        public void RemovingParentOrphansChildrenUntilItReturnsTest()
        {
            m_contributions.Add(Item(MENU, "export"));
            m_contributions.Add(Item(MENU, "export.csv", parentKey: "export"));

            m_contributions.Remove(MENU, "export");
            var zone = m_contributions.Zone(MENU);

            Assert.That(zone.Items, Is.Empty);
            Assert.That(zone.Find("export.csv"), Is.Not.Null);

            m_contributions.Add(Item(MENU, "export"));

            Assert.That(zone.Items[0].Children, Has.Count.EqualTo(1));
        }

        #endregion

        #region Replacement Tests

        [Test]
        public void DuplicateKeyReplacesItemTest()
        {
            var first = Item(BAR, "a", order: 1);
            var second = Item(BAR, "a", order: 2);

            m_contributions.Add(first);
            m_contributions.Add(second);
            var zone = m_contributions.Zone(BAR);

            Assert.That(zone.Items, Has.Count.EqualTo(1));
            Assert.That(zone.Items[0], Is.SameAs(second));
            Assert.That(m_contributions.Find(BAR, "a"), Is.SameAs(second));
        }

        [Test]
        public void RemoveAndClearNotifyCollectionTest()
        {
            m_contributions.Add(Item(BAR, "a"));
            m_contributions.Add(Item(BAR, "b"));
            var zone = m_contributions.Zone(BAR);
            var changes = 0;
            ((INotifyCollectionChanged)zone.Items).CollectionChanged += (_, _) => changes++;

            Assert.That(m_contributions.Remove(BAR, "a"), Is.True);
            Assert.That(m_contributions.Remove(BAR, "missing"), Is.False);
            Assert.That(zone.Items, Has.Count.EqualTo(1));

            m_contributions.Clear(BAR);

            Assert.That(zone.Items, Is.Empty);
            Assert.That(changes, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ZoneIsCreatedOnFirstUseTest()
        {
            Assert.That(m_contributions.Zones, Is.EqualTo(new[] { BAR }));

            m_contributions.Add(Item("Toolbar", "x"));

            Assert.That(m_contributions.Zones, Is.EqualTo(new[] { BAR, "Toolbar" }));
            Assert.That(m_contributions.Zone("Another").Name, Is.EqualTo("Another"));
            Assert.That(m_contributions.Zones, Has.Count.EqualTo(3));
        }

        [Test]
        public void AddRequiresZoneAndKeyTest()
        {
            Assert.That(() => m_contributions.Add(null!), Throws.ArgumentNullException);
            Assert.That(() => m_contributions.Add(new ContributionItem { Zone = "", Key = "a" }), Throws.ArgumentException);
            Assert.That(() => m_contributions.Add(new ContributionItem { Zone = BAR, Key = "" }), Throws.ArgumentException);
        }

        #endregion

        #region Selection Tests

        [Test]
        public async Task SelectedFollowsNavigatedTest()
        {
            var item = Item(BAR, "first", routeKey: FIRST);
            m_contributions.Add(item);
            var zone = m_contributions.Zone(BAR);

            await m_navigation.NavigateAsync(FIRST);

            Assert.That(item.IsSelected, Is.True);
            Assert.That(zone.Selected, Is.SameAs(item));

            await m_navigation.NavigateAsync(SECOND);

            Assert.That(item.IsSelected, Is.False);
            Assert.That(zone.Selected, Is.Null);
        }

        [Test]
        public async Task SelectedRespectsParametersTest()
        {
            var generic = Item(BAR, "generic", routeKey: FIRST);
            var specific = Item(BAR, "specific", routeKey: FIRST, parameters: new NavigationParameters(("id", 1)));
            m_contributions.Add(generic);
            m_contributions.Add(specific);

            await m_navigation.NavigateAsync(FIRST, new NavigationParameters(("id", 2)));

            Assert.That(generic.IsSelected, Is.True);
            Assert.That(specific.IsSelected, Is.False);

            await m_navigation.NavigateAsync(FIRST, new NavigationParameters(("id", 1)));

            Assert.That(generic.IsSelected, Is.True);
            Assert.That(specific.IsSelected, Is.True);
        }

        [Test]
        public async Task ItemAddedAfterNavigationIsSelectedImmediatelyTest()
        {
            await m_navigation.NavigateAsync(FIRST);

            var item = Item(BAR, "first", routeKey: FIRST);
            m_contributions.Add(item);

            Assert.That(item.IsSelected, Is.True);
            Assert.That(m_contributions.Zone(BAR).Selected, Is.SameAs(item));
        }

        [Test]
        public async Task SelectionIsPerOutletTest()
        {
            var main = Item(BAR, "main", routeKey: FIRST);
            var side = Item(BAR, "side", routeKey: SIDE_ROUTE);
            m_contributions.Add(main);
            m_contributions.Add(side);

            await m_navigation.NavigateAsync(FIRST);
            await m_navigation.NavigateAsync(SIDE_ROUTE);

            Assert.That(main.IsSelected, Is.True);
            Assert.That(side.IsSelected, Is.True);

            await m_navigation.NavigateAsync(SECOND);

            Assert.That(main.IsSelected, Is.False);
            Assert.That(side.IsSelected, Is.True);
        }

        #endregion

        #region Command Tests

        [Test]
        public async Task CommandIsAttachedForRouteKeyAndNavigatesTest()
        {
            var item = Item(BAR, "first", routeKey: FIRST, parameters: new NavigationParameters(("id", 9)));
            m_contributions.Add(item);

            Assert.That(item.Command, Is.Not.Null);
            Assert.That(item.Command!.CanExecute(null), Is.True);

            item.Command.Execute(null);
            await WaitForContentAsync();

            Assert.That(m_navigation.Outlet().Content, Is.InstanceOf<AwareViewModel>());
            Assert.That(m_navigation.Outlet().Parameters.Get<int>("id"), Is.EqualTo(9));
        }

        [Test]
        public void CommandIsNotAttachedWithoutRouteKeyTest()
        {
            var item = Item(BAR, "label");
            m_contributions.Add(item);

            Assert.That(item.Command, Is.Null);
        }

        [Test]
        public void CommandCanExecuteFollowsIsEnabledTest()
        {
            var item = Item(BAR, "first", routeKey: FIRST);
            m_contributions.Add(item);
            var raised = 0;
            item.Command!.CanExecuteChanged += (_, _) => raised++;

            item.IsEnabled = false;

            Assert.That(item.Command.CanExecute(null), Is.False);
            Assert.That(raised, Is.EqualTo(1));

            item.IsEnabled = true;

            Assert.That(item.Command.CanExecute(null), Is.True);
        }

        [Test]
        public void RemovedItemLosesItsCommandTest()
        {
            var item = Item(BAR, "first", routeKey: FIRST);
            m_contributions.Add(item);

            m_contributions.Remove(BAR, "first");

            Assert.That(item.Command, Is.Null);
        }

        #endregion

        #region Dispatcher Tests

        [Test]
        public void AddMarshalsThroughDispatcherTest()
        {
            var dispatcher = new CountingDispatcher { HasAccess = false };
            var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>(FIRST), dispatcher: dispatcher);
            var contributions = provider.GetRequiredService<IContributionRegistry>();

            contributions.Add(Item(BAR, "a"));

            Assert.That(dispatcher.Invocations, Is.EqualTo(1));
            Assert.That(contributions.Zone(BAR).Items, Has.Count.EqualTo(1));
        }

        #endregion

        #region Tools

        private static ContributionItem Item(string zone, string key, double order = 0, string? parentKey = null, string? routeKey = null, NavigationParameters? parameters = null)
        {
            return new ContributionItem
            {
                Zone = zone,
                Key = key,
                Order = order,
                ParentKey = parentKey,
                RouteKey = routeKey,
                Parameters = parameters,
                Header = key
            };
        }

        private async Task WaitForContentAsync()
        {
            for (var i = 0; i < 50 && m_navigation.Outlet().Content == null; i++)
                await Task.Delay(10);
        }

        #endregion
    }
}
