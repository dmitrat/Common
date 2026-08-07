using System;
using System.Globalization;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Validation
{
    /// <summary>
    /// The verdict on one licence, with enough context to explain it.
    /// <para>
    /// The payload is carried even when the licence was refused, whenever the
    /// document was structurally sound. That is what lets a UI say which
    /// customer and which expiry date it is talking about instead of just
    /// refusing.
    /// </para>
    /// </summary>
    public sealed class LicenseValidationResult
    {
        #region Constructors

        private LicenseValidationResult(LicenseStatus status, LicensePayload? payload, string? detail)
        {
            Status = status;
            Payload = payload;
            Detail = detail;
        }

        #endregion

        #region Functions

        public static LicenseValidationResult Valid(LicensePayload payload)
        {
            return new LicenseValidationResult(LicenseStatus.Valid, payload, null);
        }

        public static LicenseValidationResult Failure(LicenseStatus status, LicensePayload? payload = null, string? detail = null)
        {
            return new LicenseValidationResult(status, payload, detail);
        }

        /// <summary>
        /// A sentence a human can act on. Deliberately says what is wrong and
        /// about which licence — never just "invalid".
        /// </summary>
        public string Describe()
        {
            var subject = DescribeSubject();

            return Status switch
            {
                LicenseStatus.Valid => $"Licensed{subject}{DescribeTerm()}.",
                LicenseStatus.Missing => "No licence installed.",
                LicenseStatus.Malformed => "The licence could not be read — it looks incomplete or corrupted.",
                LicenseStatus.UnknownKeyId => "This licence was signed by a key this build does not trust.",
                LicenseStatus.SignatureInvalid => "The licence signature does not verify — it has been altered.",
                LicenseStatus.WrongProduct => $"This licence is for a different product{DescribeProduct()}.",
                LicenseStatus.WrongVersion => $"This licence does not cover the running version{DescribeVersionRange()}.",
                LicenseStatus.BindingMismatch => $"This licence was issued for a different machine{subject}.",
                LicenseStatus.NotYetValid => $"This licence is not valid until {Format(Payload?.NotBeforeUtc)}.",
                LicenseStatus.Expired => $"Licence{subject} expired on {Format(Payload?.ExpiresUtc)}.",
                LicenseStatus.ClockTampered => "The system clock has been moved backwards; the licence cannot be trusted until it is corrected.",
                LicenseStatus.ExceedsKeyPolicy => "This licence claims more than the key that signed it is permitted to grant.",
                LicenseStatus.Superseded => "This licence has been replaced by a newer one installed on this machine.",
                _ => "The licence could not be validated."
            };
        }

        public override string ToString()
        {
            return $"{Status}: {Describe()}";
        }

        #endregion

        #region Tools

        private string DescribeSubject()
        {
            var name = Payload?.Customer?.Name;

            return string.IsNullOrWhiteSpace(name) ? string.Empty : $" to {name}";
        }

        private string DescribeTerm()
        {
            if (Payload == null)
                return string.Empty;

            return Payload.IsUnlimited
                ? " (unlimited)"
                : $" until {Format(Payload.ExpiresUtc)}";
        }

        private string DescribeProduct()
        {
            return string.IsNullOrWhiteSpace(Payload?.Product) ? string.Empty : $" ({Payload!.Product})";
        }

        private string DescribeVersionRange()
        {
            return string.IsNullOrWhiteSpace(Payload?.AppVersionRange) ? string.Empty : $" ({Payload!.AppVersionRange})";
        }

        private static string Format(DateTime? value)
        {
            return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "an unspecified date";
        }

        #endregion

        #region Properties

        /// <summary>The verdict.</summary>
        public LicenseStatus Status { get; }

        /// <summary>True only for <see cref="LicenseStatus.Valid"/>.</summary>
        public bool IsValid => Status == LicenseStatus.Valid;

        /// <summary>The decoded payload, when the document was readable.</summary>
        public LicensePayload? Payload { get; }

        /// <summary>Extra context for logs — not shown to end users.</summary>
        public string? Detail { get; }

        #endregion
    }
}
