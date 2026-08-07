using System;
using System.Collections.Generic;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// What the product currently knows about its licence.
    /// <para>
    /// Note what is absent: no user, no token, no principal. Identity — who you
    /// are and which resources you may reach — is a separate axis, answered
    /// elsewhere. A licence answers only what may run on this machine. Keeping
    /// the two apart in the type is what stops a later change from quietly
    /// making one depend on the other.
    /// </para>
    /// </summary>
    public sealed class LicenseState
    {
        #region Constructors

        internal LicenseState(
            LicenseValidationResult result,
            bool isDemo,
            string fingerprint,
            IReadOnlyList<string> unrecognisedKeys,
            DateTime? demoExpiresUtc = null)
        {
            Result = result;
            IsDemo = isDemo;
            Fingerprint = fingerprint;
            UnrecognisedKeys = unrecognisedKeys;
            DemoExpiresUtc = demoExpiresUtc;
        }

        #endregion

        #region Functions

        /// <summary>A sentence for the licence panel, the log, or a refusal message.</summary>
        public string Describe()
        {
            if (IsDemo && Status == LicenseStatus.Valid)
            {
                var days = DemoExpiresUtc == null ? 0 : Math.Max(0, (DemoExpiresUtc.Value - DateTime.UtcNow).TotalDays);

                return $"Demo — {days:F0} day(s) remaining.";
            }

            if (IsDemo && Status == LicenseStatus.Expired)
                return "The demo period has ended.";

            return Result.Describe();
        }

        public override string ToString()
        {
            return $"{Status}{(IsDemo ? " (demo)" : string.Empty)}: {Describe()}";
        }

        #endregion

        #region Properties

        /// <summary>The verdict on the licence in force.</summary>
        public LicenseStatus Status => Result.Status;

        /// <summary>
        /// Whether the product may perform its licensed work. The one property a
        /// caller should gate on.
        /// </summary>
        public bool CanRun => Result.Status == LicenseStatus.Valid;

        /// <summary>The licence in force, if any — carried even when refused, when it was readable.</summary>
        public LicensePayload? Payload => Result.Payload;

        /// <summary>True when running on a self-issued demo rather than a real licence.</summary>
        public bool IsDemo { get; }

        /// <summary>When the demo period ends, if one is running.</summary>
        public DateTime? DemoExpiresUtc { get; }

        /// <summary>The host's display code, for support and for a licence request.</summary>
        public string Fingerprint { get; }

        /// <summary>
        /// Features and limits the licence granted that this build does not
        /// recognise. Almost always a typo in the issuing catalogue — surfacing
        /// it here is what turns a silently missing capability into a visible
        /// one at first install.
        /// </summary>
        public IReadOnlyList<string> UnrecognisedKeys { get; }

        /// <summary>The full validation result, for logs and diagnostics.</summary>
        public LicenseValidationResult Result { get; }

        #endregion
    }
}
