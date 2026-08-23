using System.Collections.ObjectModel;
using System.ComponentModel;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// A named, ordered, observable collection of contributions — what Prism regions were
    /// used for when nobody navigated into them. Bind an ItemsControl to <see cref="Items"/>.
    /// </summary>
    public interface IContributionZone : INotifyPropertyChanged
    {
        #region Functions

        /// <summary>
        /// Finds an item at any depth.
        /// </summary>
        /// <param name="key">The item key.</param>
        /// <returns>The item, or null.</returns>
        ContributionItem? Find(string key);

        #endregion

        #region Properties

        /// <summary>
        /// The zone name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Top-level items, sorted by <see cref="ContributionItem.Order"/> and then by
        /// insertion order. Changes are raised on the UI thread.
        /// </summary>
        ReadOnlyObservableCollection<ContributionItem> Items { get; }

        /// <summary>
        /// The item whose route and parameters its outlet currently shows, or null.
        /// </summary>
        ContributionItem? Selected { get; }

        #endregion
    }
}
