namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// How the view model behind a route is created and kept.
    /// </summary>
    public enum NavigationRouteMode
    {
        /// <summary>
        /// The view model is created once per outlet and reused for every navigation
        /// to the route. This is how Prism regions behaved, hence the default.
        /// </summary>
        Cached = 0,

        /// <summary>
        /// A new view model is created for every navigation, in its own DI scope.
        /// The previous instance and its scope are disposed once the new one is shown.
        /// </summary>
        Transient
    }
}
