using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Reflection.Tests.Mock
{
    internal class TestPublisher
    {
        public event EventHandler<EventArgs>? StandardEvent;
        public event Action<int, string>? CustomEvent;
        public event Action? SimpleEvent;

        public void RaiseStandardEvent() => StandardEvent?.Invoke(this, EventArgs.Empty);
        public void RaiseCustomEvent(int number, string text) => CustomEvent?.Invoke(number, text);
        public void RaiseSimpleEvent() => SimpleEvent?.Invoke();
    }
}
