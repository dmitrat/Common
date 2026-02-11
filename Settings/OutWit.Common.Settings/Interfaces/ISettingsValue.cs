using System.ComponentModel;
using OutWit.Common.Settings.Configuration;

namespace OutWit.Common.Settings.Interfaces
{
    public interface ISettingsValue : INotifyPropertyChanged
    {
        /// <summary>
        /// Unique identifier within a group.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Display name (for UI). Defaults to Key.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Serializer kind identifier, e.g. "String", "Boolean", "Enum".
        /// Determines which UI control to use.
        /// </summary>
        string ValueKind { get; }

        /// <summary>
        /// The active value (loaded from the appropriate scope provider).
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// The default value (from Default provider). Read-only.
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// True when Value equals DefaultValue.
        /// </summary>
        bool IsDefault { get; }

        /// <summary>
        /// The scope this setting belongs to (Default, User, or Global).
        /// </summary>
        SettingsScope Scope { get; }

        /// <summary>
        /// Whether this setting should be hidden from UI.
        /// </summary>
        bool Hidden { get; }

        /// <summary>
        /// Additional metadata (e.g. fully qualified enum type name).
        /// </summary>
        string Tag { get; }

        /// <summary>
        /// Value-based equality check.
        /// </summary>
        bool Is(ISettingsValue other);
    }
}
