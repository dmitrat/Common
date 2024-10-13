using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Prism.Navigation;

namespace OutWit.Common.Prism.Interfaces
{
    public interface INavigationContext : INotifyPropertyChanged
    {
        bool CanNavigate(string parameter);

        void Navigate(INavigationParameters parameters);
        void Navigate();

        bool IsSelected { get; set; }
        bool IsEnabled { get; set; }
        bool IsVisible { get; set; }
        bool IsHidden { get; set; }
        bool IsLoaded { get; set; }
        bool IsDefault { get; }
    }
}
