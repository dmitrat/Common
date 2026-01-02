using System;
using System.Reflection;
using Avalonia;
using AspectInjector.Broker;

namespace OutWit.Common.MVVM.Avalonia.Aspects
{
    /// <summary>
    /// Aspect that transforms properties marked with [DirectProperty] to use the generated backing field.
    /// Works in conjunction with the source generator that creates the DirectProperty fields and backing fields.
    /// </summary>
    [Aspect(Scope.PerInstance)]
    public class DirectPropertyAspect
    {
        #region Constants

        private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags PROPERTY_FIELD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        #endregion

        #region Advice

        [Advice(Kind.Around, Targets = Target.Public | Target.Getter)]
        public object? GetValue(
            [Argument(Source.Instance)] object instance,
            [Argument(Source.Name)] string propertyName,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args)
        {
            if (instance is not AvaloniaObject avaloniaObject)
                return target(args);

            var directProperty = FindDirectProperty(instance.GetType(), propertyName);
            if (directProperty == null)
                return target(args);

            return avaloniaObject.GetValue(directProperty);
        }

        [Advice(Kind.Around, Targets = Target.Public | Target.Setter)]
        public object? SetValue(
            [Argument(Source.Instance)] object instance,
            [Argument(Source.Name)] string propertyName,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args)
        {
            if (instance is not AvaloniaObject avaloniaObject)
                return target(args);

            var directProperty = FindDirectProperty(instance.GetType(), propertyName);
            if (directProperty == null)
                return target(args);

            if (args.Length > 0)
                avaloniaObject.SetValue(directProperty, args[0]);

            return null;
        }

        #endregion

        #region Functions

        private static AvaloniaProperty? FindDirectProperty(Type type, string propertyName)
        {
            var fieldName = $"{propertyName}Property";
            var field = type.GetField(fieldName, PROPERTY_FIELD_FLAGS);

            return field?.GetValue(null) as AvaloniaProperty;
        }

        #endregion
    }
}
