using System;
using System.Linq;
using AspectInjector.Broker;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Aspects
{
    /// <summary>
    /// Aspect that intercepts property access on <see cref="SettingsContainer"/>
    /// implementations to read/write settings values automatically.
    /// </summary>
    [Aspect(Scope.PerInstance)]
    public sealed class SettingAspect
    {
        #region Functions

        /// <summary>
        /// Intercepts property getters to return the settings value
        /// from the appropriate scope (Default or User).
        /// </summary>
        /// <param name="source">The object instance being accessed.</param>
        /// <param name="propName">The property name.</param>
        /// <param name="injections">The trigger attributes.</param>
        /// <returns>The settings value, or <c>null</c> if not found.</returns>
        [Advice(Kind.Around, Targets = Target.AnyAccess | Target.Getter)]
        public object? Getter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<SettingAttribute>().SingleOrDefault();
            if (attribute == null)
                return null;

            if (source is not SettingsContainer container)
                return null;

            var group = attribute.ResolveGroup(container.GetType());
            var manager = container.SettingsManager;
            var collection = manager[group];

            if (!collection.ContainsKey(propName))
                return null;

            var value = collection[propName];

            return attribute.Scope == SettingsScope.Default
                ? value.DefaultValue
                : value.Value;
        }

        /// <summary>
        /// Intercepts property setters to update the settings value.
        /// Writes are ignored for Default-scoped properties.
        /// </summary>
        /// <param name="source">The object instance being accessed.</param>
        /// <param name="propName">The property name.</param>
        /// <param name="arguments">The setter arguments.</param>
        /// <param name="injections">The trigger attributes.</param>
        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void Setter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Arguments)] object[] arguments,
            [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<SettingAttribute>().SingleOrDefault();
            if (attribute == null)
                return;

            if (source is not SettingsContainer container)
                return;

            if (attribute.Scope == SettingsScope.Default)
                return;

            var group = attribute.ResolveGroup(container.GetType());
            var manager = container.SettingsManager;
            var collection = manager[group];

            if (!collection.ContainsKey(propName))
                return;

            if (arguments.Length == 0)
                return;

            var value = collection[propName];
            value.Value = arguments[0];
        }

        #endregion
    }
}
