using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Explicit view model to view mapping. Lives in the core and knows nothing about
    /// Control: one registry, one future source generator and one test suite for both
    /// platforms. The platform's view locator consults it first and falls back to a
    /// naming convention; under trimming or AOT this registry is the only path.
    /// </summary>
    public interface IViewRegistry
    {
        #region Functions

        /// <summary>
        /// Maps a view model type to a view type. The view is created through
        /// ActivatorUtilities, so it may take dependencies in its constructor.
        /// </summary>
        /// <typeparam name="TViewModel">The view model type.</typeparam>
        /// <typeparam name="TView">The view type.</typeparam>
        void Register<TViewModel, TView>()
            where TViewModel : class
            where TView : class;

        /// <summary>
        /// Maps a view model type to a view type. The view is created through
        /// ActivatorUtilities, so it may take dependencies in its constructor.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="viewType">The view type.</param>
        void Register(Type viewModelType, Type viewType);

        /// <summary>
        /// Maps a view model type to a view factory.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="factory">Builds the view from the service provider.</param>
        void Register(Type viewModelType, Func<IServiceProvider, object> factory);

        /// <summary>
        /// Tells whether a mapping exists for the view model type.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <returns>True when registered.</returns>
        bool Contains(Type viewModelType);

        /// <summary>
        /// Gets the view factory for a view model type.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="factory">The factory.</param>
        /// <returns>True when registered.</returns>
        bool TryGetFactory(Type viewModelType, [NotNullWhen(true)] out Func<IServiceProvider, object>? factory);

        #endregion

        #region Properties

        /// <summary>
        /// All registered view model types.
        /// </summary>
        IReadOnlyCollection<Type> ViewModelTypes { get; }

        #endregion
    }
}
