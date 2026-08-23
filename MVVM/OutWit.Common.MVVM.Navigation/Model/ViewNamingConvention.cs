namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// How a platform view locator finds a view for a view model type that is not
    /// in the view registry. The lookup is by name inside the view model's own assembly —
    /// never <c>Type.GetType</c>, which does not see module assemblies.
    /// </summary>
    public enum ViewNamingConvention
    {
        /// <summary>
        /// <c>App.ViewModels.StudyViewModel</c> → <c>App.Views.StudyView</c>; when that does not
        /// exist, <c>App.ViewModels.StudyView</c> (same namespace) is tried too.
        /// </summary>
        ViewModelsToViews = 0,

        /// <summary>
        /// <c>StudyViewModel</c> → <c>StudyView</c> in the same namespace only.
        /// </summary>
        SameNamespace,

        /// <summary>
        /// No convention: only the view registry. Use under trimming or AOT.
        /// </summary>
        None
    }
}
