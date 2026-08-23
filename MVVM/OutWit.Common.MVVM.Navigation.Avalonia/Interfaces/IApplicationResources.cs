using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace OutWit.Common.MVVM.Navigation.Avalonia.Interfaces
{
    /// <summary>
    /// Lets a UI module add its styles, resource dictionaries, converters and icons to the
    /// running application from OnInitialized, without touching <c>Application.Current</c>
    /// itself.
    /// </summary>
    public interface IApplicationResources
    {
        #region Functions

        /// <summary>
        /// Adds a styles file (<c>avares://Module/Styles.axaml</c>) to the application styles.
        /// </summary>
        /// <param name="source">The avares URI.</param>
        void AddStyles(Uri source);

        /// <summary>
        /// Adds a style object to the application styles.
        /// </summary>
        /// <param name="style">The style.</param>
        void AddStyle(IStyle style);

        /// <summary>
        /// Merges a resource dictionary file (<c>avares://Module/Resources.axaml</c>) into the application resources.
        /// </summary>
        /// <param name="source">The avares URI.</param>
        void AddResources(Uri source);

        /// <summary>
        /// Merges a resource provider into the application resources.
        /// </summary>
        /// <param name="resources">The resources.</param>
        void AddResources(IResourceProvider resources);

        #endregion
    }
}
