using System;
using System.Collections.Generic;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Utils
{
    /// <summary>
    /// The view-from-view-model naming convention both platform locators share. Looks inside
    /// the view model's own assembly — never <c>Type.GetType</c>, which does not see module
    /// assemblies. The platform decides what counts as a view through the predicate.
    /// </summary>
    public static class ViewNaming
    {
        #region Constants

        public const string VIEW_MODELS_SEGMENT = ".ViewModels.";
        public const string VIEWS_SEGMENT = ".Views.";
        public const string VIEW_MODEL_SUFFIX = "ViewModel";
        public const string VIEW_SUFFIX = "View";

        #endregion

        #region Functions

        /// <summary>
        /// The full type names a view model type maps to, most specific first.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="convention">The convention.</param>
        /// <returns>Candidate view type names; empty when the type does not end with "ViewModel" or the convention is None.</returns>
        public static IEnumerable<string> Candidates(Type viewModelType, ViewNamingConvention convention)
        {
            if (viewModelType == null)
                throw new ArgumentNullException(nameof(viewModelType));

            var fullName = viewModelType.FullName;

            if (convention == ViewNamingConvention.None
                || fullName == null
                || !fullName.EndsWith(VIEW_MODEL_SUFFIX, StringComparison.Ordinal))
                yield break;

            var suffixSwapped = fullName.Substring(0, fullName.Length - VIEW_MODEL_SUFFIX.Length) + VIEW_SUFFIX;

            if (convention == ViewNamingConvention.ViewModelsToViews && suffixSwapped.Contains(VIEW_MODELS_SEGMENT))
                yield return suffixSwapped.Replace(VIEW_MODELS_SEGMENT, VIEWS_SEGMENT);

            yield return suffixSwapped;
        }

        /// <summary>
        /// Finds the first candidate type that exists in the view model's assembly and that
        /// <paramref name="isView"/> accepts.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="convention">The convention.</param>
        /// <param name="isView">Tells whether a type is a view on this platform.</param>
        /// <returns>The view type, or null.</returns>
        public static Type? FindViewType(Type viewModelType, ViewNamingConvention convention, Func<Type, bool> isView)
        {
            if (isView == null)
                throw new ArgumentNullException(nameof(isView));

            foreach (var candidate in Candidates(viewModelType, convention))
            {
                var type = viewModelType.Assembly.GetType(candidate, throwOnError: false);
                if (type != null && isView(type))
                    return type;
            }

            return null;
        }

        #endregion
    }
}
