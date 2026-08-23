using System.Collections.Generic;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Interfaces
{
    /// <summary>
    /// Where modules put their navigation bar entries, menu items and toolbar buttons.
    /// A singleton; every call can be made from any thread — collection changes are
    /// applied on the UI thread through the dispatcher.
    /// </summary>
    public interface IContributionRegistry
    {
        #region Functions

        /// <summary>
        /// Adds an item to its zone, creating the zone on first use. An item with the same
        /// key in the same zone is replaced. When the item has a route key, a navigation
        /// command is attached.
        /// </summary>
        /// <param name="item">The item.</param>
        void Add(ContributionItem item);

        /// <summary>
        /// Adds several items.
        /// </summary>
        /// <param name="items">The items.</param>
        void AddRange(IEnumerable<ContributionItem> items);

        /// <summary>
        /// Removes an item from a zone. Its children stay attached to it until their
        /// parent key is registered again.
        /// </summary>
        /// <param name="zone">The zone name.</param>
        /// <param name="key">The item key.</param>
        /// <returns>True when an item was removed.</returns>
        bool Remove(string zone, string key);

        /// <summary>
        /// Removes every item from a zone.
        /// </summary>
        /// <param name="zone">The zone name.</param>
        void Clear(string zone);

        /// <summary>
        /// Gets a zone, creating it on first use.
        /// </summary>
        /// <param name="name">The zone name.</param>
        /// <returns>The zone.</returns>
        IContributionZone Zone(string name);

        /// <summary>
        /// Finds an item at any depth of a zone.
        /// </summary>
        /// <param name="zone">The zone name.</param>
        /// <param name="key">The item key.</param>
        /// <returns>The item, or null.</returns>
        ContributionItem? Find(string zone, string key);

        #endregion

        #region Properties

        /// <summary>
        /// Names of all zones.
        /// </summary>
        IReadOnlyList<string> Zones { get; }

        #endregion
    }
}
