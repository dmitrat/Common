using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Aspects.Tests.Mock
{
    public class Augmenting_InheritedViewModel : Augmenting_BaseViewModel
    {
        [Notify(NotifyAlso = nameof(CalculatedProperty))]
        public virtual string SourceProperty { get; set; }

        public string CalculatedProperty => $"Derived from: {SourceProperty}";
    }
}
