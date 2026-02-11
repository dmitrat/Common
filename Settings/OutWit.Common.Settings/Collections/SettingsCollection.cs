using System.Collections;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Collections
{
    /// <summary>
    /// A named collection of settings values belonging to a single group.
    /// </summary>
    public sealed class SettingsCollection : ModelBase, IEnumerable<ISettingsValue>
    {
        #region Fields

        private readonly Dictionary<string, ISettingsValue> m_values = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new settings collection for the specified group.
        /// </summary>
        /// <param name="group">The group name.</param>
        /// <param name="displayName">Optional display name; defaults to group name if empty.</param>
        /// <param name="priority">Display priority (lower values appear first).</param>
        public SettingsCollection(string group, string displayName = "", int priority = 0)
        {
            Group = group;
            DisplayName = string.IsNullOrEmpty(displayName) ? group : displayName;
            Priority = priority;
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{DisplayName} ({m_values.Count} settings)";
        }

        /// <summary>
        /// Determines whether the collection contains a setting with the specified key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
        public bool ContainsKey(string key)
        {
            return m_values.ContainsKey(key);
        }

        internal void Add(ISettingsValue value)
        {
            m_values[value.Key] = value;
        }

        #endregion

        #region ModelBase

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SettingsCollection other)
                return false;

            return Group.Is(other.Group);
        }

        /// <inheritdoc />
        public override ModelBase Clone()
        {
            var clone = new SettingsCollection(Group, DisplayName, Priority);

            foreach (var value in m_values.Values)
                clone.Add(value);

            return clone;
        }

        #endregion

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator<ISettingsValue> GetEnumerator()
        {
            return m_values.Values.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the settings value for the specified key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public ISettingsValue this[string key] => m_values[key];

        /// <summary>
        /// Gets the group name.
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Gets or sets the display name for this group.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the display priority (lower values appear first).
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets the number of settings in this collection.
        /// </summary>
        public int Count => m_values.Count;

        #endregion
    }
}
