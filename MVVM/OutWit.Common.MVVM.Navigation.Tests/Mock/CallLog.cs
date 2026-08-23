using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.MVVM.Navigation.Tests.Mock
{
    /// <summary>
    /// Shared, DI-provided call recorder: view models and guards append "Who.What" here so
    /// tests can assert call order.
    /// </summary>
    public sealed class CallLog
    {
        #region Fields

        private readonly object m_sync = new();
        private readonly List<string> m_entries = new();

        #endregion

        #region Functions

        public void Add(string entry)
        {
            lock (m_sync)
                m_entries.Add(entry);
        }

        public int Count(string entry)
        {
            lock (m_sync)
                return m_entries.Count(candidate => candidate == entry);
        }

        public void Clear()
        {
            lock (m_sync)
                m_entries.Clear();
        }

        #endregion

        #region Properties

        public IReadOnlyList<string> Entries
        {
            get
            {
                lock (m_sync)
                    return m_entries.ToArray();
            }
        }

        #endregion
    }
}
