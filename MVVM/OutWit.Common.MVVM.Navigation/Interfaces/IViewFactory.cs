using System;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Builds a view for a view model. Implemented by the platform's view locator
    /// (<see cref="IViewRegistry"/> first, naming convention second). The core needs it
    /// in two places: the dialog service builds a dialog's view, and
    /// <c>ValidateNavigation()</c> checks at start-up that every route has a view.
    /// </summary>
    public interface IViewFactory
    {
        #region Functions

        /// <summary>
        /// Tells whether a view can be built for the view model type.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <returns>True when a view is known.</returns>
        bool CanBuild(Type viewModelType);

        /// <summary>
        /// Builds a view for the view model. The view's DataContext is set to the view model.
        /// </summary>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The view.</returns>
        /// <exception cref="InvalidOperationException">No view is known for the view model's type.</exception>
        object Build(object viewModel);

        #endregion
    }
}
