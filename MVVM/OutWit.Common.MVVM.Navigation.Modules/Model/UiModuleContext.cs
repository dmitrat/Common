using System;
using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Modules.Model
{
    /// <summary>
    /// What a module gets in its second phase: the built container and the registries it
    /// contributes to, already resolved.
    /// </summary>
    public sealed class UiModuleContext
    {
        #region Constructors

        /// <summary>
        /// Resolves the registries from the container.
        /// </summary>
        /// <param name="services">The built container.</param>
        public UiModuleContext(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Navigation = services.GetRequiredService<INavigationService>();
            Routes = services.GetRequiredService<IRouteRegistry>();
            Views = services.GetRequiredService<IViewRegistry>();
            Contributions = services.GetRequiredService<IContributionRegistry>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The built container, for anything not covered below.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// The navigation service — outlets, and navigation if a module needs to trigger one.
        /// </summary>
        public INavigationService Navigation { get; }

        /// <summary>
        /// Where routes go.
        /// </summary>
        public IRouteRegistry Routes { get; }

        /// <summary>
        /// Where explicit view mappings go; optional where the platform convention finds the view.
        /// </summary>
        public IViewRegistry Views { get; }

        /// <summary>
        /// Where navigation bar entries, menu items and toolbar buttons go.
        /// </summary>
        public IContributionRegistry Contributions { get; }

        #endregion
    }
}
