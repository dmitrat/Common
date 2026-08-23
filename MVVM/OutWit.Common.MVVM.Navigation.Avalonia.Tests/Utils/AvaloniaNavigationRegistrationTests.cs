using Avalonia.Headless.NUnit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutWit.Common.MVVM.Abstractions;
using OutWit.Common.MVVM.Avalonia.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Dialogs;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Services;
using OutWit.Common.MVVM.Navigation.Avalonia.Utils;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Utils;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Tests.Utils
{
    [TestFixture]
    public class AvaloniaNavigationRegistrationTests
    {
        #region Registration Tests

        [AvaloniaTest]
        public void AvaloniaDispatcherReplacesImmediateOneWhicheverOrderTest()
        {
            var afterCore = new ServiceCollection();
            afterCore.AddNavigation();
            afterCore.AddAvaloniaNavigation();

            var beforeCore = new ServiceCollection();
            beforeCore.AddAvaloniaNavigation();
            beforeCore.AddNavigation();

            using var first = afterCore.BuildServiceProvider();
            using var second = beforeCore.BuildServiceProvider();

            Assert.That(first.GetRequiredService<IDispatcher>(), Is.InstanceOf<AvaloniaDispatcher>());
            Assert.That(second.GetRequiredService<IDispatcher>(), Is.InstanceOf<AvaloniaDispatcher>());
        }

        [Test]
        public void WindowDialogsAreTheDefaultTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddAvaloniaNavigation();
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IDialogHost>(), Is.InstanceOf<DialogHostWindow>());
            Assert.That(provider.GetRequiredService<ITopLevelProvider>(), Is.InstanceOf<TopLevelProviderDefault>());
            Assert.That(provider.GetRequiredService<IApplicationResources>(), Is.InstanceOf<ApplicationResources>());
            Assert.That(provider.GetRequiredService<IViewFactory>(), Is.InstanceOf<ViewLocator>());
            Assert.That(provider.GetRequiredService<IDialogService>(), Is.Not.Null);
        }

        [Test]
        public void OverlayDialogsCanBeChosenTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddAvaloniaNavigation(o => o.UseOverlayDialogs());
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IDialogHost>(), Is.InstanceOf<DialogHostOverlay>());
        }

        [Test]
        public void CustomDialogHostCanBeChosenTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddAvaloniaNavigation(o => o.UseDialogHost<CustomDialogHost>());
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();

            Assert.That(provider.GetRequiredService<IDialogHost>(), Is.InstanceOf<CustomDialogHost>());
        }

        [AvaloniaTest]
        public void UseAvaloniaViewLocatorAddsTemplateOnceTest()
        {
            var services = new ServiceCollection();
            services.AddNavigation();
            services.AddAvaloniaNavigation();
            services.AddSingleton<IDispatcher>(DispatcherImmediate.Instance);
            using var provider = services.BuildServiceProvider();
            var application = global::Avalonia.Application.Current!;
            var before = application.DataTemplates.Count;

            provider.UseAvaloniaViewLocator();
            provider.UseAvaloniaViewLocator();

            try
            {
                Assert.That(application.DataTemplates.Count, Is.EqualTo(before + 1));
                Assert.That(application.DataTemplates, Does.Contain(provider.GetRequiredService<ViewLocator>()));
            }
            finally
            {
                application.DataTemplates.Remove(provider.GetRequiredService<ViewLocator>());
            }
        }

        #endregion

        #region Classes

        private sealed class CustomDialogHost : IDialogHost
        {
            public bool SupportsNesting => true;

            public bool IsOpen(string host) => false;

            public System.Threading.Tasks.Task ShowAsync(string host, object view, System.Func<System.Threading.Tasks.Task<bool>> canDismiss, System.Threading.CancellationToken cancellation)
                => System.Threading.Tasks.Task.CompletedTask;

            public void Close(string host)
            {
            }
        }

        #endregion
    }
}
