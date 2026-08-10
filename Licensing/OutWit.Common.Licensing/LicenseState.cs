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
    /// <para>
    /// Every time-dependent answer here is taken from <see cref="EvaluatedUtc"/>
    /// rather than from the wall clock. A state that described itself using a
    /// different instant from the one it was computed at could report a demo
    /// with days left while refusing to run, and the disagreement would be
    /// invisible to whoever was reading it.
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
            DateTime evaluatedUtc,
            TimeSpan grace = default,
            DateTime? demoExpiresUtc = null,
            bool isClockSuspect = false,
            TimeSpan? clockBehindBy = null)
        {
            Result = result;
            IsDemo = isDemo;
            Fingerprint = fingerprint;
            UnrecognisedKeys = unrecognisedKeys;
            EvaluatedUtc = evaluatedUtc;
            Grace = grace;
            DemoExpiresUtc = demoExpiresUtc;
            IsClockSuspect = isClockSuspect;
            ClockBehindBy = clockBehindBy;

            GraceExpiresUtc = ResolveGraceEnd(result, isDemo, grace);
            Mode = ResolveMode(result, isDemo, isClockSuspect, GraceExpiresUtc, evaluatedUtc);
        }

        #endregion

        #region Functions

        /// <summary>A sentence for the licence panel, the log, or a refusal message.</summary>
        public string Describe()
        {
            if (IsClockSuspect)
                return DescribeClock();

            if (IsDemo && Status == LicenseStatus.Valid)
                return $"Demo — {Math.Max(0, DaysRemaining ?? 0)} day(s) remaining.";

            if (IsDemo && Status == LicenseStatus.Expired)
                return "The demo period has ended.";

            if (Mode == LicenseMode.Grace)
                return DescribeGrace();

            return Result.Describe();
        }

        /// <summary>
        /// The product's renewal grace, in words, for the panel to disclose. A
        /// grace nobody knows about produces the same 3am support call it was
        /// meant to prevent, one fortnight later.
        /// </summary>
        public string DescribeGracePolicy()
        {
            return Grace <= TimeSpan.Zero
                ? "No renewal grace: the licence stops covering this product the day it expires."
                : $"Renewal grace: {Grace.TotalDays:F0} day(s) of continued use after a licence expires.";
        }

        public override string ToString()
        {
            return $"{Mode} ({Status}{(IsDemo ? ", demo" : string.Empty)}): {Describe()}";
        }

        #endregion

        #region Tools

        /// <summary>
        /// When the renewal grace runs out, or <c>null</c> when none applies.
        /// <para>
        /// A demo never gets one: renewal grace answers "the term ended and no
        /// new document has arrived yet", and there is no document to renew.
        /// Handing a demo a grace period would just be a longer demo, decided in
        /// the wrong place.
        /// </para>
        /// </summary>
        private static DateTime? ResolveGraceEnd(LicenseValidationResult result, bool isDemo, TimeSpan grace)
        {
            if (isDemo || grace <= TimeSpan.Zero)
                return null;

            return result.Payload?.ExpiresUtc?.Add(grace);
        }

        private static LicenseMode ResolveMode(
            LicenseValidationResult result,
            bool isDemo,
            bool isClockSuspect,
            DateTime? graceExpiresUtc,
            DateTime evaluatedUtc)
        {
            // An unusable clock gates level 1 whatever the licence says — it is
            // the free bypass, and no expiry date means anything without it. It
            // is also self-healing: the next evaluation after the clock is
            // corrected returns here with no support call and no reissue.
            if (isClockSuspect)
                return LicenseMode.Restricted;

            if (result.Status == LicenseStatus.Valid)
                return isDemo ? LicenseMode.Demo : LicenseMode.Licensed;

            // Only a lapsed term can be graced. A version mismatch is a scope
            // mismatch, not a lapse, and no amount of waiting fixes one.
            if (result.Status == LicenseStatus.Expired && graceExpiresUtc != null && evaluatedUtc < graceExpiresUtc.Value)
                return LicenseMode.Grace;

            return LicenseMode.Restricted;
        }

        private string DescribeGrace()
        {
            return $"{Result.Describe()} Running on a {Grace.TotalDays:F0}-day renewal grace " +
                   $"that ends on {GraceExpiresUtc:yyyy-MM-dd}.";
        }

        /// <summary>
        /// Describes the observation, never the motive. The legitimate causes —
        /// a VM restored from a snapshot, a dead RTC, a laptop back from a badly
        /// configured timezone, a fresh container with no NTP yet — are all more
        /// common than the illegitimate one, and the licence itself is named so
        /// that a customer whose CMOS battery died is not told their licence is
        /// the problem.
        /// <para>
        /// Composed here rather than delegated to
        /// <see cref="LicenseValidationResult.Describe"/> because only the state
        /// knows how far behind the clock is; the result keeps the same sentence
        /// without the number, for callers holding one on its own.
        /// </para>
        /// </summary>
        private string DescribeClock()
        {
            var name = Payload?.Customer?.Name;
            var whose = string.IsNullOrWhiteSpace(name) ? "the licence" : $"the licence to {name}";

            var behind = ClockBehindBy != null && ClockBehindBy.Value.TotalDays >= 1
                ? $" {Math.Ceiling(ClockBehindBy.Value.TotalDays):F0} day(s)"
                : string.Empty;

            return $"The system clock is{behind} behind the last time this product ran; " +
                   $"{whose} cannot be checked until it is corrected.";
        }

        #endregion

        #region Properties

        /// <summary>How the product should behave. The one property a caller reasons about.</summary>
        public LicenseMode Mode { get; }

        /// <summary>The verdict on the licence in force.</summary>
        public LicenseStatus Status => Result.Status;

        /// <summary>
        /// Whether the product may perform its licensed work. True in
        /// <see cref="LicenseMode.Licensed"/>, <see cref="LicenseMode.Demo"/> and
        /// <see cref="LicenseMode.Grace"/>.
        /// </summary>
        public bool CanRun => Mode is LicenseMode.Licensed or LicenseMode.Demo or LicenseMode.Grace;

        /// <summary>The licence in force, if any — carried even when refused, when it was readable.</summary>
        public LicensePayload? Payload => Result.Payload;

        /// <summary>True when running on a self-issued demo rather than a real licence.</summary>
        public bool IsDemo { get; }

        /// <summary>When the demo period ends, if one is running.</summary>
        public DateTime? DemoExpiresUtc { get; }

        /// <summary>The instant this state was computed at, from the injected clock.</summary>
        public DateTime EvaluatedUtc { get; }

        /// <summary>
        /// The product's renewal grace. Set by the build rather than carried in
        /// the payload: it is a product promise, uniform across customers, and
        /// putting it in the document would mean every issued licence silently
        /// ran longer than the term on the invoice.
        /// </summary>
        public TimeSpan Grace { get; }

        /// <summary>When the renewal grace runs out, or <c>null</c> when none applies.</summary>
        public DateTime? GraceExpiresUtc { get; }

        /// <summary>
        /// When the entitlement ends — the licence's expiry, or the demo's.
        /// <c>null</c> means unlimited.
        /// </summary>
        public DateTime? ExpiresUtc => IsDemo ? DemoExpiresUtc : Payload?.ExpiresUtc;

        /// <summary>
        /// Whole days from <see cref="EvaluatedUtc"/> to <see cref="ExpiresUtc"/>,
        /// rounded up, or <c>null</c> when unlimited. Negative once the term has
        /// passed, which is what a grace banner counts against.
        /// </summary>
        public int? DaysRemaining => ExpiresUtc == null
            ? null
            : (int)Math.Ceiling((ExpiresUtc.Value - EvaluatedUtc).TotalDays);

        /// <summary>
        /// True when the clock reads further back than the guard tolerates. The
        /// licence is still evaluated and still carried — a panel must be able to
        /// say which licence it is talking about while reporting the clock.
        /// </summary>
        public bool IsClockSuspect { get; }

        /// <summary>How far the clock is behind what was already observed, when known.</summary>
        public TimeSpan? ClockBehindBy { get; }

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
