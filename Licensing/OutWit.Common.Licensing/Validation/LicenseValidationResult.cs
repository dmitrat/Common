using System;
using System.Globalization;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;

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
                LicenseStatus.BindingMismatch => DescribeBindingMismatch(),
                LicenseStatus.NotYetValid => $"This licence is not valid until {Format(Payload?.NotBeforeUtc)}.",
                LicenseStatus.Expired => $"Licence{subject} expired on {Format(Payload?.ExpiresUtc)}.",
                // Deliberately an observation rather than an accusation. A VM
                // restored from a snapshot, a dead RTC, a laptop back from a
                // badly configured timezone and a container with no NTP yet are
                // each more common than the tampering this guards against, and
                // all four produce a customer who did nothing wrong.
                LicenseStatus.ClockTampered => $"The system clock is behind the last time this product ran; the licence{subject} cannot be checked until it is corrected.",
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

        /// <summary>
        /// Names the thing the licence is actually tied to.
        /// <para>
        /// It used to say "a different machine" whatever the binding was. Told
        /// that about a container, a server operator goes and looks at hardware
        /// — and the answer is a URL or an installation id in a config file.
        /// A refusal that names the wrong axis sends people to the wrong place,
        /// which is the one thing a specific reason exists to prevent.
        /// </para>
        /// <para>
        /// The customer is introduced separately rather than through the shared
        /// subject clause, because "issued for a different machine to ACME GmbH"
        /// reads as though the machine were theirs.
        /// </para>
        /// </summary>
        private string DescribeBindingMismatch()
        {
            var subject = Payload?.Binding?.Kind == LicenseBindingKind.Tenant
                ? "a different deployment"
                : "a different machine";

            var name = Payload?.Customer?.Name;

            return string.IsNullOrWhiteSpace(name)
                ? $"This licence was issued for {subject}."
                : $"This licence, issued to {name}, was issued for {subject}.";
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
