using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Navigation.Interfaces;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// The state behind <see cref="IContributionZone"/>: the sorted tree of items and the
    /// selection. Mutated on the UI thread only — the registry sees to that.
    /// </summary>
    internal sealed class ContributionZone : NotifyPropertyChangedBase, IContributionZone
    {
        #region Fields

        private readonly ObservableCollection<ContributionItem> m_items = new();
        private readonly Dictionary<string, ContributionItem> m_all = new(StringComparer.Ordinal);
        private readonly List<ContributionItem> m_orphans = new();

        private long m_sequence;

        #endregion

        #region Constructors

        public ContributionZone(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Zone name must be a non-empty string.", nameof(name));

            Name = name;
            Items = new ReadOnlyObservableCollection<ContributionItem>(m_items);
        }

        #endregion

        #region Functions

        /// <summary>
        /// Adds an item, replacing one with the same key. Returns the replaced item, if any.
        /// </summary>
        public ContributionItem? Add(ContributionItem item)
        {
            ContributionItem? replaced = null;

            if (m_all.TryGetValue(item.Key, out var existing))
            {
                replaced = existing;
                Detach(existing);
            }

            item.Sequence = ++m_sequence;
            m_all[item.Key] = item;

            if (item.ParentKey == null)
                InsertSorted(m_items, item);
            else if (m_all.TryGetValue(item.ParentKey, out var parent))
                InsertSorted(parent.ChildrenInternal, item);
            else
                m_orphans.Add(item);

            for (var i = m_orphans.Count - 1; i >= 0; i--)
            {
                if (m_orphans[i].ParentKey != item.Key)
                    continue;

                var orphan = m_orphans[i];
                m_orphans.RemoveAt(i);
                InsertSorted(item.ChildrenInternal, orphan);
            }

            UpdateSelected();

            return replaced;
        }

        /// <summary>
        /// Removes an item. Its children become orphans and re-attach when the key is registered again.
        /// </summary>
        public ContributionItem? Remove(string key)
        {
            if (!m_all.TryGetValue(key, out var item))
                return null;

            Detach(item);
            UpdateSelected();

            return item;
        }

        public IReadOnlyList<ContributionItem> Clear()
        {
            var removed = m_all.Values.ToArray();

            foreach (var item in removed)
            {
                item.IsSelected = false;
                item.ChildrenInternal.Clear();
            }

            m_items.Clear();
            m_orphans.Clear();
            m_all.Clear();

            Selected = null;

            return removed;
        }

        public ContributionItem? Find(string key)
        {
            return key != null && m_all.TryGetValue(key, out var item) ? item : null;
        }

        /// <summary>
        /// Aligns IsSelected of every item targeting the outlet with what the outlet shows.
        /// </summary>
        public void UpdateSelection(INavigationOutlet outlet, Func<string, string?> defaultOutletOf)
        {
            foreach (var item in m_all.Values)
            {
                if (item.RouteKey == null)
                    continue;

                var itemOutlet = item.Outlet ?? defaultOutletOf(item.RouteKey) ?? NavigationOutlets.MAIN;
                if (itemOutlet != outlet.Name)
                    continue;

                item.IsSelected = item.RouteKey == outlet.RouteKey
                                  && (item.Parameters == null || item.Parameters.Is(outlet.Parameters));
            }

            UpdateSelected();
        }

        private void Detach(ContributionItem item)
        {
            m_all.Remove(item.Key);

            if (!m_items.Remove(item) && !m_orphans.Remove(item) && item.ParentKey != null
                && m_all.TryGetValue(item.ParentKey, out var parent))
                parent.ChildrenInternal.Remove(item);

            foreach (var child in item.ChildrenInternal.ToArray())
            {
                item.ChildrenInternal.Remove(child);
                m_orphans.Add(child);
            }

            item.IsSelected = false;
        }

        private void UpdateSelected()
        {
            Selected = m_all.Values
                .Where(item => item.IsSelected)
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Sequence)
                .FirstOrDefault();
        }

        private static void InsertSorted(ObservableCollection<ContributionItem> list, ContributionItem item)
        {
            var index = 0;

            while (index < list.Count && Compare(list[index], item) <= 0)
                index++;

            list.Insert(index, item);
        }

        private static int Compare(ContributionItem left, ContributionItem right)
        {
            var byOrder = left.Order.CompareTo(right.Order);
            return byOrder != 0 ? byOrder : left.Sequence.CompareTo(right.Sequence);
        }

        #endregion

        #region Properties

        public string Name { get; }

        public ReadOnlyObservableCollection<ContributionItem> Items { get; }

        [Notify]
        public ContributionItem? Selected { get; private set; }

        public IReadOnlyCollection<ContributionItem> All => m_all.Values;

        #endregion
    }
}
