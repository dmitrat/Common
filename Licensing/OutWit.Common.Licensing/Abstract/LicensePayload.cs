using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// The decoded contents of a licence: what was granted, to whom, for how
    /// long, and where it may run.
    /// <para>
    /// <b><see cref="Edition"/> is a label, never a decision.</b> Products
    /// branch on <see cref="Features"/> and <see cref="Limits"/> only. The
    /// moment a product contains <c>if (edition == "Enterprise")</c>, the set of
    /// valid editions becomes a fact compiled into a binary, and every new tier
    /// — an academic price, an OEM bundle, a one-off deal — turns into a code
    /// change and a release. With attributes, it is a row in a table.
    /// </para>
    /// <para>
    /// Unknown JSON members are preserved on the wire by construction: a
    /// verifier checks the signature over the exact bytes it received and only
    /// then parses, so a newer issuer may add fields without invalidating
    /// anything.
    /// </para>
    /// </summary>
    public sealed class LicensePayload : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicensePayload other)
                return false;

            return Id.Is(other.Id)
                   && IssuedUtc.Is(other.IssuedUtc)
                   && Product.Is(other.Product)
                   && Edition.Is(other.Edition)
                   && AppVersionRange.Is(other.AppVersionRange)
                   && NotBeforeUtc.Is(other.NotBeforeUtc)
                   && ExpiresUtc.Is(other.ExpiresUtc)
                   && Notes.Is(other.Notes)
                   && IsSameModel(Customer, other.Customer, tolerance)
                   && IsSameModel(Binding, other.Binding, tolerance)
                   && IsSameModel(CheckIn, other.CheckIn, tolerance)
                   && Features.Is(other.Features)
                   && Supersedes.Is(other.Supersedes)
                   && Limits.Is(other.Limits);
        }

        public override LicensePayload Clone()
        {
            return new LicensePayload
            {
                Id = Id,
                IssuedUtc = IssuedUtc,
                Product = Product,
                Edition = Edition,
                AppVersionRange = AppVersionRange,
                NotBeforeUtc = NotBeforeUtc,
                ExpiresUtc = ExpiresUtc,
                Notes = Notes,
                Customer = Customer?.Clone(),
                Binding = Binding?.Clone(),
                CheckIn = CheckIn?.Clone(),
                Features = Features.ToList(),
                Supersedes = Supersedes.ToList(),
                Limits = new Dictionary<string, long>(Limits)
            };
        }

        private static bool IsSameModel(ModelBase? me, ModelBase? other, double tolerance)
        {
            if (me == null && other == null)
                return true;

            if (me == null || other == null)
                return false;

            return me.Is(other, tolerance);
        }

        #endregion

        #region Functions

        /// <summary>
        /// True when the licence never expires. Expressed as an absent
        /// <see cref="ExpiresUtc"/> rather than a date a century out, so it
        /// reads as "Unlimited" instead of "expires 2126" and the commercial
        /// intent stays auditable.
        /// </summary>
        [JsonIgnore]
        public bool IsUnlimited => ExpiresUtc == null;

        /// <summary>
        /// Returns the value of <paramref name="key"/>, or
        /// <paramref name="fallback"/> when the licence does not set it. An
        /// absent limit is unlimited.
        /// </summary>
        public long Limit(string key, long fallback = long.MaxValue)
        {
            return Limits.TryGetValue(key, out var value) ? value : fallback;
        }

        /// <summary>True when the licence grants <paramref name="key"/>. An absent feature is disabled.</summary>
        public bool HasFeature(string key)
        {
            return Features.Any(feature => string.Equals(feature, key, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Properties

        /// <summary>Issuance id, quoted in support and used by supersession.</summary>
        [JsonPropertyName("jti")]
        public string Id { get; init; } = string.Empty;

        /// <summary>When the licence was issued.</summary>
        [JsonPropertyName("iat")]
        public DateTime IssuedUtc { get; init; }

        /// <summary>Product key. Checked hard — a licence for one product never satisfies another.</summary>
        [JsonPropertyName("product")]
        public string Product { get; init; } = string.Empty;

        /// <summary>Human-facing bundle name. Display only — see the type remarks.</summary>
        [JsonPropertyName("edition")]
        public string Edition { get; init; } = string.Empty;

        /// <summary>
        /// SemVer range of product versions this licence covers, e.g.
        /// <c>"&gt;=1.5.0 &lt;2.0.0"</c>. Empty covers every version. This is the
        /// safety valve that makes an unlimited term sane: perpetual for what
        /// was bought, a new document for a major upgrade.
        /// </summary>
        [JsonPropertyName("appVer")]
        public string AppVersionRange { get; init; } = string.Empty;

        /// <summary>Who it was issued to. Commercial record only.</summary>
        [JsonPropertyName("customer")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LicenseCustomer? Customer { get; init; }

        /// <summary>Not valid before this instant.</summary>
        [JsonPropertyName("nbf")]
        public DateTime NotBeforeUtc { get; init; }

        /// <summary>Expiry, or <c>null</c> for unlimited.</summary>
        [JsonPropertyName("exp")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? ExpiresUtc { get; init; }

        /// <summary>Where the licence may run.</summary>
        [JsonPropertyName("binding")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LicenseBinding? Binding { get; init; }

        /// <summary>Numeric caps. Absent means unlimited.</summary>
        [JsonPropertyName("limits")]
        public IReadOnlyDictionary<string, long> Limits { get; init; } = new Dictionary<string, long>();

        /// <summary>Granted capabilities. Absent means disabled.</summary>
        [JsonPropertyName("features")]
        public IReadOnlyList<string> Features { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Ids this document invalidates. A store that holds both refuses the
        /// listed ones, which is what makes renewal, a corrected reissue and a
        /// transfer clean when the successor reaches the same machine.
        /// </summary>
        [JsonPropertyName("supersedes")]
        public IReadOnlyList<string> Supersedes { get; init; } = Array.Empty<string>();

        /// <summary>Opt-in revocation. Absent — the default — means nothing is ever contacted.</summary>
        [JsonPropertyName("checkIn")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LicenseCheckIn? CheckIn { get; init; }

        /// <summary>Free text carried for the operator's benefit.</summary>
        [JsonPropertyName("notes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Notes { get; init; }

        #endregion
    }
}
