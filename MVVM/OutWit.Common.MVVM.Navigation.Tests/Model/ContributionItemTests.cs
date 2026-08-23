using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Tests.Mock;

namespace OutWit.Common.MVVM.Navigation.Tests.Model
{
    /// <summary>
    /// [Notify] is woven by AspectInjector at build time through the transitive reference
    /// to OutWit.Common.Aspects. If the weave did not reach this assembly, the properties
    /// below would compile and silently never notify — so the weave itself is under test.
    /// </summary>
    [TestFixture]
    public class ContributionItemTests
    {
        #region Notify Tests

        [Test]
        public void HeaderRaisesPropertyChangedTest()
        {
            var item = new ContributionItem { Zone = "Bar", Key = "a" };
            var changed = new List<string?>();
            item.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            item.Header = "Hello";
            item.IsEnabled = false;
            item.IsChecked = true;
            item.Icon = "Box";

            Assert.That(changed, Is.EqualTo(new[]
            {
                nameof(ContributionItem.Header),
                nameof(ContributionItem.IsEnabled),
                nameof(ContributionItem.IsChecked),
                nameof(ContributionItem.Icon)
            }));
        }

        [Test]
        public async Task OutletRaisesPropertyChangedOnNavigationTest()
        {
            using var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>("plain"));
            var navigation = provider.GetRequiredService<INavigationService>();
            var outlet = navigation.Outlet();
            var changed = new List<string?>();
            outlet.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            await navigation.NavigateAsync("plain");

            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.Content)));
            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.Route)));
            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.RouteKey)));
            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.Parameters)));
            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.IsNavigating)));
            Assert.That(changed, Does.Contain(nameof(INavigationOutlet.CanGoBack)));
        }

        [Test]
        public async Task ZoneRaisesPropertyChangedOnSelectedTest()
        {
            using var provider = NavigationTestHost.Build(nav => nav.AddRoute<PlainViewModel>("plain"));
            var navigation = provider.GetRequiredService<INavigationService>();
            var contributions = provider.GetRequiredService<IContributionRegistry>();
            var zone = contributions.Zone("Bar");
            var changed = new List<string?>();
            zone.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            contributions.Add(new ContributionItem { Zone = "Bar", Key = "plain", RouteKey = "plain" });

            await navigation.NavigateAsync("plain");

            Assert.That(changed, Does.Contain(nameof(IContributionZone.Selected)));
            Assert.That(zone.Selected, Is.Not.Null);
        }

        #endregion

        #region Identity Tests

        [Test]
        public void ToStringShowsZoneKeyAndRouteTest()
        {
            var item = new ContributionItem { Zone = "Bar", Key = "a", RouteKey = "r" };

            Assert.That(item.ToString(), Is.EqualTo("Bar/a -> r"));
        }

        [Test]
        public void ChildrenStartEmptyTest()
        {
            var item = new ContributionItem { Zone = "Bar", Key = "a" };

            Assert.That(item.Children, Is.Empty);
            Assert.That(item.IsVisible, Is.True);
            Assert.That(item.IsEnabled, Is.True);
            Assert.That(item.Command, Is.Null);
        }

        #endregion
    }
}
