using System;
using System.Windows;

namespace OutWit.Common.MVVM.Navigation.WPF.Interfaces
{
    /// <summary>
    /// Lets a UI module add its resource dictionaries — styles, templates, converters,
    /// brushes — to the running application from OnInitialized, without touching
    /// <c>Application.Current</c> itself.
    /// </summary>
    public interface IApplicationResources
    {
        #region Functions

        /// <summary>
        /// Merges a resource dictionary file (<c>pack://application:,,,/Module;component/Resources.xaml</c>)
        /// into the application resources.
        /// </summary>
        /// <param name="source">The pack URI.</param>
        void AddResources(Uri source);

        /// <summary>
        /// Merges a resource dictionary into the application resources.
        /// </summary>
        /// <param name="resources">The dictionary.</param>
        void AddResources(ResourceDictionary resources);

        #endregion
    }
}
