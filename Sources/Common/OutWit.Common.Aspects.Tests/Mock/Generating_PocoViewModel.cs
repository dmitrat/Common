using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Aspects.Tests.Mock
{
    public class Generating_PocoViewModel
    {
        [NotifyAuto(NotifyAlso = nameof(Description))]
        public virtual int Value { get; set; }

        public string Description => $"The value is {Value}";
    }
}
