using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;
using OutWit.Common.MVVM.Navigation.WPF.Services;
using OutWit.Common.MVVM.WPF.Abstractions;

namespace OutWit.Common.MVVM.Navigation.WPF.Utils
{
    /// <summary>
    /// Registers the WPF half of navigation in <see cref="IServiceCollection"/>.
    /// </summary>
    public static class WpfNavigationServiceCollectionExtensions
    {
        #region Functions

        /// <summary>
        /// Registers the WPF dispatcher as <see cref="IDispatcher"/>, the <see cref="ViewLocator"/>
        /// (also as <see cref="IViewFactory"/>), the top-level provider, the application resources
        /// and the chosen <see cref="IDialogHost"/>. Works before or after <c>AddNavigation()</c>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Chooses the dialog host and the view naming convention.</param>
        /// <returns>The service collection.</returns>
        /// <example>
        /// <code>
        /// services.AddNavigation(nav => nav.AddRoute&lt;StudiesViewModel&gt;(Routes.STUDIES));
        /// services.AddWpfNavigation();
        /// </code>
        /// </example>
        public static IServiceCollection AddWpfNavigation(this IServiceCollection services, Action<WpfNavigationOptions>? configure = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var options = new WpfNavigationOptions();
            configure?.Invoke(options);

            services.AddSingleton(options);

            // Replace, not TryAdd: AddNavigation may already have put DispatcherImmediate in,
            // and the real UI dispatcher must win whichever call came first.
            services.Replace(ServiceDescriptor.Singleton<IDispatcher>(_ =>
                new WpfDispatcher(Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher)));

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
