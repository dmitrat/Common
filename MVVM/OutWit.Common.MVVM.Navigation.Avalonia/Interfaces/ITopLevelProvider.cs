using Avalonia.Controls;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Interfaces
{
    /// <summary>
    /// Where dialog hosts find the window or view to show on top of. The default looks at
    /// <c>Application.Current.ApplicationLifetime</c>; an application can register its own —
    /// a shell window that knows itself, for instance.
    /// </summary>
    public interface ITopLevelProvider
    {
        #region Functions

        /// <summary>
        /// The active top level, or null when the application has none yet.
        /// </summary>
        /// <returns>The top level.</returns>
        TopLevel? GetActive();

        #endregion
    }
}
