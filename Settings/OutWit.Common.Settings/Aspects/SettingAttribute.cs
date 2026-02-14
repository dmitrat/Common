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
        /// Creates a new setting attribute with default User scope.
        /// The group name is inferred from the declaring container class name at runtime.
        /// </summary>
        public SettingAttribute()
        {
            Group = null;
            Scope = SettingsScope.User;
        }

        /// <summary>
        /// Creates a new setting attribute with the specified scope.
        /// The group name is inferred from the declaring container class name at runtime.
        /// </summary>
        /// <param name="scope">The scope to read from (Default, User or Global).</param>
        public SettingAttribute(SettingsScope scope)
        {
            Group = null;
            Scope = scope;
        }

        /// <summary>
        /// Creates a new setting attribute with an explicit group name.
        /// </summary>
        /// <param name="group">The settings group this property belongs to.</param>
        /// <param name="scope">The scope to read from (Default, User or Global).</param>
        public SettingAttribute(string group, SettingsScope scope = SettingsScope.User)
        {
            Group = group;
            Scope = scope;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Resolves the effective group name. Returns <see cref="Group"/> if explicitly set,
        /// otherwise falls back to the container type name.
        /// </summary>
        /// <param name="containerType">The declaring container type.</param>
        /// <returns>The resolved group name.</returns>
        internal string ResolveGroup(Type containerType)
        {
            return Group ?? containerType.Name;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings group name, or <c>null</c> if inferred from the container class name.
        /// </summary>
        public string? Group { get; }

        /// <summary>
        /// Gets the scope to read from.
        /// </summary>
        public SettingsScope Scope { get; }

        #endregion
    }
}
