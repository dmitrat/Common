using AspectInjector.Broker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using OutWit.Common.Utils;

namespace OutWit.Common.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(NotifyAutoAspect))]
    public class NotifyAutoAttribute : Attribute
    {
        public string NotifyAlso { get; set; }
    }

    [Mixin(typeof(INotifyPropertyChanged))]
    [Aspect(Scope.PerInstance)]
    public class NotifyAutoAspect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { };

        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void AfterSetter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Triggers)] Attribute[] triggers
        )
        {
            FirePropertyChanged(source, propName);

            foreach (var notify in triggers.OfType<NotifyAutoAttribute>().ToArray())
            {
                if(string.IsNullOrEmpty(notify.NotifyAlso))
                    continue;
                
                FirePropertyChanged(source, notify.NotifyAlso);
            }
        }

        private void FirePropertyChanged(object source, string propertyName)
        {
            PropertyChanged(source, new PropertyChangedEventArgs(propertyName));
        }
    }
}
