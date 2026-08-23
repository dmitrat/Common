using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Modules;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.MVVM.Navigation.Sample.Core;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Sample.Module.WPF
{
    /// <summary>
    /// A UI module that ships as a DLL. The application does not reference this assembly:
    /// its build output is copied into <c>@Modules/audit.module/</c> and
    /// <see cref="UiModules"/> finds it there through <c>OutWit.Common.Plugins</c>.
    /// </summary>
    /// <remarks>
    /// Compare it with the Avalonia module of the same name: the module class, the route and
    /// the contribution are identical, and only the view differs. That is the seam — a module
    /// that ships screens is platform-specific, everything above the screens is not.
    /// </remarks>
    [WitPluginManifest("Audit", Version = "1.0.0")]
    public sealed class AuditModule : UiModuleBase
    {
        #region UiModuleBase

        public override void Initialize(IServiceCollection services)
        {
            // phase one: the container is still open, and a module with services registers
            // them here. This one has none.
        }

        protected override void OnInitialized(UiModuleContext context)
        {
            context.Routes.Register<AuditViewModel>(AuditRoutes.MAIN);
            context.Views.Register<AuditViewModel, AuditView>();

            context.Contributions.Add(new ContributionItem
            {
                Zone = Zones.NAVIGATION_BAR,
                Key = "Audit",
                Order = 500,
                Header = "Audit",
                Icon = "🧩",
                RouteKey = AuditRoutes.MAIN
            });
        }

        #endregion
    }
}
