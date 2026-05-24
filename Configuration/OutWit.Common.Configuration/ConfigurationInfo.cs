using System.IO;
using System.Reflection;
using OutWit.Common.Interfaces;

namespace OutWit.Common.Configuration
{
    /// <summary>
    /// Builder DTO holding the base path (assembly directory or
    /// <see cref="IAssemblyContext"/>-supplied home directory),
    /// file name, and environment for configuration construction.
    /// </summary>
    /// <remarks>
    /// Construct via one of the <see cref="ConfigurationUtils.For(Assembly)"/>
    /// / <see cref="ConfigurationUtils.For(IAssemblyContext)"/> overloads;
    /// extend via the <see cref="ConfigurationUtils.WithFileName"/> /
    /// <see cref="ConfigurationUtils.WithEnvironment(ConfigurationInfo, string)"/>
    /// chain; finalize via <see cref="ConfigurationUtils.Build"/>.
    /// </remarks>
    public sealed class ConfigurationInfo
    {
        #region Constructors

        internal ConfigurationInfo(Assembly assembly)
        {
            Assembly = assembly;
            BasePath = Path.GetDirectoryName(assembly.Location);
        }

        internal ConfigurationInfo(IAssemblyContext context)
        {
            Assembly = null;
            BasePath = context.HomeDirectory;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The assembly the configuration is anchored to, when the
        /// instance was constructed via
        /// <see cref="ConfigurationUtils.For(Assembly)"/>.
        /// Null when the instance was constructed via
        /// <see cref="ConfigurationUtils.For(IAssemblyContext)"/>
        /// (the context carries the path directly; no
        /// <see cref="System.Reflection.Assembly"/> reference is held).
        /// </summary>
        public Assembly? Assembly { get; }

        /// <summary>
        /// Absolute path to the directory the configuration file lookup
        /// uses as its base. Derived from
        /// <c>Path.GetDirectoryName(Assembly.Location)</c> in the
        /// assembly-based ctor, or from <see cref="IAssemblyContext.HomeDirectory"/>
        /// in the context-based ctor.
        /// </summary>
        public string? BasePath { get; }

        /// <summary>
        /// Gets or sets the base configuration file name (without extension).
        /// </summary>
        public string? FileName { get; internal set; }

        /// <summary>
        /// Gets or sets the environment name for environment-specific overrides.
        /// </summary>
        public string? Environment { get; internal set; }

        #endregion
    }
}
