using System;

namespace OutWit.Common.Configuration.Attributes
{
    /// <summary>
    /// Specifies a custom configuration section name for property binding via
    /// <see cref="Configuration.ConfigurationUtils.BindSettings{TSettings}"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigSectionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigSectionAttribute"/> class.
        /// </summary>
        /// <param name="name">The configuration section name to bind to.</param>
        public ConfigSectionAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the configuration section name.
        /// </summary>
        public string Name { get; }
    }
}
