using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Model;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Modules.Tests.Mock
{
    /// <summary>
    /// A well-behaved module: a service in phase one; a route, a view and a contribution in phase two.
    /// </summary>
    [WitPluginManifest("Summary")]
    public sealed class SummaryModule : UiModuleBase
    {
        #region Constants

        public const string ROUTE = "summary";
        public const string ZONE = "NavigationBar";

        #endregion

        #region Constructors

        public SummaryModule()
        {
        }

        public SummaryModule(ModuleCallLog log)
        {
            Log = log;
        }

        #endregion

        #region UiModuleBase

        public override void Initialize(IServiceCollection services)
        {
            Log?.Entries.Add("Summary.Initialize");
            services.AddSingleton<SummaryService>();
        }

        protected override void OnInitialized(UiModuleContext context)
        {
            Log?.Entries.Add("Summary.OnInitialized");
            Context = context;

            context.Routes.Register<SummaryViewModel>(ROUTE, metadata: "Feature.Summary");
            context.Views.Register<SummaryViewModel, SummaryView>();
            context.Contributions.Add(new ContributionItem
            {
                Zone = ZONE,
                Key = "Summary",
                Order = 200,
                Header = "Summary",
                RouteKey = ROUTE
            });
        }

        #endregion

        #region Properties

        public ModuleCallLog? Log { get; }

        public UiModuleContext? Context { get; private set; }

        #endregion
    }
}
