using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;

namespace OutWit.Common.Settings.Tests.Utils
{
    internal sealed class MemorySettingsProvider : ISettingsProvider, ISettingsGroupInfoProvider
    {
        #region Fields

        private readonly Dictionary<string, List<SettingsEntry>> m_data = new();
        private List<SettingsGroupInfo> m_groupInfos = new();

        #endregion

        #region Constructors

        public MemorySettingsProvider(bool isReadOnly = false)
        {
            IsReadOnly = isReadOnly;
        }

        #endregion

        #region ISettingsProvider

        public IReadOnlyList<SettingsEntry> Read(string group)
        {
            return m_data.TryGetValue(group, out var entries)
                ? entries.ToList()
                : new List<SettingsEntry>();
        }

        public void Write(string group, IReadOnlyList<SettingsEntry> entries)
        {
            if (entries.Count == 0)
                m_data.Remove(group);
            else
                m_data[group] = entries.ToList();
        }

        public IReadOnlyList<string> GetGroups()
        {
            return m_data.Where(p => p.Value.Count > 0).Select(p => p.Key).ToList();
        }

        #endregion

        #region ISettingsGroupInfoProvider

        public IReadOnlyList<SettingsGroupInfo> ReadGroupInfo()
        {
            return m_groupInfos.ToList();
        }

        public void WriteGroupInfo(IReadOnlyList<SettingsGroupInfo> groups)
        {
            m_groupInfos = groups.ToList();
        }

        #endregion

        #region Functions

        public void Delete()
        {
            if (IsReadOnly)
                return;

            m_data.Clear();
            m_groupInfos.Clear();
        }

        public void AddEntry(string group, SettingsEntry entry)
        {
            if (!m_data.ContainsKey(group))
                m_data[group] = new List<SettingsEntry>();

            entry.Group = group;
            m_data[group].Add(entry);
        }

        public void AddGroupInfo(SettingsGroupInfo info)
        {
            m_groupInfos.Add(info);
        }

        #endregion

        #region Properties

        public bool IsReadOnly { get; }

        #endregion
    }
}
