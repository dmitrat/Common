using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Tests.Mock
{
    internal class DerivedEventSource : BaseEventSource, IEventSource
    {
        public event Action<int, bool> DerivedEvent;

        // Explicit implementation of the interface event
        event Action<string> IEventSource.InterfaceEvent
        {
            add { /* dummy */ }
            remove { /* dummy */ }
        }
    }
}
