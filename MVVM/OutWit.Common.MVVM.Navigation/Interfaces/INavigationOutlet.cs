using System.Collections.Generic;
using System.ComponentModel;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// A place with one active view model and a journal. Bindable straight from XAML:
    /// a ContentControl on <see cref="Content"/>, or the platform's NavigationOutlet
    /// control on the outlet itself.
    /// </summary>
    public interface INavigationOutlet : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The outlet name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The current view model, or null when nothing has been shown yet.
        /// </summary>
        object? Content { get; }

        /// <summary>
        /// The route of the current view model.
        /// </summary>
        NavigationRoute? Route { get; }

        /// <summary>
        /// Shortcut for <c>Route?.Key</c>.
        /// </summary>
        string? RouteKey { get; }

        /// <summary>
        /// The parameters the current view model was navigated to with. Never null.
        /// </summary>
        NavigationParameters Parameters { get; }

        /// <summary>
        /// True while a navigation into this outlet is in progress.
        /// </summary>
        bool IsNavigating { get; }

        /// <summary>
        /// True when the journal has an entry before the current one.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// True when the journal has an entry after the current one.
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// The journal, oldest first.
        /// </summary>
        IReadOnlyList<NavigationEntry> History { get; }

        /// <summary>
        /// Index of the current entry in <see cref="History"/>, or -1 when the journal is empty.
        /// </summary>
        int HistoryIndex { get; }

        #endregion
    }
}
