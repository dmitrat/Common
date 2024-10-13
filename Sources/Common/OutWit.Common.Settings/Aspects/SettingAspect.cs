using System;
using System.Linq;
using AspectInjector.Broker;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(SettingAspect))]
    public class SettingAttribute : Attribute
    {
        public SettingAttribute(string collectionName) 
            : this(collectionName, SettingScope.User)
        {
        }

        public SettingAttribute(string collectionName, SettingScope scope)
        {
            CollectionName = collectionName;
            Scope = scope;
        }


        public SettingScope Scope { get; }
        public string CollectionName { get; }
    }

    public enum SettingScope
    {
        Default = 0,
        User = 1
    }

    [Aspect(Scope.PerInstance)]
    public class SettingAspect
    {
        [Advice(Kind.Around, Targets = Target.AnyAccess | Target.Getter)]
        public object Getter([Argument(Source.Instance)] object source, [Argument(Source.Name)] string propName, [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<SettingAttribute>().Single();

            var type = source.GetType();
            var collectionProperty = type.GetProperty(attribute.CollectionName);
            var collection = collectionProperty?.GetValue(source) as SettingsCollection;

            if (collection == null || !collection.ContainsKey(propName))
                return null;

            ISettingsValue setting = collection[propName];
            
            switch (attribute.Scope)
            {
                case SettingScope.User: 
                    return setting.HasUserValue? setting.UserValue: setting.DefaultValue;
                default: 
                    return setting.DefaultValue;
            }
        }

        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void Setter([Argument(Source.Instance)] object source, [Argument(Source.Name)] string propName, [Argument(Source.Arguments)] object[] arguments, [Argument(Source.Triggers)] Attribute[] injections)
        {
            var attribute = injections.OfType<SettingAttribute>().Single();

            var type = source.GetType();
            var collectionProperty = type.GetProperty(attribute.CollectionName);
            var collection = collectionProperty?.GetValue(source) as SettingsCollection;

            if (collection == null || !collection.ContainsKey(propName))
                return;

            ISettingsValue setting = collection[propName];

            switch (attribute.Scope)
            {
                case SettingScope.User: 
                    if(setting.HasUserValue)
                        setting.UserValue = arguments.SingleOrDefault();
                    break;
                default:
                    if(setting.HasDefaultValue)
                        setting.DefaultValue = arguments.SingleOrDefault();
                    break;
            }
        }
    }
}
