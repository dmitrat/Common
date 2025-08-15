using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Plugins.Abstractions.Attributes
{
    /// <summary>
    /// Specifies a dependency on another plugin. This attribute can be used multiple times.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class WitPluginDependencyAttribute: Attribute
    {
        #region Constructors

        public WitPluginDependencyAttribute(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName))
            {
                throw new ArgumentException("Dependency plugin name cannot be null or whitespace.", nameof(pluginName));
            }
            PluginName = pluginName;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return string.IsNullOrEmpty(MinimumVersion) 
                ? $"PluginName: {PluginName}" 
                : $"PluginName: {PluginName}, MinimumVersion: {MinimumVersion}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The name of the required plugin, matching its [PluginName].
        /// </summary>
        public string PluginName { get; }

        /// <summary>
        /// The minimum required version of the dependency (e.g., "1.2.0").
        /// If null, any version is accepted.
        /// </summary>
        public string? MinimumVersion { get; set; }

        #endregion

    }
}
