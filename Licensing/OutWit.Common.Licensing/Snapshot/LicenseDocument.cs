using System;
using OutWit.Common.Abstract;
using OutWit.Common.Licensing.Validation;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Snapshot
{
    /// <summary>
    /// One licence installed on this host, and what the runtime made of it.
    /// <para>
    /// The store holds several documents on purpose — that is what makes a
    /// renewal safe — so a panel that showed only the effective one would hide
    /// the staged renewal a customer just installed and is waiting to see.
    /// </para>
    /// </summary>
    public sealed class LicenseDocument : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseDocument other)
                return false;

            return Id.Is(other.Id)
                   && Edition.Is(other.Edition)
                   && KeyId.Is(other.KeyId)
                   && CustomerName.Is(other.CustomerName)
                   && NotBeforeUtc.Is(other.NotBeforeUtc)
                   && ExpiresUtc.Is(other.ExpiresUtc)
                   && Status.Is(other.Status)
                   && IsEffective.Is(other.IsEffective);
        }

        public override LicenseDocument Clone()
        {
            return new LicenseDocument
            {
                Id = Id,
                Edition = Edition,
                KeyId = KeyId,
                CustomerName = CustomerName,
                NotBeforeUtc = NotBeforeUtc,
                ExpiresUtc = ExpiresUtc,
                Status = Status,
                IsEffective = IsEffective
            };
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"{Edition} — {DescribeTerm()} — {Status}{(IsEffective ? " (in force)" : string.Empty)}";
        }

        /// <summary>The document's term, with the unlimited case spelled out.</summary>
        public string DescribeTerm()
        {
            return IsUnlimited
                ? "unlimited"
                : $"{NotBeforeUtc:yyyy-MM-dd} → {ExpiresUtc:yyyy-MM-dd}";
        }

        #endregion

        #region Properties

        /// <summary>Issuance id (<c>jti</c>) — what support quotes, and what uninstall takes.</summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>Human-facing bundle name. Display only.</summary>
        public string Edition { get; init; } = string.Empty;

        /// <summary>The key that signed it, for diagnosing a ring that does not carry it.</summary>
        public string KeyId { get; init; } = string.Empty;

        /// <summary>Who it was issued to.</summary>
        public string CustomerName { get; init; } = string.Empty;

        /// <summary>Not valid before this instant.</summary>
        public DateTime NotBeforeUtc { get; init; }

        /// <summary>Expiry, or <c>null</c> for unlimited.</summary>
        public DateTime? ExpiresUtc { get; init; }

        /// <summary>True when the document never expires.</summary>
        public bool IsUnlimited => ExpiresUtc == null;

        /// <summary>What this document alone validated as.</summary>
        public LicenseStatus Status { get; init; } = LicenseStatus.Missing;

        /// <summary>True for the one document currently in force, if any.</summary>
        public bool IsEffective { get; init; }

        #endregion
    }
}
