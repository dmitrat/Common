using OutWit.Common.Interfaces;

namespace OutWit.Common.Plugins.Model
{
    /// <summary>
    /// Snapshot of where the plugin loader found and registered a single
    /// plugin on disk. Carries the plugin's manifest name, the full path
    /// to its DLL, and the directory the DLL was loaded from — what the
    /// loader considers the plugin's "home" for sibling-file lookups
    /// (config files, embedded assets next to the DLL, etc.).
    /// </summary>
    /// <remarks>
    /// Implements <see cref="IAssemblyContext"/> so that
    /// <c>OutWit.Common.Configuration.ConfigurationUtils.For(IAssemblyContext)</c>
    /// can resolve a plugin's <c>appsettings.json</c> against
    /// <see cref="IAssemblyContext.HomeDirectory"/> instead of
    /// <see cref="System.Reflection.Assembly.Location"/>, which can
    /// disagree when the runtime resolved the assembly from a
    /// project-reference graph copy or a shared
    /// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> entry
    /// rather than from the path the loader scanned.
    /// </remarks>
    public sealed class WitPluginHostContext : IAssemblyContext
    {
        #region Constructors

        public WitPluginHostContext(string name, string assemblyPath, string homeDirectory)
        {
            Name = name;
            AssemblyPath = assemblyPath;
            HomeDirectory = homeDirectory;
        }

        #endregion

        #region Properties

        public string Name { get; }

        public string AssemblyPath { get; }

        public string HomeDirectory { get; }

        #endregion
    }
}
