using System.Linq;
using System.Windows;
using OutWit.Common.MVVM.Navigation.WPF.Interfaces;

namespace OutWit.Common.MVVM.Navigation.WPF.Services
{
    /// <summary>
    /// The default <see cref="ITopLevelProvider"/>: the active window, the main window
    /// otherwise. This is the one place in the platform package that reads
    /// <c>Application.Current</c> for windows.
    /// </summary>
    public sealed class TopLevelProviderDefault : ITopLevelProvider
    {
        #region ITopLevelProvider

        public Window? GetActive()
        {
            var application = Application.Current;
            if (application == null)
                return null;

            return application.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                   ?? application.MainWindow;
        }

        #endregion
    }
}
