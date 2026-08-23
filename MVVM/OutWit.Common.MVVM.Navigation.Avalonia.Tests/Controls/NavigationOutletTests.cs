using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Navigation.Avalonia.Controls;
using OutWit.Common.MVVM.Navigation.Avalonia.Services;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.ViewModels;
using OutWit.Common.MVVM.Navigation.Avalonia.Tests.Mock.Views;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Controls
{
    [TestFixture]
    public class NavigationOutletTests
    {
        #region Constants

        private const string SAMPLE = "sample";
        private const string TRANSIENT = "transient";
        private const string INJECTED = "injected";

        #endregion

        #region View Caching Tests

        [AvaloniaTest]
        public async Task OutletShowsViewForCurrentViewModelTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(((SampleView)control.Content!).DataContext, Is.SameAs(navigation.Outlet().Content));

            window.Close();
        }

        [AvaloniaTest]
        public async Task CachedRouteKeepsViewAcrossNavigationsTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());

            await navigation.NavigateAsync(SAMPLE);
            var first = control.Content;

            await navigation.NavigateAsync(INJECTED);
            Assert.That(control.Content, Is.InstanceOf<InjectedView>());

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.SameAs(first));

            window.Close();
        }

        [AvaloniaTest]
        public async Task KeepViewsFalseRebuildsViewEveryTimeTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());
            control.KeepViews = false;

            await navigation.NavigateAsync(SAMPLE);
            var first = control.Content;
            await navigation.NavigateAsync(INJECTED);
            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(control.Content, Is.Not.SameAs(first));

            window.Close();
        }

        [AvaloniaTest]
        public async Task TransientRouteGetsNewViewEveryTimeTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());

            await navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 1)));
            var first = control.Content;
            await navigation.NavigateAsync(TRANSIENT, new NavigationParameters(("id", 2)));

            Assert.That(control.Content, Is.InstanceOf<TransientSampleView>());
            Assert.That(control.Content, Is.Not.SameAs(first));

            window.Close();
        }

        [AvaloniaTest]
        public async Task RefreshOfCachedViewModelKeepsTheViewTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());

            await navigation.NavigateAsync(SAMPLE);
            var first = control.Content;
            await navigation.RefreshAsync();

            Assert.That(control.Content, Is.SameAs(first));

            window.Close();
        }

        #endregion

        #region Factory Discovery Tests

        [AvaloniaTest]
        public async Task OutletFindsViewLocatorInApplicationDataTemplatesTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var locator = provider.GetRequiredService<ViewLocator>();
            global::Avalonia.Application.Current!.DataTemplates.Add(locator);

            try
            {
                var control = new NavigationOutlet { Outlet = navigation.Outlet() };
                var window = new Window { Content = control };
                window.Show();

                await navigation.NavigateAsync(SAMPLE);

                Assert.That(control.Content, Is.InstanceOf<SampleView>());

                window.Close();
            }
            finally
            {
                global::Avalonia.Application.Current!.DataTemplates.Remove(locator);
            }
        }

        [AvaloniaTest]
        public async Task WithoutAnyFactoryOutletHandsViewModelToPresenterTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var control = new NavigationOutlet { Outlet = navigation.Outlet() };
            var window = new Window { Content = control };
            window.Show();

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleViewModel>());

            window.Close();
        }

        [AvaloniaTest]
        public async Task SwappingOutletFollowsTheNewOneTest()
        {
            using var provider = Build(nav => nav.AddOutlet("Second"));
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());
            await navigation.NavigateAsync(SAMPLE);
            await navigation.NavigateAsync(INJECTED, outlet: "Second");

            control.Outlet = navigation.Outlet("Second");

            Assert.That(control.Content, Is.InstanceOf<InjectedView>(), "the control must follow the outlet it was just given");

            await navigation.NavigateAsync(TRANSIENT);

            Assert.That(control.Content, Is.InstanceOf<InjectedView>(), "and stop following the old one");

            window.Close();
        }

        #endregion

        #region Transition Tests

        [AvaloniaTest]
        public async Task WithoutADurationTheSwapIsImmediateTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());

            Assert.That(control.TransitionDuration, Is.EqualTo(TimeSpan.Zero), "no animation unless asked for");

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());
            Assert.That(control.Opacity, Is.EqualTo(1));

            window.Close();
        }

        [AvaloniaTest]
        public async Task AnOffScreenOutletDoesNotAnimateTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();

            // never attached to a window: fading something nobody can see would only delay it
            var control = new NavigationOutlet
            {
                ViewFactory = provider.GetRequiredService<ViewLocator>(),
                Outlet = navigation.Outlet(),
                TransitionDuration = TimeSpan.FromSeconds(5)
            };

            await navigation.NavigateAsync(SAMPLE);

            Assert.That(control.Content, Is.InstanceOf<SampleView>(), "the content must be there at once, not in five seconds");
            Assert.That(control.Opacity, Is.EqualTo(1));
        }

        #endregion

        #region Lifecycle Tests

        [AvaloniaTest]
        public async Task OutletReflectsOutletAssignedAfterNavigationTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            await navigation.NavigateAsync(SAMPLE);

            var (window, control) = Show(provider, navigation.Outlet());

            Assert.That(control.Content, Is.InstanceOf<SampleView>());

            window.Close();
        }

        [AvaloniaTest]
        public async Task DetachedOutletStopsFollowingNavigationTest()
        {
            using var provider = Build();
            var navigation = provider.GetRequiredService<INavigationService>();
            var (window, control) = Show(provider, navigation.Outlet());
            await navigation.NavigateAsync(SAMPLE);

            window.Content = null;
            await navigation.NavigateAsync(INJECTED);

            Assert.That(control.Content, Is.InstanceOf<SampleView>());

            window.Content = control;

            Assert.That(control.Content, Is.InstanceOf<InjectedView>());

            window.Close();
        }

        [AvaloniaTest]
        public void EmptyOutletShowsNothingTest()
        {
            using var provider = Build();
            var (window, control) = Show(provider, provider.GetRequiredService<INavigationService>().Outlet());

            Assert.That(control.Content, Is.Null);

            window.Close();
        }

        #endregion

        #region Tools

        private static ServiceProvider Build(Action<Navigation.Utils.NavigationBuilder>? extra = null)
        {
            return AvaloniaTestHost.Build(nav =>
            {
                nav.AddRoute<SampleViewModel>(SAMPLE);
                nav.AddRoute<TransientSampleViewModel>(TRANSIENT, NavigationRouteMode.Transient);
                nav.AddRoute<InjectedViewModel>(INJECTED);
                extra?.Invoke(nav);
            });
        }

        private static (Window Window, NavigationOutlet Control) Show(ServiceProvider provider, INavigationOutlet outlet)
        {
            var control = new NavigationOutlet
            {
                Outlet = outlet,
                ViewFactory = provider.GetRequiredService<ViewLocator>()
            };

            var window = new Window { Content = control };
            window.Show();

            return (window, control);
        }

        #endregion
    }
}
