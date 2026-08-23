namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// Why a navigation is happening. Lets a view model tell a fresh arrival from
    /// a journal move or a refresh without being told through parameters.
    /// </summary>
    public enum NavigationKind
    {
        /// <summary>
        /// A new navigation: a journal entry is written.
        /// </summary>
        New = 0,

        /// <summary>
        /// Moving back in the outlet's journal.
        /// </summary>
        Back,

        /// <summary>
        /// Moving forward in the outlet's journal.
        /// </summary>
        Forward,

        /// <summary>
        /// Re-entering the current route with the current parameters.
        /// </summary>
        Refresh
    }
}
