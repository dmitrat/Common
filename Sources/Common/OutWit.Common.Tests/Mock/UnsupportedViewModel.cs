using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Tests.Mock
{
    public class UnsupportedViewModel : INotifyPropertyChanged
    {
        private readonly EventHandlerList _events = new();
        private static readonly object PropertyChangedKey = new();

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _events.AddHandler(PropertyChangedKey, value);
            remove => _events.RemoveHandler(PropertyChangedKey, value);
        }
    }
}
