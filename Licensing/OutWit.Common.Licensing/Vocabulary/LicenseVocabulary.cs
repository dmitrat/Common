using System;
using System.Collections.Generic;
using System.Linq;

namespace OutWit.Common.Licensing.Vocabulary
{
    /// <summary>
    /// What a product understands: the feature and limit keys it will ever
    /// check.
    /// <para>
    /// Declaring this is what closes a whole class of silent failure. While the
    /// issuing catalogue is maintained by hand, an operator can tick
    /// <c>"SSO"</c> while the product checks <c>"sso"</c> — nothing errors, the
    /// customer simply does not get what they paid for, and nobody finds out for
    /// weeks. With a declared vocabulary the product can say so at first
    /// install, and the same declaration exports as the descriptor that removes
    /// the mismatch at its root.
    /// </para>
    /// </summary>
    public sealed class LicenseVocabulary
    {
        #region Fields

        private readonly Dictionary<string, string> m_features = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LimitDefinition> m_limits = new(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Functions

        /// <summary>Declares a capability this product gates on.</summary>
        public LicenseVocabulary Feature(string key, string? description = null)
        {
            if (!string.IsNullOrWhiteSpace(key))
                m_features[key.Trim()] = description ?? string.Empty;

            return this;
        }

        /// <summary>Declares a numeric cap this product reads, and what it falls back to.</summary>
        public LicenseVocabulary Limit(string key, string? description = null, long? @default = null)
        {
            if (!string.IsNullOrWhiteSpace(key))
                m_limits[key.Trim()] = new LimitDefinition(description ?? string.Empty, @default);

            return this;
        }

        /// <summary>True when the product declared <paramref name="key"/> as a feature it understands.</summary>
        public bool KnowsFeature(string key)
        {
            return m_features.ContainsKey(key);
        }

        /// <summary>True when the product declared <paramref name="key"/> as a limit it reads.</summary>
        public bool KnowsLimit(string key)
        {
            return m_limits.ContainsKey(key);
        }

        /// <summary>
        /// Keys a licence granted that this product does not recognise — the
        /// typo report.
        /// </summary>
        public IReadOnlyList<string> Unrecognised(IEnumerable<string>? features, IEnumerable<string>? limits)
        {
            // With nothing declared the product opts out of the check entirely,
            // rather than reporting every key it was given as unknown.
            if (m_features.Count == 0 && m_limits.Count == 0)
                return Array.Empty<string>();

            var unknown = new List<string>();

            if (m_features.Count > 0 && features != null)
                unknown.AddRange(features.Where(key => !KnowsFeature(key)).Select(key => $"feature '{key}'"));

            if (m_limits.Count > 0 && limits != null)
                unknown.AddRange(limits.Where(key => !KnowsLimit(key)).Select(key => $"limit '{key}'"));

            return unknown;
        }

        /// <summary>The fallback for a limit the licence does not set.</summary>
        public long DefaultFor(string key, long fallback)
        {
            return m_limits.TryGetValue(key, out var definition) && definition.Default != null
                ? definition.Default.Value
                : fallback;
        }

        #endregion

        #region Properties

        /// <summary>Declared feature keys with their descriptions.</summary>
        public IReadOnlyDictionary<string, string> Features => m_features;

        /// <summary>Declared limit keys with their descriptions and defaults.</summary>
        public IReadOnlyDictionary<string, LimitDefinition> Limits => m_limits;

        #endregion

        #region Definitions

        /// <summary>A declared limit: what it means, and what it is when unset.</summary>
        public sealed record LimitDefinition(string Description, long? Default);

        #endregion
    }
}
