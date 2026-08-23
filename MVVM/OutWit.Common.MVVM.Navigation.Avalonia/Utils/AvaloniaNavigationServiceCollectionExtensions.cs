using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Common.MVVM.Avalonia.Abstractions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;
using OutWit.Common.MVVM.Navigation.Avalonia.Services;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Utils
{
    /// <summary>
    /// Registers the Avalonia half of navigation in <see cref="IServiceCollection"/>.
    /// </summary>
    public static class AvaloniaNavigationServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Registers the Avalonia dispatcher as <see cref="IDispatcher"/>, the
        /// <see cref="ViewLocator"/> (also as <see cref="IViewFactory"/>), the top-level provider,
        /// the application resources and the chosen <see cref="IDialogHost"/>. Works before or
        /// after <c>AddNavigation()</c>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Chooses the dialog host and the view naming convention.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// <code>
        /// services.AddNavigation(nav => nav.AddRoute&lt;StudiesViewModel&gt;(Routes.STUDIES));
        /// services.AddAvaloniaNavigation(o => o.UseOverlayDialogs());
        /// </code>
        /// </example>
        public static IServiceCollection AddAvaloniaNavigation(this IServiceCollection services, Action<AvaloniaNavigationOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new AvaloniaNavigationOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);

            // Replace, not TryAdd: AddNavigation may already have put DispatcherImmediate in,
            // and the real UI dispatcher must win whichever call came first.
            services.Replace(ServiceDescriptor.Singleton<IDispatcher>(_ => AvaloniaDispatcher.UIThread));

            services.TryAddSingleton<ITopLevelProvider, TopLevelProviderDefault>();
            services.TryAddSingleton<IApplicationResources, ApplicationResources>();
            services.TryAddSingleton<ViewLocator>();
            services.TryAddSingleton<IViewFactory>(provider => provider.GetRequiredService<ViewLocator>());
            services.TryAddSingleton(typeof(IDialogHost), options.DialogHostType);

            return services;
        }

        #endregion
    }
}
