using Microsoft.Extensions.DependencyInjection;
using OutWit.Common.MVVM.Navigation.Modules.Model;
using OutWit.Common.Plugins.Abstractions.Attributes;

namespace OutWit.Common.MVVM.Navigation.Modules.Tests.Mock
{
    /// <summary>
    /// A second well-behaved module, to observe ordering and survival next to a broken one.
    /// </summary>
    [WitPluginManifest("Second")]
    public sealed class SecondModule : UiModuleBase
    {
        #region Constructors

        public SecondModule(ModuleCallLog log)
        {
            Log = log;
        }

        #endregion

        #region UiModuleBase

        public override void Initialize(IServiceCollection services)
        {
            Log.Entries.Add("Second.Initialize");
        }

        protected override void OnInitialized(UiModuleContext context)
        {
            Log.Entries.Add("Second.OnInitialized");
        }

        #endregion

        #region Properties

        public ModuleCallLog Log { get; }

        #endregion
    }
}
