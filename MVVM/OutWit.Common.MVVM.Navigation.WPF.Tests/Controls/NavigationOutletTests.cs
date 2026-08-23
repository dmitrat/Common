using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.WPF.Controls;
using OutWit.Common.MVVM.Navigation.WPF.Services;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.ViewModels;
using OutWit.Common.MVVM.Navigation.WPF.Tests.Mock.Views;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Controls
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class NavigationOutletTests
    {
        #region Constants

        private const string SAMPLE = "sample";
        private const string TRANSIENT = "transient";
        private const string INJECTED = "injected";

        #endregion

        #region View Caching Tests

        [Test]
        public async Task OutletShowsViewForCurrentViewModelTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(((SampleView)control.Content!).DataContext, Is.SameAs(navigation.Outlet().Content));
        }

        [Test]
        public async Task CachedRouteKeepsViewAcrossNavigationsTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());

            await navigation.NavigateAsync(SAMPLE);
            var first = control.Content;
            await navigation.NavigateAsync(INJECTED);
            Assert.That(control.Content, Is.InstanceOf<InjectedView>());
            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.SameAs(first));
        }

        [Test]
        public async Task KeepViewsFalseRebuildsViewEveryTimeTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());
            control.KeepViews = false;

            await navigation.NavigateAsync(SAMPLE);
            var first = control.Content;
            await navigation.NavigateAsync(INJECTED);
            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(control.Content, Is.Not.SameAs(first));
        }

        [Test]
        public async Task TransientRouteGetsNewViewEveryTimeTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());

            await navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 1)));
            var first = control.Content;
            await navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 2)));

            Assert.That(control.Content, Is.InstanceOf<TransientSampleView>());
            Assert.That(control.Content, Is.Not.SameAs(first));
        }

        #endregion

        #region Factory Discovery Tests

        [Test]
        public async Task WithoutAnyFactoryOutletHandsViewModelToPresenterTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = new NavigationOutlet { Outlet = navigation.Outlet() };

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleViewModel>());
        }

        [Test]
        public async Task OutletAssignedAfterNavigationShowsViewTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            await navigation.NavigateAsync(SAMPLE);

            var control = Create(provider, navigation.Outlet());

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
        }

        [Test]
        public async Task SwappingOutletFollowsTheNewOneTest()
        {
            using var provider = Build(nav => nav.AddOutlet("Second"));
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());
            await navigation.NavigateAsync(SAMPLE);
            await navigation.NavigateAsync(INJECTED, outlet: "Second");

            control.Outlet = navigation.Outlet("Second");

            Assert.That(control.Content, Is.InstanceOf<InjectedView>());

            await navigation.NavigateAsync(TRANSIENT);

            Assert.That(control.Content, Is.InstanceOf<InjectedView>());
        }

        #endregion

        #region Transition Tests

        [Test]
        public async Task WithoutADurationTheSwapIsImmediateTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());

            Assert.That(control.TransitionDuration, Is.EqualTo(TimeSpan.Zero), "no animation unless asked for");

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(control.Opacity, Is.EqualTo(1));
        }

        [Test]
        public async Task AnOffScreenOutletDoesNotAnimateTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = Create(provider, navigation.Outlet());
            control.TransitionDuration = TimeSpan.FromSeconds(5);

            // never shown in a window: fading something nobody can see would only delay it
            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>(), "the content must be there at once, not in five seconds");
            Assert.That(control.Opacity, Is.EqualTo(1));
        }

        #endregion

        #region Tools

        private static ServiceProvider Build(System.Action<Navigation.Utils.NavigationBuilder>? extra = null)
        {
            return WpfTestHost.Build(nav =>
            {
                nav.AddRoute<SampleViewModel>(SAMPLE);
                nav.AddRoute<TransientSampleViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.AddRoute<InjectedViewModel>(INJECTED);
                extra?.Invoke(nav);
            });
        }

        private static NavigationOutlet Create(ServiceProvider provider, INavigationOutlet outlet)
        {
            return new NavigationOutlet
            {
                ViewFactory = provider.GetRequiredService<ViewLocator>(),
                Outlet = outlet
            };
        }

        #endregion
    }
}
