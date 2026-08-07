using System;
using System.Collections.Generic;

namespace OutWit.Common.Licensing.Demo
{
    /// <summary>
    /// What the product allows before anything has been bought.
    /// <para>
    /// The demo term is the one term the product decides for itself, because
    /// there is no operator in the loop. Every issued licence takes its term
    /// from whoever issues it.
    /// </para>
    /// </summary>
    public sealed class LicenseDemoOptions
    {
        #region Constants

        public const string DEMO_EDITION = "Demo";
        public const int DEFAULT_DAYS = 30;

        #endregion

        #region Fields

        private readonly Dictionary<string, long> m_limits = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> m_features = new();

        #endregion

        #region Functions

        /// <summary>Caps a limit for the demo.</summary>
        public LicenseDemoOptions Limit(string key, long value)
        {
            if (!string.IsNullOrWhiteSpace(key))
                m_limits[key.Trim()] = value;

            return this;
        }

        /// <summary>Grants a feature during the demo.</summary>
        public LicenseDemoOptions Feature(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !m_features.Contains(key))
                m_features.Add(key.Trim());

            return this;
        }

        /// <summary>Sets how long the demo lasts.</summary>
        public LicenseDemoOptions For(TimeSpan term)
        {
            Term = term;
            return this;
        }

        #endregion

        #region Properties

        /// <summary>How long the demo runs from first start.</summary>
        public TimeSpan Term { get; set; } = TimeSpan.FromDays(DEFAULT_DAYS);

        /// <summary>Demo caps.</summary>
        public IReadOnlyDictionary<string, long> Limits => m_limits;

        /// <summary>Features available during the demo.</summary>
        public IReadOnlyList<string> Features => m_features;

        #endregion
    }
}
