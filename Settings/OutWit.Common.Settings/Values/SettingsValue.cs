using System;
using System.Collections.Generic;
using System.ComponentModel;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Settings.Configuration;
using OutWit.Common.Settings.Interfaces;
using OutWit.Common.Utils;
using OutWit.Common.Values;

namespace OutWit.Common.Settings.Values
{
    /// <summary>
    /// Strongly-typed settings value that tracks default and active values.
    /// </summary>
    /// <typeparam name="T">The CLR type of the setting.</typeparam>
    [MemoryPackable]
    public sealed partial class SettingsValue<T> : ModelBase, ISettingsValue
    {
        #region Constructors

        /// <summary>
        /// Creates a new settings value with default and active values.
        /// </summary>
        /// <param name="key">Unique key within the group.</param>
        /// <param name="valueKind">Serializer kind identifier.</param>
        /// <param name="tag">Additional metadata (e.g. enum type name).</param>
        /// <param name="hidden">Whether this setting should be hidden from UI.</param>
        /// <param name="scope">The scope this setting belongs to.</param>
        /// <param name="serializer">The serializer for this value type.</param>
        /// <param name="defaultValue">The default value from the Default provider.</param>
        /// <param name="value">The active value from the appropriate scope provider.</param>
        public SettingsValue(string key, string valueKind, string tag, bool hidden,
                             SettingsScope scope, ISettingsSerializer serializer,
                             T defaultValue, T value)
        {
            Key = key;
            Name = key;
            ValueKind = valueKind;
            Tag = tag;
            Hidden = hidden;
            Scope = scope;
            Serializer = serializer;
            DefaultValue = defaultValue;
            Value = value;

            IsDefault = ComputeIsDefault();

            PropertyChanged += OnPropertyChanged;
        }

        /// <summary>
        /// MemoryPack deserialization constructor (client-side, no serializer).
        /// </summary>
        [MemoryPackConstructor]
        public SettingsValue(string key, string name, string valueKind, string tag,
                             bool hidden, SettingsScope scope,
                             T defaultValue, T value, bool isDefault)
        {
            Key = key;
            Name = name;
            ValueKind = valueKind;
            Tag = tag;
            Hidden = hidden;
            Scope = scope;
            DefaultValue = defaultValue;
            Value = value;
            IsDefault = isDefault;

            PropertyChanged += OnPropertyChanged;
        }

        #endregion

        #region Functions

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Key} = {Value} ({ValueKind})";
        }

        /// <summary>
        /// Compares this value with another <see cref="ISettingsValue"/> for value-based equality.
        /// </summary>
        /// <param name="other">The other value to compare with.</param>
        /// <returns><c>true</c> if both represent the same key and values.</returns>
        public bool Is(ISettingsValue other)
        {
            return other is ModelBase modelBase && Is(modelBase);
        }

        #endregion

        #region ModelBase

        /// <inheritdoc />
        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not SettingsValue<T> other)
                return false;

            if (!Key.Is(other.Key) || !ValueKind.Is(other.ValueKind) || !Scope.Is(other.Scope))
                return false;

            if (Serializer != null)
            {
                return Serializer.AreEqual(DefaultValue!, other.DefaultValue!) &&
                       Serializer.AreEqual(Value!, other.Value!);
            }

            return EqualityComparer<T>.Default.Equals(DefaultValue, other.DefaultValue) &&
                   EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        /// <inheritdoc />
        public override ModelBase Clone()
        {
            if (Serializer != null)
            {
                return new SettingsValue<T>(Key, ValueKind, Tag, Hidden, Scope, Serializer,
                                            DefaultValue, Value)
                {
                    Name = Name
                };
            }

            return new SettingsValue<T>(Key, Name, ValueKind, Tag, Hidden, Scope,
                                        DefaultValue, Value, IsDefault);
        }

        #endregion

        #region Event Handlers

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((SettingsValue<T> v) => v.DefaultValue) ||
                e.IsProperty((SettingsValue<T> v) => v.Value))
            {
                IsDefault = ComputeIsDefault();
            }
        }

        #endregion

        #region ISettingsValue

        object ISettingsValue.Value
        {
            get => Value!;
            set => Value = ConvertValue(value);
        }

        object ISettingsValue.DefaultValue => DefaultValue!;

        SettingsScope ISettingsValue.Scope => Scope;

        private bool ComputeIsDefault()
        {
            if (Serializer != null)
            {
                return DefaultValue is not null && Value is not null
                    ? Serializer.AreEqual(DefaultValue, Value)
                    : DefaultValue is null && Value is null;
            }

            return EqualityComparer<T>.Default.Equals(DefaultValue, Value);
        }

        private T ConvertValue(object value)
        {
            if (value is T typed)
                return typed;

            if (Serializer != null)
                return (T)Serializer.Deserialize(value?.ToString() ?? "", Tag);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public string ValueKind { get; }

        /// <inheritdoc />
        public string Tag { get; }

        /// <inheritdoc />
        public bool Hidden { get; }

        /// <summary>
        /// Gets the scope this setting belongs to.
        /// </summary>
        public SettingsScope Scope { get; }

        /// <inheritdoc />
        [Notify]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the default value from the Default provider.
        /// </summary>
        [Notify]
        public T DefaultValue { get; set; } = default!;

        /// <summary>
        /// Gets or sets the active value from the appropriate scope provider.
        /// </summary>
        [Notify]
        public T Value { get; set; } = default!;

        /// <inheritdoc />
        [Notify]
        public bool IsDefault { get; private set; }

        [MemoryPackIgnore]
        private ISettingsSerializer? Serializer { get; }

        #endregion

    }
}
