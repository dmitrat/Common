using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using AspectInjector.Broker;

namespace OutWit.Common.MVVM.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(BindableAspect))]
    public class BindableAttribute : Attribute
    {
        public BindableAttribute(string property = null)
        {
            Property = property;
        }

        public string Property { get; }
    }

    [Aspect(Scope.PerInstance)]
    public class BindableAspect
    {
        [Advice(Kind.Around, Targets = Target.AnyAccess | Target.Getter)]
        public object Getter([Argument(Source.Instance)] object source, [Argument(Source.Name)] string propName, [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<BindableAttribute>().Single();

            var property = string.IsNullOrEmpty(attribute.Property) ? $"{propName}Property" : attribute.Property;

            var field = GetField(source.GetType(), property);

            var dependencyProperty = field?.GetValue(null) as DependencyProperty;
            var dependencyObject = source as DependencyObject;
            if (dependencyObject == null || dependencyProperty == null)
                return null;

            return dependencyObject.GetValue(dependencyProperty);
        }

        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void Setter([Argument(Source.Instance)] object source, [Argument(Source.Name)] string propName, [Argument(Source.Arguments)] object[] arguments, [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<BindableAttribute>().Single();

            var property = string.IsNullOrEmpty(attribute.Property) ? $"{propName}Property" : attribute.Property;
            
            var field = GetField(source.GetType(), property);

            //var field = type.GetField(property, BindingFlags.Public | BindingFlags.Static);
            //if (field == null && type.BaseType != null)
            //    field = type.BaseType.GetField(property, BindingFlags.Public | BindingFlags.Static);

            var dependencyProperty = field?.GetValue(null) as DependencyProperty;
            var dependencyObject = source as DependencyObject;
            if (dependencyObject == null || dependencyProperty == null)
                return;

            dependencyObject.SetValue(dependencyProperty, arguments.SingleOrDefault());
        }

        private FieldInfo? GetField(Type? type, string property)
        {
            while (type != null)
            {
                var field = type.GetField(property, BindingFlags.Public | BindingFlags.Static);
                if(field != null)
                    return field;

                type = type.BaseType;
            }

            return null;

        }
    }
}
