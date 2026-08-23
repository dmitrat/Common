using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Modules;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.MVVM.Navigation.Sample.Core.ViewModels;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Sample.Core.Modules
{
    /// <summary>
    /// A UI module. In the sample it is compiled into the application, but nothing here says
    /// so: the same class dropped into <c>@Modules/reports.module/</c> as a DLL would be found
    /// by the folder loader and behave identically. It adds a route, a navigation bar entry
    /// and a menu item without the shell knowing it exists.
    /// </summary>
    /// <remarks>
    /// The view is registered by each platform's sample, since the view models live in this
    /// shared assembly and the views do not: the naming convention looks inside the view
    /// model's own assembly, so a cross-assembly pair needs an explicit registration.
    /// </remarks>
    [WitPluginManifest("Reports", Version = "1.0.0")]
    public sealed class ReportsModule : UiModuleBase
    {
        #region UiModuleBase

        public override void Initialize(IServiceCollection services)
        {
            // phase one: the container is still open. A real module registers its services here.
        }

        protected override void OnInitialized(UiModuleContext context)
        {
            context.Routes.Register<ReportsViewModel>(Routes.REPORTS, metadata: "Feature.Reports");

            context.Contributions.Add(new ContributionItem
            {
                Zone = Zones.NAVIGATION_BAR,
                Key = "Reports",
                Order = 300,
                Header = "Reports",
                Icon = "📊",
                RouteKey = Routes.REPORTS
            });

            context.Contributions.Add(new ContributionItem
            {
                Zone = Zones.MENU_FILE,
                Key = "File.Reports",
                ParentKey = "File.Open",
                Order = 20,
                Header = "Reports…",
                RouteKey = Routes.REPORTS
            });
        }

        #endregion
    }
}
