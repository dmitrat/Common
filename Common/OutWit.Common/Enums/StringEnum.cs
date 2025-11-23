using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.Enums
{
    /// <summary>
    /// Base class for string-based enumerations using standard class semantics.
    /// Implements full equality comparison and string conversion.
    /// </summary>
    /// <typeparam name="T">The concrete enum type.</typeparam>
    public abstract class StringEnum<T> : IEquatable<StringEnum<T>>, IComparable<StringEnum<T>> where T : StringEnum<T>
    {
        #region Cache

        // Cache for Parse/GetAll methods
        private static readonly Lazy<Dictionary<string, T>> ITEM_CACHE = new(() =>
        {
            return typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.FieldType == typeof(T))
                .Select(f => (T)f.GetValue(null)!)
                .ToDictionary(x => x.Value);
        });

        #endregion

        #region Constructors

        protected StringEnum(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return Value;
        }

        #endregion

        #region Operators

        public static implicit operator string(StringEnum<T> stringEnum)
        {
            return stringEnum?.Value;
        }

        public static bool operator ==(StringEnum<T>? left, StringEnum<T>? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(StringEnum<T>? left, StringEnum<T>? right)
        {
            return !(left == right);
        }

        #endregion

        #region Static Functions

        public static IReadOnlyCollection<T> GetAll()
        {
            return ITEM_CACHE.Value.Values;
        }

        public static T Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (ITEM_CACHE.Value.TryGetValue(value, out var result))
                return result;

            throw new InvalidOperationException($"Value '{value}' is not a valid {typeof(T).Name}.");
        }

        public static bool TryParse(string value, out T? result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return false;
            }
            return ITEM_CACHE.Value.TryGetValue(value, out result);
        }

        #endregion

        #region Equality

        public override bool Equals(object? obj)
        {
            return Equals(obj as StringEnum<T>);
        }

        public bool Equals(StringEnum<T>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare by Value string
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        #endregion

        #region Comparable

        public int CompareTo(StringEnum<T> other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return
                string.Compare(Value, other.Value, StringComparison.Ordinal);
        }

        #endregion

        #region Properties

        public string Value { get; }

        #endregion

   
    }
}
