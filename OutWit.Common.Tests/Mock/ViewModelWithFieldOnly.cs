using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.Tests.Mock
{
    public class ViewModelWithFieldOnly : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
    }

}
