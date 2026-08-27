using System.Collections.Generic;
using System.Linq;
using OutWit.Common.MVVM.Navigation.Model;

namespace OutWit.Common.MVVM.Navigation.Services
{
    /// <summary>
    /// Where each outlet last was in each group. Deliberately not the journal: ClearHistory
    /// empties the journal, and "the page this section was left at" has to survive that,
    /// because it is not history — it is where the section currently is.
    /// </summary>
    internal sealed class NavigationGroupMemory
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly Dictionary<(string Outlet, string Group), NavigationEntry> m_entries = new();

        #endregion

        #region Functions

        /// <summary>
        /// Records the page for every group it belongs to. Called once a navigation has
        /// committed, whatever its kind: going Back to a page is being at that page.
        /// </summary>
        public void Remember(string outlet, string routeKey, NavigationParameters parameters, IReadOnlyList<NavigationGroup> groups)
        {
            NavigationEntry? entry = null;

            lock (m_sync)
            {
                foreach (var group in groups)
                {
                    if (!group.Contains(routeKey))
                        continue;

                    entry ??= new NavigationEntry(routeKey, parameters);
                    m_entries[(outlet, group.Key)] = entry;
                }
            }
        }

        /// <summary>
        /// The page remembered for the outlet in the group; null when none.
        /// </summary>
        public NavigationEntry? Recall(string outlet, string groupKey)
        {
            lock (m_sync)
                return m_entries.TryGetValue((outlet, groupKey), out var entry) ? entry : null;
        }

        /// <summary>
        /// Forgets. Null narrows nothing: no group means every group, no outlet means every outlet.
        /// </summary>
        public void Forget(string? groupKey, string? outlet)
        {
            lock (m_sync)
            {
                if (groupKey == null && outlet == null)
                {
                    m_entries.Clear();
                    return;
                }

                var keys = m_entries.Keys
                    .Where(key => (groupKey == null || key.Group == groupKey) && (outlet == null || key.Outlet == outlet))
                    .ToArray();

                foreach (var key in keys)
                    m_entries.Remove(key);
            }
        }

        #endregion
    }
}
