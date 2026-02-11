using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Settings.Providers;
using OutWit.Common.Settings.Samples.Service.Contracts;

namespace OutWit.Common.Settings.Samples.Service.Services
{
    /// <summary>
    /// Server-side implementation of <see cref="ISettingsService"/>
    /// that operates on aggregated settings managers.
    /// </summary>
    public sealed class SettingsServiceImpl : ISettingsService
    {
        #region Fields

        private readonly List<ISettingsManager> m_managers;

        #endregion

        #region Constructors

        public SettingsServiceImpl(List<ISettingsManager> managers)
        {
            m_managers = managers;
        }

        #endregion

        #region ISettingsService

        public Task<IReadOnlyList<SettingsGroupInfo>> GetGroupsAsync()
        {
            var groups = m_managers
                .SelectMany(m => m.Collections)
                .OrderBy(c => c.Priority)
                .Where(c => c.Any(v => !v.Hidden))
                .Select(c => new SettingsGroupInfo
                {
                    Group = c.Group,
                    DisplayName = c.DisplayName,
                    Priority = c.Priority
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<SettingsGroupInfo>>(groups);
        }

        public Task<IReadOnlyList<ISettingsValue>> GetValuesAsync(string group)
        {
            var values = m_managers
                .SelectMany(m => m.Collections)
                .Where(c => c.Group == group)
                .SelectMany(c => c)
                .Where(v => !v.Hidden)
                .ToList();

            return Task.FromResult<IReadOnlyList<ISettingsValue>>(values);
        }

        public Task UpdateValueAsync(string group, string key, string value)
        {
            var settingsValue = FindValue(group, key);
            if (settingsValue != null)
                settingsValue.Value = settingsValue.Value is string ? value : ParseValue(settingsValue, value);

            return Task.CompletedTask;
        }

        public Task ResetValueAsync(string group, string key)
        {
            var settingsValue = FindValue(group, key);
            if (settingsValue != null)
                settingsValue.Value = settingsValue.DefaultValue;

            return Task.CompletedTask;
        }

        public Task ResetGroupAsync(string group)
        {
            foreach (var manager in m_managers)
            {
                foreach (var collection in manager.Collections)
                {
                    if (collection.Group != group)
                        continue;

                    foreach (var value in collection)
                        value.Value = value.DefaultValue;
                }
            }

            return Task.CompletedTask;
        }

        public Task SaveAsync()
        {
            foreach (var manager in m_managers)
                manager.Save();

            return Task.CompletedTask;
        }

        #endregion

        #region Tools

        private ISettingsValue? FindValue(string group, string key)
        {
            foreach (var manager in m_managers)
            {
                foreach (var collection in manager.Collections)
                {
                    if (collection.Group != group)
                        continue;

                    if (collection.ContainsKey(key))
                        return collection[key];
                }
            }

            return null;
        }

        private static object ParseValue(ISettingsValue settingsValue, string stringValue)
        {
            var currentValue = settingsValue.Value;

            return currentValue switch
            {
                int => int.TryParse(stringValue, out var i) ? i : currentValue,
                long => long.TryParse(stringValue, out var l) ? l : currentValue,
                double => double.TryParse(stringValue, out var d) ? d : currentValue,
                decimal => decimal.TryParse(stringValue, out var dc) ? dc : currentValue,
                bool => bool.TryParse(stringValue, out var b) ? b : currentValue,
                TimeSpan => TimeSpan.TryParse(stringValue, out var ts) ? ts : currentValue,
                Enum => TryParseEnum(settingsValue.Tag, stringValue, currentValue),
                _ => stringValue
            };
        }

        private static object TryParseEnum(string tag, string value, object fallback)
        {
            if (string.IsNullOrEmpty(tag))
                return fallback;

            var type = Type.GetType(tag);
            if (type != null && type.IsEnum && Enum.TryParse(type, value, out var result))
                return result!;

            return fallback;
        }

        #endregion
    }
}
