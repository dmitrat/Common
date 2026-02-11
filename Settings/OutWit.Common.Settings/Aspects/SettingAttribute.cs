using System;
using AspectInjector.Broker;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Aspects
{
    /// <summary>
    /// Marks a property as a settings-backed value.
    /// The property getter/setter will be intercepted by <see cref="SettingAspect"/>
    /// to read/write from the settings manager.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(SettingAspect))]
    public sealed class SettingAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        /// Creates a new setting attribute.
        /// </summary>
        /// <param name="group">The settings group this property belongs to.</param>
        /// <param name="scope">The scope to read from (Default or User).</param>
        public SettingAttribute(string group, SettingsScope scope = SettingsScope.User)
        {
            Group = group;
            Scope = scope;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings group name.
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Gets the scope to read from.
        /// </summary>
        public SettingsScope Scope { get; }

        #endregion
    }
}
