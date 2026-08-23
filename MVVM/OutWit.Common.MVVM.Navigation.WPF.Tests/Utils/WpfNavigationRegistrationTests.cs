using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Utils;
using OutWit.Common.MVVM.Navigation.WPF.Dialogs;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Services;
using OutWit.Common.MVVM.Navigation.WPF.Utils;
using OutWit.Common.MVVM.WPF.Abstractions;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Utils
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WpfNavigationRegistrationTests
    {
        #region Registration Tests

        [Test]
        public void WpfDispatcherReplacesImmediateOneWhicheverOrderTest()
        {
            var afterCore = new ServiceCollection();
            afterCore.AddNavigation();
            afterCore.AddWpfNavigation();

            var beforeCore = new ServiceCollection();
            beforeCore.AddWpfNavigation();
            beforeCore.AddNavigation();

            using var first = afterCore.BuildServiceProvider();
            using var second = beforeCore.BuildServiceProvider();

            Assert.That(first.GetRequiredService<IDispatcher>(), Is.InstanceOf<WpfDispatcher>());
            Assert.That(second.GetRequiredService<IDispatcher>(), Is.InstanceOf<WpfDispatcher>());
        }

        [Test]
        public void DefaultsAreRegisteredTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddWpfNavigation();
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IDialogHost>(), Is.InstanceOf<DialogHostWindow>());
            Assert.That(provider.GetRequiredService<ITopLevelProvider>(), Is.InstanceOf<TopLevelProviderDefault>());
            Assert.That(provider.GetRequiredService<IApplicationResources>(), Is.InstanceOf<ApplicationResources>());
            Assert.That(provider.GetRequiredService<IViewFactory>(), Is.InstanceOf<ViewLocator>());
            Assert.That(provider.GetRequiredService<IDialogService>(), Is.Not.Null);
        }

        [Test]
        public void UseWpfViewLocatorPutsLocatorIntoApplicationResourcesTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddWpfNavigation();
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();
            var application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

            provider.UseWpfViewLocator(application);

            Assert.That(application.Resources[ViewLocator.RESOURCE_KEY], Is.SameAs(provider.GetRequiredService<ViewLocator>()));
            Assert.That(ViewLocator.FindInApplication(), Is.SameAs(provider.GetRequiredService<ViewLocator>()));
        }

        #endregion
    }
}
