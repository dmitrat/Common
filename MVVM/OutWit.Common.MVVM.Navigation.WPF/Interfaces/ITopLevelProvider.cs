using System.Windows;

namespace OutWit.Common.MVVM.Navigation.WPF.Interfaces
{
    /// <summary>
    /// Where the dialog host finds the window to own its dialogs. The default looks at
    /// <c>Application.Current.Windows</c>; an application can register its own — a shell
    /// window that knows itself, for instance.
    /// </summary>
    public interface ITopLevelProvider
    {
        #region Functions

        /// <summary>
        /// The active window, or null when the application has none yet.
        /// </summary>
        /// <returns>The window.</returns>
        Window? GetActive();

        #endregion
    }
}
