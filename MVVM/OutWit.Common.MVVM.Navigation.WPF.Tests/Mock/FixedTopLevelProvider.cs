using System.Windows;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;

namespace OutWit.Common.MVVM.Navigation.WPF.Tests.Mock
{
    /// <summary>
    /// An ITopLevelProvider that returns the window a test created.
    /// </summary>
    public sealed class FixedTopLevelProvider : ITopLevelProvider
    {
        public FixedTopLevelProvider(Window? window)
        {
            Window = window;
        }

        public Window? Window { get; set; }

        public Window? GetActive()
        {
            return Window;
        }
    }
}
