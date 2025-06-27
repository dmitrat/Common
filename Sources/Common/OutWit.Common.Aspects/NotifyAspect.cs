using AspectInjector.Broker;
using System;
using System.ComponentModel;
using System.Linq;
using OutWit.Common.Utils;

namespace OutWit.Common.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(NotifyAspect))]
    public class NotifyAttribute : Attribute
    {
        public string NotifyAlso { get; set; }
    }

    [Aspect(Scope.PerInstance)]
    public class NotifyAspect
    {
        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void AfterSetter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Triggers)] Attribute[] triggers)
        {
            FirePropertyChanged(source, propName);

            foreach (var notify in triggers.OfType<NotifyAttribute>().ToArray())
            {
                if(string.IsNullOrEmpty(notify.NotifyAlso))
                    continue;
                
                FirePropertyChanged(source, notify.NotifyAlso);
            }
        }

        private void FirePropertyChanged(object source, string propertyName)
        {
            (source as INotifyPropertyChanged)?.FirePropertyChanged(propertyName);
        }
    }
}
