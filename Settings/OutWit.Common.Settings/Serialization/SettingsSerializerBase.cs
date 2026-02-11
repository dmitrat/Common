using System;
using System.Collections.Generic;
using OutWit.Common.Settings.Interfaces;

namespace OutWit.Common.Settings.Serialization
{
    /// <summary>
    /// Base class for strongly-typed settings serializers.
    /// </summary>
    /// <typeparam name="T">The CLR type this serializer handles.</typeparam>
    public abstract class SettingsSerializerBase<T> : ISettingsSerializer
    {
        #region Functions

        /// <summary>
        /// Parses a serialized string into a typed value.
        /// </summary>
        /// <param name="value">The serialized string from storage.</param>
        /// <param name="tag">Additional metadata (e.g. enum type name).</param>
        /// <returns>The deserialized value.</returns>
        public abstract T Parse(string value, string tag);

        /// <summary>
        /// Formats a typed value into a string for storage.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized string.</returns>
        public abstract string Format(T value);

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a">First value.</param>
        /// <param name="b">Second value.</param>
        /// <returns><c>true</c> if the values are equal.</returns>
        public virtual bool AreEqual(T a, T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        #endregion

        #region ISettingsSerializer

        object ISettingsSerializer.Deserialize(string value, string tag)
        {
            return Parse(value, tag)!;
        }

        string ISettingsSerializer.Serialize(object value)
        {
            return value is T t ? Format(t) : "";
        }

        bool ISettingsSerializer.AreEqual(object a, object b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is T ta && b is T tb)
                return AreEqual(ta, tb);

            return false;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public abstract string ValueKind { get; }

        /// <inheritdoc />
        public Type ValueType => typeof(T);

        #endregion
    }
}
