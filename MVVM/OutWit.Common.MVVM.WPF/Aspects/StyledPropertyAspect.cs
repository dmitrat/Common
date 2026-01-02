using System;
using System.Reflection;
using System.Windows;
using AspectInjector.Broker;

namespace OutWit.Common.MVVM.WPF.Aspects
{
    /// <summary>
    /// Aspect that transforms properties marked with [StyledProperty] to use DependencyProperty GetValue/SetValue.
    /// Works in conjunction with the source generator that creates the DependencyProperty fields.
    /// </summary>
    [Aspect(Scope.PerInstance)]
    public class StyledPropertyAspect
    {
        #region Constants

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
            if (instance is not DependencyObject dependencyObject)
                return target(args);

            var dependencyProperty = FindDependencyProperty(instance.GetType(), propertyName);
            if (dependencyProperty == null)
                return target(args);

            return dependencyObject.GetValue(dependencyProperty);
        }

        [Advice(Kind.Around, Targets = Target.Public | Target.Setter)]
        public object? SetValue(
            [Argument(Source.Instance)] object instance,
            [Argument(Source.Name)] string propertyName,
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args)
        {
            if (instance is not DependencyObject dependencyObject)
                return target(args);

            var dependencyProperty = FindDependencyProperty(instance.GetType(), propertyName);
            if (dependencyProperty == null)
                return target(args);

            if (args.Length > 0)
                dependencyObject.SetValue(dependencyProperty, args[0]);

            return null;
        }

        #endregion

        #region Functions

        private static DependencyProperty? FindDependencyProperty(Type type, string propertyName)
        {
            var fieldName = $"{propertyName}Property";
            var field = type.GetField(fieldName, PROPERTY_FIELD_FLAGS);

            return field?.GetValue(null) as DependencyProperty;
        }

        #endregion
    }
}
