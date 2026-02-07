using System.Reflection;

namespace OutWit.Common.Configuration
{
    /// <summary>
    /// Builder DTO holding assembly, file name, and environment for configuration construction.
    /// </summary>
    public sealed class ConfigurationInfo 
    {
        #region Constructors

        internal ConfigurationInfo(Assembly assembly)
        {
            Assembly = assembly;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the assembly whose directory is used as the base path.
        /// </summary>
        public Assembly Assembly { get;}
        
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
