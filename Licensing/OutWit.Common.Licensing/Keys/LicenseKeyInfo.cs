using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Keys
{
    /// <summary>
    /// A trusted public key, and the limits of what it may vouch for.
    /// <para>
    /// The legacy shape this replaces kept <b>one</b> key pair in a settings
    /// file and signed everything with it: no rotation, no scoping, and one leak
    /// compromising every licence ever issued. Here a key names the products it
    /// may sign for, so a product embeds only the ring of its own line — and a
    /// leaked key for one product cannot mint a licence for another.
    /// </para>
    /// </summary>
    public sealed class LicenseKeyInfo : ModelBase
    {
        #region Functions

        /// <summary>
        /// True when this key is allowed to vouch for <paramref name="product"/>.
        /// An empty <see cref="Products"/> list scopes the key to nothing —
        /// forgetting to scope a key must fail closed, not open.
        /// </summary>
        public bool CoversProduct(string? product)
        {
            if (string.IsNullOrWhiteSpace(product))
                return false;

            return Products.Any(known => string.Equals(known, product, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// True when the key may still be used to sign. Retired keys keep
        /// verifying — licences already issued must not stop working — but sign
        /// nothing new.
        /// </summary>
        public bool CanSign(DateTime utcNow)
        {
            return RetiredUtc == null || RetiredUtc > utcNow;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseKeyInfo other)
                return false;

            return KeyId.Is(other.KeyId)
                   && Algorithm.Is(other.Algorithm)
                   && PublicKeyPem.Is(other.PublicKeyPem)
                   && Policy.Is(other.Policy)
                   && RetiredUtc.Is(other.RetiredUtc)
                   && Products.Is(other.Products);
        }

        public override LicenseKeyInfo Clone()
        {
            return new LicenseKeyInfo
            {
                KeyId = KeyId,
                Algorithm = Algorithm,
                PublicKeyPem = PublicKeyPem,
                Policy = Policy,
                RetiredUtc = RetiredUtc,
                Products = Products.ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>Key identifier, matched against a token's <c>kid</c>.</summary>
        [JsonPropertyName("kid")]
        public string KeyId { get; init; } = string.Empty;

        /// <summary>
        /// The one algorithm this key may be used with. A token whose header
        /// says otherwise is rejected.
        /// </summary>
        [JsonPropertyName("alg")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LicenseAlgorithm Algorithm { get; init; } = LicenseAlgorithm.None;

        /// <summary>SPKI PEM public key.</summary>
        [JsonPropertyName("publicKeyPem")]
        public string PublicKeyPem { get; init; } = string.Empty;

        /// <summary>Products this key may vouch for.</summary>
        [JsonPropertyName("products")]
        public IReadOnlyList<string> Products { get; init; } = Array.Empty<string>();

        /// <summary>What the key may grant regardless of payload claims.</summary>
        [JsonPropertyName("policy")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LicenseKeyPolicy Policy { get; init; } = LicenseKeyPolicy.Commercial;

        /// <summary>When the key stopped signing, if it has. Verification continues regardless.</summary>
        [JsonPropertyName("retiredUtc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? RetiredUtc { get; init; }

        #endregion
    }
}
