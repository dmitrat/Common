namespace OutWit.Common.Licensing.Keys
{
    /// <summary>
    /// What a key is allowed to grant, independently of what a payload signed
    /// by it claims.
    /// <para>
    /// The point is blast radius. A key that has to be conveniently reachable —
    /// so trials can be issued in one click — must not also be able to mint a
    /// full commercial licence if it leaks.
    /// </para>
    /// </summary>
    public enum LicenseKeyPolicy
    {
        /// <summary>May grant anything the payload claims.</summary>
        Commercial = 0,

        /// <summary>
        /// May only grant time-limited licences within the trial ceiling. A
        /// payload signed by such a key that claims an unlimited term is
        /// rejected even though its signature is perfect.
        /// </summary>
        TrialOnly = 1
    }
}
