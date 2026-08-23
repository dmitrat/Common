using System;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Avalonia.Services;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Utils
{
    /// <summary>
    /// Start-up helpers over a built container.
    /// </summary>
    public static class AvaloniaNavigationServiceProviderExtensions
    {
        #region Functions

        /// <summary>
        /// Adds the container's <see cref="ViewLocator"/> to the application's DataTemplates,
        /// so that every ContentControl — outlets without a NavigationOutlet control, zone
        /// widgets, dialog content — finds its views. Idempotent. The locator comes from DI
        /// rather than XAML so that views with constructor dependencies resolve.
        /// </summary>
        /// <param name="provider">The built container.</param>
        /// <param name="application">The application; null means <c>Application.Current</c>.</param>
        /// <returns>The provider.</returns>
        public static IServiceProvider UseAvaloniaViewLocator(this IServiceProvider provider, Application? application = null)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var target = application
                         ?? Application.Current
                         ?? throw new InvalidOperationException("Application.Current is null: call UseAvaloniaViewLocator once the application has initialized.");

            var locator = provider.GetRequiredService<ViewLocator>();

            if (!target.DataTemplates.Contains(locator))
                target.DataTemplates.Add(locator);

            return provider;
        }

        #endregion
    }
}
