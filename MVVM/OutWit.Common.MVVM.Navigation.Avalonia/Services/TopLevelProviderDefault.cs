using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OutWit.Common.MVVM.Navigation.Avalonia.Interfaces;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Services
{
    /// <summary>
    /// The default <see cref="ITopLevelProvider"/>: the active window of a desktop
    /// application, its main window otherwise, or the top level of a single-view
    /// application. This is the one place in the platform package that reads
    /// <c>Application.Current</c>.
    /// </summary>
    public sealed class TopLevelProviderDefault : ITopLevelProvider
    {
        #region ITopLevelProvider

        public TopLevel? GetActive()
        {
            switch (Application.Current?.ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    return desktop.Windows.FirstOrDefault(window => window.IsActive) ?? desktop.MainWindow;

                case ISingleViewApplicationLifetime single:
                    return single.MainView != null ? TopLevel.GetTopLevel(single.MainView) : null;

                default:
                    return null;
            }
        }

        #endregion
    }
}
