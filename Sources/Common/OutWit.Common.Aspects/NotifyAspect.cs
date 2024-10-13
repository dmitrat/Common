using AspectInjector.Broker;
using System;
using System.ComponentModel;
using System.Linq;
using OutWit.Common.Aspects.Utils;

namespace OutWit.Common.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    [Injection(typeof(NotifyAspect))]
    public class Notify : Attribute
    {
        public string NotifyAlso { get; set; }
    }


    [Mixin(typeof(INotifyPropertyChanged))]
    [Aspect(Scope.PerInstance)]
    public class NotifyAspect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { };

        [Advice(Kind.After, Targets = Target.AnyAccess | Target.Setter)]
        public void AfterSetter(
            [Argument(Source.Instance)] object source,
            [Argument(Source.Name)] string propName,
            [Argument(Source.Triggers)] Attribute[] injections
        )
        {
            FirePropertyChanged(source, propName);

            foreach (var i in injections.OfType<Notify>().ToArray())
                FirePropertyChanged(source, i.NotifyAlso);
        }

        private void FirePropertyChanged(object source, string propertyName)
        {
            (source as INotifyPropertyChanged)?.FirePropertyChanged(propertyName);
            PropertyChanged(source, new PropertyChangedEventArgs(propertyName));
        }
    }
}
