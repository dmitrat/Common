using System;
using System.Reflection;
using Avalonia;
using AspectInjector.Broker;

namespace OutWit.Common.MVVM.Avalonia.Aspects
{
    /// <summary>
    /// Aspect that transforms static properties marked with [AttachedProperty] to use AvaloniaProperty GetValue/SetValue.
    /// Works in conjunction with the source generator that creates the AttachedProperty fields and Get/Set methods.
    /// </summary>
    [Aspect(Scope.Global)]
    public class AttachedPropertyAspect
    {
        #region Constants

        private const BindingFlags PROPERTY_FIELD_FLAGS = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        #endregion

        #region Advice

        [Advice(Kind.Around, Targets = Target.Public | Target.Static | Target.Getter)]
        public object? GetValue(
            [Argument(Source.Type)] Type type,
            [Argument(Source.Name)] string propertyName,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args)
        {
            // For attached properties, the getter just returns the default - real access through Get{PropertyName} method
            var attachedProperty = FindAttachedProperty(type, propertyName);
            if (attachedProperty == null)
                return target(args);

            // Return the default value from the AvaloniaProperty metadata
            var metadata = attachedProperty.GetMetadata(typeof(AvaloniaObject));
            return metadata.GetType().GetProperty("DefaultValue")?.GetValue(metadata);
        }

        [Advice(Kind.Around, Targets = Target.Public | Target.Static | Target.Setter)]
        public object? SetValue(
            [Argument(Source.Type)] Type type,
            [Argument(Source.Name)] string propertyName,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args)
        {
            // For attached properties, the setter is a no-op - real access through Set{PropertyName} method
            return null;
        }

        #endregion

        #region Functions

        private static AvaloniaProperty? FindAttachedProperty(Type type, string propertyName)
        {
            var fieldName = $"{propertyName}Property";
            var field = type.GetField(fieldName, PROPERTY_FIELD_FLAGS);

            return field?.GetValue(null) as AvaloniaProperty;
        }

        #endregion
    }
}
