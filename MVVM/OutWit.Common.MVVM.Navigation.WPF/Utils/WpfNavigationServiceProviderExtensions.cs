using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.ViewModels;
using OutWit.Common.MVVM.Navigation.WPF.Dialogs;
using OutWit.Common.MVVM.Navigation.WPF.Services;

namespace OutWit.Common.MVVM.Navigation.WPF.Utils
{
    /// <summary>
    /// Start-up helpers over a built container.
    /// </summary>
    public static class WpfNavigationServiceProviderExtensions
    {
        #region Functions

        /// <summary>
        /// Puts the container's <see cref="ViewLocator"/> into the application resources under
        /// <see cref="ViewLocator.RESOURCE_KEY"/>, where <c>NavigationOutlet</c> and
        /// <c>ViewPresenter</c> find it and where XAML can reach it as a template selector:
        /// <c>ContentTemplateSelector="{StaticResource OutWit.Navigation.ViewLocator}"</c>.
        /// The locator comes from DI rather than XAML so that views with constructor
        /// dependencies resolve. Also registers the built-in progress dialog view, unless the
        /// application has already registered one of its own.
        /// </summary>
        /// <param name="provider">The built container.</param>
        /// <param name="application">The application; null means <c>Application.Current</c>.</param>
        /// <returns>The provider.</returns>
        public static IServiceProvider UseWpfViewLocator(this IServiceProvider provider, Application? application = null)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var target = application
                         ?? Application.Current
                         ?? throw new InvalidOperationException("Application.Current is null: call UseWpfViewLocator once the application has started.");

            target.Resources[ViewLocator.RESOURCE_KEY] = provider.GetRequiredService<ViewLocator>();

            // the progress dialog works out of the box; an application that registered its own
            // view for it before this call keeps it
            var views = provider.GetRequiredService<IViewRegistry>();
            if (!views.Contains(typeof(ProgressDialogViewModel)))
                views.Register<ProgressDialogViewModel, ProgressDialogView>();

            return provider;
        }

        #endregion
    }
}
