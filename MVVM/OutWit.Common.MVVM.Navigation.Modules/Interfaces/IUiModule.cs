using OutWit.Common.Plugins.Abstractions.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Modules.Interfaces
{
    /// <summary>
    /// A UI module: a plugin that, besides services, registers routes, views and
    /// contributions. Mark implementations with <c>[WitPluginManifest("Name")]</c> so the
    /// folder loader finds them; derive from <see cref="UiModuleBase"/> for the typed
    /// OnInitialized.
    /// </summary>
    public interface IUiModule : IWitPlugin
    {
    }
}
