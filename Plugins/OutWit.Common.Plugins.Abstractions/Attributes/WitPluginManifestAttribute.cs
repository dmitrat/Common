using System;

namespace OutWit.Common.Plugins.Abstractions.Attributes
{
    /// <summary>
    /// Defines the essential metadata for a plugin, making it discoverable by the PluginLoader.
    /// This attribute is mandatory for any class that implements IPlugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class WitPluginManifestAttribute : Attribute
    {
        #region Constructors

        public WitPluginManifestAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Plugin name cannot be null or whitespace.", nameof(name));
            }
            Name = name;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            string priority = Priority < int.MaxValue 
                ? $"{Priority}" 
                : "Max";

            return $"Name: {Name}, Version: {Version}, Priority: {priority}";
        }

        #endregion

        #region Properties

        /// <summary>
        /// The unique name of the plugin. Used as a key for loading and unloading.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The semantic version of the plugin (e.g., "1.0.0" or a generated build version).
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// The loading and execution priority. Lower numbers are processed first.
        /// </summary>
        public int Priority { get; set; } = int.MaxValue;

        #endregion

    }
}
