using System;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Demo;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Vocabulary;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// How a product wires licensing up.
    /// <para>
    /// Nothing here accepts a user, a token or a principal — by design, so the
    /// separation between identity and entitlement cannot be violated by
    /// configuration.
    /// </para>
    /// </summary>
    public sealed class LicensingOptions
    {
        #region Constants

        private const int PREFIX_LENGTH = 3;

        #endregion

        #region Functions

        /// <summary>Names the product and the running version. Required.</summary>
        public LicensingOptions ForProduct(string product, Version? version = null, string? fingerprintPrefix = null)
        {
            Product = product;
            ProductVersion = version;
            FingerprintPrefix = fingerprintPrefix ?? DeriveFingerprintPrefix(product);

            return this;
        }

        /// <summary>Sets the public keys this build trusts — the ring of its own product line.</summary>
        public LicensingOptions WithKeyRing(ILicenseKeyRing keyRing)
        {
            KeyRing = keyRing;
            return this;
        }

        /// <summary>Sets what a licence is tied to on this host.</summary>
        public LicensingOptions WithBinding(ILicenseBindingProvider bindingProvider)
        {
            BindingProvider = bindingProvider;
            return this;
        }

        /// <summary>Sets where licences and runtime state live.</summary>
        public LicensingOptions WithStore(ILicenseStore store)
        {
            Store = store;
            return this;
        }

        /// <summary>Enables a demo period before anything has been bought.</summary>
        public LicensingOptions WithDemo(TimeSpan term, Action<LicenseDemoOptions>? configure = null)
        {
            Demo = new LicenseDemoOptions().For(term);
            configure?.Invoke(Demo);

            return this;
        }

        /// <summary>
        /// Declares the feature and limit keys this product understands, so a
        /// licence granting something else can be reported instead of silently
        /// doing nothing.
        /// </summary>
        public LicensingOptions Declares(Action<LicenseVocabulary> configure)
        {
            configure(Vocabulary);
            return this;
        }

        /// <summary>Overrides the clock. For tests, and for hosts with their own time source.</summary>
        public LicensingOptions WithClock(Func<DateTime> clock)
        {
            Clock = clock;
            return this;
        }

        /// <summary>Overrides clock-tamper tolerance.</summary>
        public LicensingOptions WithClockTolerance(TimeSpan tolerance)
        {
            ClockGuard = new ClockGuard(tolerance);
            return this;
        }

        /// <summary>
        /// Sets how long the product keeps working after a licence expires.
        /// <para>
        /// This is <b>renewal</b> grace, and it belongs to the build rather than
        /// to the payload: it is a product promise, uniform across customers,
        /// and putting it in the document would mean every licence ever issued
        /// silently ran longer than the term on the invoice. A server takes one
        /// because the operator who fixes it may be asleep; a desktop app
        /// usually takes none, because a person is sitting in front of it and
        /// the fix is one paste.
        /// </para>
        /// <para>
        /// Not to be confused with check-in grace, which answers "the registry
        /// is unreachable" and is chosen per licence by whoever issues it.
        /// </para>
        /// </summary>
        public LicensingOptions WithGrace(TimeSpan grace)
        {
            Grace = grace < TimeSpan.Zero ? TimeSpan.Zero : grace;
            return this;
        }

        /// <summary>
        /// Re-evaluates the licence on a timer, so a long-lived process notices
        /// its own expiry.
        /// <para>
        /// Needed by both families and for the same reason. A server up for four
        /// months crosses <c>exp</c> and would never find out until it restarts;
        /// a CAD session runs for days and a draughtsman does not restart their
        /// host either. Without this, the only re-evaluation is one somebody
        /// asked for by hand.
        /// </para>
        /// </summary>
        public LicensingOptions WithPeriodicReload(TimeSpan interval)
        {
            ReloadInterval = interval <= TimeSpan.Zero ? null : interval;
            return this;
        }

        #endregion

        #region Tools

        /// <summary>
        /// Derives a three-letter display marker from the product name.
        /// <para>
        /// Built from the initials of the name's capitalised parts, padded from
        /// the last part — <c>WitSweep</c> becomes <c>WSW</c>, <c>WitCloud</c>
        /// becomes <c>WCL</c>. Simply taking the first three letters would give
        /// every product in a family the same marker (<c>WIT</c>), which defeats
        /// the entire point of having one.
        /// </para>
        /// </summary>
        private static string DeriveFingerprintPrefix(string product)
        {
            var words = SplitWords(product);
            if (words.Count == 0)
                return string.Empty;

            var prefix = new string(words.Select(word => word[0]).ToArray());

            // Pad from the tail of the last part until three characters.
            var last = words[words.Count - 1];
            for (var index = 1; prefix.Length < PREFIX_LENGTH && index < last.Length; index++)
                prefix += last[index];

            return prefix.ToUpperInvariant();
        }

        /// <summary>Splits a name at capital letters, keeping only letters.</summary>
        private static IReadOnlyList<string> SplitWords(string? product)
        {
            var words = new List<string>();
            var current = string.Empty;

            foreach (var character in product ?? string.Empty)
            {
                if (!char.IsLetter(character))
                    continue;

                if (char.IsUpper(character) && current.Length > 0)
                {
                    words.Add(current);
                    current = string.Empty;
                }

                current += character;
            }

            if (current.Length > 0)
                words.Add(current);

            return words;
        }

        #endregion

        #region Properties

        /// <summary>Product key, matched hard against a licence's <c>product</c>.</summary>
        public string Product { get; set; } = string.Empty;

        /// <summary>Running version, matched against a licence's version range.</summary>
        public Version? ProductVersion { get; set; }

        /// <summary>Short marker shown before the fingerprint.</summary>
        public string FingerprintPrefix { get; set; } = string.Empty;

        /// <summary>Trusted public keys. Empty means nothing verifies.</summary>
        public ILicenseKeyRing KeyRing { get; set; } = LicenseKeyRing.Empty();

        /// <summary>What the licence is tied to here.</summary>
        public ILicenseBindingProvider BindingProvider { get; set; } = new LicenseBindingProviderNone();

        /// <summary>Where licences live.</summary>
        public ILicenseStore Store { get; set; } = new LicenseStoreMemory();

        /// <summary>Declared vocabulary.</summary>
        public LicenseVocabulary Vocabulary { get; } = new();

        /// <summary>Demo settings, or null when the product has no demo mode.</summary>
        public LicenseDemoOptions? Demo { get; set; }

        /// <summary>Clock-tamper detection.</summary>
        public ClockGuard ClockGuard { get; set; } = new();

        /// <summary>
        /// How long the product keeps working past expiry. Zero — the default —
        /// means the entitlement ends the day the licence does.
        /// </summary>
        public TimeSpan Grace { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How often the licence is re-evaluated on its own, or <c>null</c> for
        /// never. Off by default: a timer nobody asked for is a surprise in a
        /// short-lived process.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }

        /// <summary>The time source.</summary>
        public Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

        #endregion
    }
}
