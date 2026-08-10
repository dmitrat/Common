namespace OutWit.Common.Licensing
{
    /// <summary>
    /// How the product should behave right now.
    /// <para>
    /// A boolean was enough to gate a verb and not enough to express anything
    /// the design committed to. The mode is derived once, from the licence, the
    /// clock and the product's own grace policy, and read everywhere — so that
    /// "expired but still inside the renewal window" does not get reinvented,
    /// differently, in each product.
    /// </para>
    /// <para>
    /// Note what is <b>absent</b>: there is no <c>Blocked</c>. Nothing in this
    /// system ever refuses to start, and nothing terminates work already
    /// running. <see cref="Restricted"/> refuses the <i>next</i> productive
    /// verb and nothing else — the settings screen, the exports and the licence
    /// panel stay reachable, because that is where the fix is applied.
    /// </para>
    /// </summary>
    public enum LicenseMode
    {
        /// <summary>A valid licence is in force.</summary>
        Licensed = 0,

        /// <summary>Running on a self-issued demo, within its term.</summary>
        Demo = 1,

        /// <summary>
        /// The term ended and the product's renewal grace has not.
        /// <para>
        /// Work is still allowed. A grace window that refused work would be
        /// nothing but a later expiry with a worse message; its whole purpose is
        /// that a lapse discovered at 3am on a Sunday makes noise instead of
        /// stopping a production cluster.
        /// </para>
        /// </summary>
        Grace = 2,

        /// <summary>
        /// No entitlement: grace exhausted, bound elsewhere, wrong version, the
        /// clock unusable, the demo over, or nothing installed after a licence
        /// once was. Carries the underlying
        /// <see cref="Validation.LicenseStatus"/>, so the reason stays specific.
        /// </summary>
        Restricted = 3
    }
}
