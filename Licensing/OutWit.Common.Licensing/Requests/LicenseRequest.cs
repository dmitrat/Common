using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Requests
{
    /// <summary>
    /// What a customer sends when asking for a licence.
    /// <para>
    /// The weak point of a naive fingerprint flow is the hand-off — "the code
    /// gets to the admin somehow". This is that somehow, made concrete. It
    /// carries the <b>factor hashes</b>, not just the display code, so the
    /// operator pastes one blob and the binding block fills itself in. The
    /// display code exists for the phone call; this exists for the actual work,
    /// and nothing is ever retyped.
    /// </para>
    /// </summary>
    public sealed class LicenseRequest : ModelBase
    {
        #region Constants

        /// <summary>Current blob version, so a newer client is recognisable to an older tool.</summary>
        public const int CURRENT_VERSION = 1;

        #endregion

        #region Fields

        private static readonly JsonSerializerOptions JSON_OPTIONS = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        #endregion

        #region Functions

        /// <summary>Serialises the request for transport.</summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JSON_OPTIONS);
        }

        /// <summary>Reads a request, or <c>null</c> when the text is not one.</summary>
        public static LicenseRequest? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<LicenseRequest>(json!, JSON_OPTIONS);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>The suggested file name — the fingerprint makes it self-identifying.</summary>
        public string SuggestedFileName()
        {
            var product = string.IsNullOrWhiteSpace(Product) ? "license" : Product;

            return $"{product}-{Fingerprint}.owlreq".Replace(' ', '-');
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseRequest other)
                return false;

            return Version.Is(other.Version)
                   && Product.Is(other.Product)
                   && ProductVersion.Is(other.ProductVersion)
                   && Fingerprint.Is(other.Fingerprint)
                   && Host.Is(other.Host)
                   && Contact.Is(other.Contact)
                   && Notes.Is(other.Notes)
                   && Factors.Is(other.Factors);
        }

        public override LicenseRequest Clone()
        {
            return new LicenseRequest
            {
                Version = Version,
                Product = Product,
                ProductVersion = ProductVersion,
                Fingerprint = Fingerprint,
                Host = Host,
                Contact = Contact,
                Notes = Notes,
                Factors = Factors.Select(factor => factor.Clone()).ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>Blob format version.</summary>
        [JsonPropertyName("v")]
        public int Version { get; init; } = CURRENT_VERSION;

        /// <summary>Which product the licence is for.</summary>
        [JsonPropertyName("product")]
        public string Product { get; init; } = string.Empty;

        /// <summary>Which version of it is installed — so the operator can set an appropriate range.</summary>
        [JsonPropertyName("productVersion")]
        public string ProductVersion { get; init; } = string.Empty;

        /// <summary>The display code, for the phone call and for the file name.</summary>
        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; init; } = string.Empty;

        /// <summary>The hashed factors the licence should be bound to.</summary>
        [JsonPropertyName("factors")]
        public IReadOnlyList<LicenseFactor> Factors { get; init; } = Array.Empty<LicenseFactor>();

        /// <summary>Human description of the host, so the operator knows what they are licensing.</summary>
        [JsonPropertyName("host")]
        public string Host { get; init; } = string.Empty;

        /// <summary>Where to send the licence back.</summary>
        [JsonPropertyName("contact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Contact { get; init; }

        /// <summary>Anything the requester wants to add — what they are asking for, a purchase order.</summary>
        [JsonPropertyName("notes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Notes { get; init; }

        #endregion
    }
}
