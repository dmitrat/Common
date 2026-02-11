using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Providers
{
    /// <summary>
    /// Represents a single settings entry with group, key, value, and metadata.
    /// </summary>
    public class SettingsEntry : ModelBase
    {
        #region Constructors

        public SettingsEntry()
        {
            Group = "";
            Key = "";
            Value = "";
            ValueKind = "";
            Tag = "";
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{Group}] {Key} = {Value} ({ValueKind})";
        }

        #endregion

        #region ModelBase

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SettingsEntry other)
                return false;

            return Group.Is(other.Group) &&
                   Key.Is(other.Key) &&
                   Value.Is(other.Value) &&
                   ValueKind.Is(other.ValueKind) &&
                   Tag.Is(other.Tag) &&
                   Hidden.Is(other.Hidden);
        }

        /// <inheritdoc />
        public override ModelBase Clone()
        {
            return new SettingsEntry
            {
                Group = Group,
                Key = Key,
                Value = Value,
                ValueKind = ValueKind,
                Tag = Tag,
                Hidden = Hidden
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the group this entry belongs to.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Gets or sets the unique key within the group.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the serialized string value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the serializer kind (e.g. "String", "Boolean", "Enum").
        /// </summary>
        public string ValueKind { get; set; }

        /// <summary>
        /// Gets or sets additional metadata (e.g. fully qualified enum type name).
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets whether this entry should be hidden from UI.
        /// </summary>
        public bool Hidden { get; set; }

        #endregion
    }
}
