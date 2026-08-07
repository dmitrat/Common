using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Licensing.Keys
{
    /// <summary>
    /// An in-memory key ring, usually loaded from the JSON artifact the issuing
    /// service exports and the product embeds as a resource.
    /// <para>
    /// The export exists so nobody copies PEM blocks by hand. That sounds like
    /// convenience; it is not. A hand-copied ring eventually contains the wrong
    /// key, and the failure appears at a customer site as "licence invalid" with
    /// no way to tell which side is wrong.
    /// </para>
    /// </summary>
    public sealed class LicenseKeyRing : ILicenseKeyRing
    {
        #region Fields

        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        private readonly Dictionary<string, LicenseKeyInfo> m_byKeyId;

        #endregion

        #region Constructors

        public LicenseKeyRing(IEnumerable<LicenseKeyInfo> keys)
        {
            var known = keys?.Where(key => !string.IsNullOrWhiteSpace(key.KeyId)).ToList()
                        ?? new List<LicenseKeyInfo>();

            // Last one wins rather than throwing: a ring with a duplicated kid is
            // a packaging mistake, and refusing to start a product over it would
            // turn a build slip into an outage.
            m_byKeyId = new Dictionary<string, LicenseKeyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in known)
                m_byKeyId[key.KeyId] = key;

            Keys = known;
        }

        #endregion

        #region Functions

        /// <summary>An empty ring — trusts nothing. Every licence fails with <c>UnknownKeyId</c>.</summary>
        public static LicenseKeyRing Empty()
        {
            return new LicenseKeyRing(Array.Empty<LicenseKeyInfo>());
        }

        /// <summary>
        /// Reads a ring from its JSON export, or returns an empty ring when the
        /// content cannot be parsed.
        /// <para>
        /// Failing to an empty ring rather than throwing is deliberate: a
        /// corrupt embedded resource then surfaces as an unlicensed product with
        /// a clear status, not as a crash on startup.
        /// </para>
        /// </summary>
        public static LicenseKeyRing FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Empty();

            try
            {
                var export = JsonSerializer.Deserialize<LicenseKeyRingExport>(json!, JSON_OPTIONS);

                return new LicenseKeyRing(export?.Keys ?? new List<LicenseKeyInfo>());
            }
            catch (JsonException)
            {
                return Empty();
            }
        }

        /// <summary>Serialises the ring to the exchange format <see cref="FromJson"/> reads.</summary>
        public string ToJson(string product)
        {
            return JsonSerializer.Serialize(
                new LicenseKeyRingExport { Product = product, Keys = Keys.ToList() },
                JSON_OPTIONS);
        }

        #endregion

        #region ILicenseKeyRing

        public LicenseKeyInfo? Find(string? keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId))
                return null;

            return m_byKeyId.TryGetValue(keyId!, out var key) ? key : null;
        }

        public IReadOnlyList<LicenseKeyInfo> Keys { get; }

        #endregion

        #region Export

        /// <summary>
        /// The on-disk shape of an exported ring. <see cref="Product"/> is
        /// informational — it names what the ring was generated for, so a
        /// mismatched file is obvious to a human reading it.
        /// </summary>
        private sealed class LicenseKeyRingExport
        {
            [JsonPropertyName("product")]
            public string Product { get; set; } = string.Empty;

            [JsonPropertyName("keys")]
            public List<LicenseKeyInfo> Keys { get; set; } = new();
        }

        #endregion
    }
}
