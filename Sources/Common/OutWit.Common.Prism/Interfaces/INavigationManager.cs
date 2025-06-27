using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OutWit.Common.Prism.Interfaces
{
    public interface INavigationManager
    {
        event LoadedEventHandler Loaded;
        event NavigationChangedEventHandler NavigationChanged;

        void Register<TNavigation>(string key) where TNavigation : INavigationControl, new();
        void Register<TView>();
        void Register<TMenuItem>(MenuRoots menuRoot) where TMenuItem : Control, new();

        void Navigate(INavigationControl navigationCtrl, string parameter = "");
        void NavigateAsync(INavigationControl navigationCtrl, string parameter = "");

        void Navigate<TView>();
        void NavigateAsync<TView>();

        void Navigate(string key, string parameter);
        void NavigateAsync(string key, string parameter);


        bool CanNavigate(INavigationControl navigationCtrl, string parameter = "");
        bool CanNavigate(string key, string parameter);

        bool IsOnScreen<TView>();

        void NavigateFirst();

        void LockNavigation();
        void UnlockNavigation();

        void Refresh();


        INavigationControl Current { get; }
    }

    public enum MenuRoots
    {
        File = 0,
        View,
        Windows
    }

    public delegate void LoadedEventHandler();
    public delegate void NavigationChangedEventHandler(INavigationControl sender);
}
