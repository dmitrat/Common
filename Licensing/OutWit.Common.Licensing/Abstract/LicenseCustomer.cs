using System.Text.Json.Serialization;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Abstract
{
    /// <summary>
    /// Who the licence was issued to.
    /// <para>
    /// A commercial record, shown on the licence panel and quoted in support —
    /// <b>never an authorisation subject</b>. Identity (who you are, what you
    /// may reach) is a separate axis answered by an identity provider; a licence
    /// answers only what may run here. Nothing in validation reads this block.
    /// </para>
    /// </summary>
    public sealed class LicenseCustomer : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseCustomer other)
                return false;

            return Id.Is(other.Id)
                   && Name.Is(other.Name)
                   && Contact.Is(other.Contact);
        }

        public override LicenseCustomer Clone()
        {
            return new LicenseCustomer
            {
                Id = Id,
                Name = Name,
                Contact = Contact
            };
        }

        #endregion

        #region Properties

        /// <summary>Stable customer key in the issuing registry.</summary>
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;

        /// <summary>Display name, as it should appear to the customer.</summary>
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        /// <summary>Contact address recorded at issue time. Optional.</summary>
        [JsonPropertyName("contact")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Contact { get; init; }

        #endregion
    }
}
