using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Navigation.Model
{
    /// <summary>
    /// A set of navigation parameters. Immutable once built: parameters end up in the
    /// journal, and a mutable bag there would mean "back" restores a state nobody left.
    /// </summary>
    public sealed class NavigationParameters : ModelBase, IEnumerable<KeyValuePair<string, object?>>
    {
        #region Static

        /// <summary>
        /// The empty parameter set.
        /// </summary>
        public static readonly NavigationParameters EMPTY = new();

        #endregion

        #region Fields

        private readonly Dictionary<string, object?> m_values;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty parameter set.
        /// </summary>
        public NavigationParameters()
        {
            m_values = new Dictionary<string, object?>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Creates a parameter set from key/value pairs. A repeated key keeps the last value.
        /// </summary>
        /// <param name="values">The pairs.</param>
        public NavigationParameters(params (string Key, object? Value)[] values)
            : this()
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var (key, value) in values)
                m_values[Validate(key)] = value;
        }

        /// <summary>
        /// Creates a parameter set from key/value pairs. A repeated key keeps the last value.
        /// </summary>
        /// <param name="values">The pairs.</param>
        public NavigationParameters(IEnumerable<KeyValuePair<string, object?>> values)
            : this()
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            foreach (var pair in values)
                m_values[Validate(pair.Key)] = pair.Value;
        }

        private NavigationParameters(Dictionary<string, object?> values)
        {
            m_values = values;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Returns a new set with <paramref name="key"/> set to <paramref name="value"/>.
        /// This set is not modified.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The new set.</returns>
        public NavigationParameters With(string key, object? value)
        {
            var copy = new Dictionary<string, object?>(m_values, StringComparer.Ordinal)
            {
                [Validate(key)] = value
            };

            return new NavigationParameters(copy);
        }

        /// <summary>
        /// Returns a set without <paramref name="key"/>. Returns this set when the key is absent.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The new set, or this set.</returns>
        public NavigationParameters Without(string key)
        {
            if (!m_values.ContainsKey(Validate(key)))
                return this;

            var copy = new Dictionary<string, object?>(m_values, StringComparer.Ordinal);
            copy.Remove(key);

            return new NavigationParameters(copy);
        }

        /// <summary>
        /// Tells whether the set contains <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True when present.</returns>
        public bool Contains(string key)
        {
            return m_values.ContainsKey(key);
        }

        /// <summary>
        /// Gets the value under <paramref name="key"/> if it is present and of type <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The expected value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value, or default.</param>
        /// <returns>True when present and of the expected type.</returns>
        public bool TryGet<TValue>(string key, out TValue value)
        {
            if (m_values.TryGetValue(key, out var raw) && raw is TValue typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Gets the value under <paramref name="key"/>, or <paramref name="defaultValue"/> when it is
        /// absent or of another type.
        /// </summary>
        /// <typeparam name="TValue">The expected value type.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The fallback.</param>
        /// <returns>The value or the fallback.</returns>
        public TValue Get<TValue>(string key, TValue defaultValue = default!)
        {
            return TryGet<TValue>(key, out var value) ? value : defaultValue;
        }

        private static string Validate(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Parameter key must be a non-empty string.", nameof(key));

            return key;
        }

        private static bool ValuesEqual(object? left, object? right, double tolerance)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            if (left is ModelBase leftModel && right is ModelBase rightModel)
                return leftModel.Is(rightModel, tolerance);

            if (left is double leftDouble && right is double rightDouble)
                return leftDouble.Is(rightDouble, tolerance);

            if (left is float leftFloat && right is float rightFloat)
                return leftFloat.Is(rightFloat, tolerance);

            return left.Equals(right);
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not NavigationParameters other)
                return false;

            if (other.m_values.Count != m_values.Count)
                return false;

            foreach (var pair in m_values)
            {
                if (!other.m_values.TryGetValue(pair.Key, out var otherValue))
                    return false;

                if (!ValuesEqual(pair.Value, otherValue, tolerance))
                    return false;
            }

            return true;
        }

        public override ModelBase Clone()
        {
            return new NavigationParameters(new Dictionary<string, object?>(m_values, StringComparer.Ordinal));
        }

        public override string ToString()
        {
            if (m_values.Count == 0)
                return "{}";

            return "{ " + string.Join(", ", m_values.Select(pair => $"{pair.Key} = {pair.Value ?? "null"}")) + " }";
        }

        #endregion

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return m_values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Number of parameters.
        /// </summary>
        public int Count => m_values.Count;

        /// <summary>
        /// The keys.
        /// </summary>
        public IEnumerable<string> Keys => m_values.Keys;

        /// <summary>
        /// The value under <paramref name="key"/>, or null when absent.
        /// </summary>
        /// <param name="key">The key.</param>
        public object? this[string key] => m_values.TryGetValue(key, out var value) ? value : null;

        #endregion
    }
}
