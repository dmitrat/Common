using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Licensing.Validation;
using OutWit.Common.Values;

namespace OutWit.Common.Licensing.Snapshot
{
    /// <summary>
    /// A flat, serialisable projection of everything a licence panel or a health
    /// endpoint wants to say.
    /// <para>
    /// It exists because <see cref="LicenseState"/> deliberately cannot cross a
    /// wire — it holds a <see cref="LicenseValidationResult"/> and is built by
    /// the runtime, not by a deserialiser — while a service's panel runs in a
    /// browser, one WitRPC round trip away from the licence. A projection is
    /// also what a panel binds to anyway, so the same type serves an in-process
    /// desktop app and a remote admin screen without either learning about the
    /// other.
    /// </para>
    /// </summary>
    public sealed class LicenseSnapshot : ModelBase
    {
        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not LicenseSnapshot other)
                return false;

            return Mode.Is(other.Mode)
                   && Status.Is(other.Status)
                   && Description.Is(other.Description)
                   && Product.Is(other.Product)
                   && ProductVersion.Is(other.ProductVersion)
                   && Fingerprint.Is(other.Fingerprint)
                   && LicenseId.Is(other.LicenseId)
                   && Edition.Is(other.Edition)
                   && CustomerName.Is(other.CustomerName)
                   && IsDemo.Is(other.IsDemo)
                   && IsClockSuspect.Is(other.IsClockSuspect)
                   && CanRun.Is(other.CanRun)
                   && EvaluatedUtc.Is(other.EvaluatedUtc)
                   && ExpiresUtc.Is(other.ExpiresUtc)
                   && DaysRemaining.Is(other.DaysRemaining)
                   && GraceExpiresUtc.Is(other.GraceExpiresUtc)
                   && GracePolicy.Is(other.GracePolicy)
                   && Grants.Is(other.Grants)
                   && UnrecognisedKeys.Is(other.UnrecognisedKeys)
                   && Installed.Is(other.Installed);
        }

        public override LicenseSnapshot Clone()
        {
            return new LicenseSnapshot
            {
                Mode = Mode,
                Status = Status,
                Description = Description,
                Product = Product,
                ProductVersion = ProductVersion,
                Fingerprint = Fingerprint,
                LicenseId = LicenseId,
                Edition = Edition,
                CustomerName = CustomerName,
                IsDemo = IsDemo,
                IsClockSuspect = IsClockSuspect,
                CanRun = CanRun,
                EvaluatedUtc = EvaluatedUtc,
                ExpiresUtc = ExpiresUtc,
                DaysRemaining = DaysRemaining,
                GraceExpiresUtc = GraceExpiresUtc,
                GracePolicy = GracePolicy,
                Grants = Grants.Select(grant => grant.Clone()).ToList(),
                UnrecognisedKeys = UnrecognisedKeys.ToList(),
                Installed = Installed.Select(document => document.Clone()).ToList()
            };
        }

        #endregion

        #region Functions

        /// <summary>An empty snapshot, for a consumer that has not been given one yet.</summary>
        public static LicenseSnapshot Empty()
        {
            return new LicenseSnapshot
            {
                Mode = LicenseMode.Restricted,
                Status = LicenseStatus.Missing,
                Description = "No licence information is available."
            };
        }

        /// <summary>The cap in force for <paramref name="key"/>, or <paramref name="fallback"/>.</summary>
        public long Limit(string key, long fallback = long.MaxValue)
        {
            var grant = Grants.FirstOrDefault(candidate =>
                candidate.Kind == LicenseGrantKind.Limit &&
                string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase));

            return grant?.Value ?? fallback;
        }

        /// <summary>True when the licence in force grants <paramref name="key"/>.</summary>
        public bool HasFeature(string key)
        {
            return Grants.Any(candidate =>
                candidate.Kind == LicenseGrantKind.Feature &&
                candidate.IsGranted &&
                string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        public override string ToString()
        {
            return $"{Mode} ({Status}): {Description}";
        }

        #endregion

        #region Properties

        /// <summary>How the product should behave.</summary>
        public LicenseMode Mode { get; init; } = LicenseMode.Restricted;

        /// <summary>The underlying verdict, so a refusal keeps its specific reason.</summary>
        public LicenseStatus Status { get; init; } = LicenseStatus.Missing;

        /// <summary>The sentence a panel, a log line or a refusal shows.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>The product this state belongs to.</summary>
        public string Product { get; init; } = string.Empty;

        /// <summary>The running version, as the verifier was told it.</summary>
        public string ProductVersion { get; init; } = string.Empty;

        /// <summary>The host's display code, for support and for a licence request.</summary>
        public string Fingerprint { get; init; } = string.Empty;

        /// <summary>The <c>jti</c> of the licence in force, when there is one.</summary>
        public string LicenseId { get; init; } = string.Empty;

        /// <summary>Human-facing bundle name. Display only — nothing branches on it.</summary>
        public string Edition { get; init; } = string.Empty;

        /// <summary>Who the licence was issued to.</summary>
        public string CustomerName { get; init; } = string.Empty;

        /// <summary>True when running on a self-issued demo.</summary>
        public bool IsDemo { get; init; }

        /// <summary>True when the clock reads further back than the guard tolerates.</summary>
        public bool IsClockSuspect { get; init; }

        /// <summary>Whether the product may perform its licensed work.</summary>
        public bool CanRun { get; init; }

        /// <summary>When this was computed, from the injected clock.</summary>
        public DateTime EvaluatedUtc { get; init; }

        /// <summary>When the entitlement ends. <c>null</c> means unlimited.</summary>
        public DateTime? ExpiresUtc { get; init; }

        /// <summary>True when the entitlement never expires.</summary>
        public bool IsUnlimited => ExpiresUtc == null;

        /// <summary>Whole days to <see cref="ExpiresUtc"/>, negative once past. <c>null</c> when unlimited.</summary>
        public int? DaysRemaining { get; init; }

        /// <summary>When the renewal grace runs out, when one applies.</summary>
        public DateTime? GraceExpiresUtc { get; init; }

        /// <summary>
        /// The product's renewal grace in words. Present even when there is none,
        /// because a grace the customer does not know about produces exactly the
        /// support call it was meant to prevent.
        /// </summary>
        public string GracePolicy { get; init; } = string.Empty;

        /// <summary>Every key the product declared, with what the licence gave it.</summary>
        public IReadOnlyList<LicenseGrant> Grants { get; init; } = Array.Empty<LicenseGrant>();

        /// <summary>Keys the licence granted that this build does not recognise.</summary>
        public IReadOnlyList<string> UnrecognisedKeys { get; init; } = Array.Empty<string>();

        /// <summary>Every licence installed on this host, effective or not.</summary>
        public IReadOnlyList<LicenseDocument> Installed { get; init; } = Array.Empty<LicenseDocument>();

        #endregion
    }
}
