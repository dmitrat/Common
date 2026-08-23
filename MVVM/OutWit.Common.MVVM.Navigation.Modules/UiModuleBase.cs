using System;
using OutWit.Common.MVVM.Navigation.Modules.Interfaces;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.Plugins.Abstractions;

namespace OutWit.Common.MVVM.Navigation.Modules
{
    /// <summary>
    /// Base class for UI modules. Override <see cref="WitPluginBase.Initialize"/> to register
    /// services (phase one, before the container is built) and
    /// <see cref="OnInitialized(UiModuleContext)"/> to register routes, views, contributions
    /// and resources (phase two, with the container built). View models are not registered:
    /// navigation creates them with ActivatorUtilities.
    /// </summary>
    /// <example>
    /// <code>
    /// [WitPluginManifest("Summary")]
    /// public class SummaryModule : UiModuleBase
    /// {
    ///     public override void Initialize(IServiceCollection services)
    ///     {
    ///         services.AddSingleton&lt;ISummaryService, SummaryService&gt;();
    ///     }
    ///
    ///     protected override void OnInitialized(UiModuleContext context)
    ///     {
    ///         context.Routes.Register&lt;SummaryViewModel&gt;(SummaryRoutes.GENERAL);
    ///         context.Contributions.Add(new ContributionItem { Zone = Zones.NAVIGATION_BAR, Key = "Summary", Order = 200, Header = "Summary", RouteKey = SummaryRoutes.GENERAL });
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class UiModuleBase : WitPluginBase, IUiModule
    {
        #region WitPluginBase

        public sealed override void OnInitialized(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            OnInitialized(new UiModuleContext(serviceProvider));
        }

        #endregion

        #region Functions

        /// <summary>
        /// Phase two: the container is built, the registries are ready.
        /// </summary>
        /// <param name="context">The container and the registries.</param>
        protected abstract void OnInitialized(UiModuleContext context);

        #endregion
    }
}
