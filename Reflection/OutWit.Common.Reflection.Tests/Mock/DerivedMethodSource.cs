using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Tests.Mock
{
    internal class DerivedMethodSource : BaseMethodSource, IMethodSource
    {
        public void DerivedMethod() { }
        public void InterfaceMethod() { }
    }
}
