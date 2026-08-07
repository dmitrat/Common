namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// What a licence is tied to. The kind is descriptive — it names the family
    /// of factors recorded — while the actual decision is made by matching
    /// factors against a threshold.
    /// </summary>
    public enum LicenseBindingKind
    {
        /// <summary>Tied to nothing. Valid wherever it is installed.</summary>
        None = 0,

        /// <summary>
        /// Tied to a workstation, via the factors
        /// <c>OutWit.Common.Platform</c> reads. What is sold is the right to run
        /// the program on this machine, so whoever sits at it may use it.
        /// </summary>
        Machine = 1,

        /// <summary>
        /// Tied to a deployment — a tenant slug, an installation id. The right
        /// choice for a containerised server, where hardware identity is not
        /// stable across a recreated container and would make the licence die on
        /// an ordinary redeploy.
        /// </summary>
        Tenant = 2,

        /// <summary>Tied to factors from more than one family.</summary>
        Composite = 3
    }
}
