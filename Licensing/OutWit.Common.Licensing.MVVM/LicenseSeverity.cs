namespace OutWit.Common.Licensing.MVVM
{
    /// <summary>
    /// How loudly a banner should say what the panel is saying.
    /// <para>
    /// Supplied here rather than decided per product, because the escalation is
    /// part of the design and not a styling choice: a licence going quiet until
    /// the morning it stops working is the failure the thirty-day warning window
    /// exists to prevent. Where the strip hangs is still a layout decision, and
    /// stays with the view.
    /// </para>
    /// </summary>
    public enum LicenseSeverity
    {
        /// <summary>Nothing to say. A licence comfortably in force shows no banner at all.</summary>
        None = 0,

        /// <summary>Worth knowing: a demo running normally.</summary>
        Info = 1,

        /// <summary>Approaching a cliff: an expiry inside the warning window, or a demo nearly over.</summary>
        Warning = 2,

        /// <summary>Past one: refused, or running on borrowed time inside the renewal grace.</summary>
        Error = 3
    }
}
