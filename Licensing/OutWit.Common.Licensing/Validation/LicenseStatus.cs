namespace OutWit.Common.Licensing.Validation
{
    /// <summary>
    /// Why a licence was accepted or refused.
    /// <para>
    /// Validation reports a reason rather than a boolean on purpose. "Invalid"
    /// turns every licence question into an investigation; "Enterprise licence
    /// for ACME GmbH expired on 2027-08-05" turns most of them into a
    /// thirty-second answer — for the customer, for support, and for whoever
    /// reads the log six months later.
    /// </para>
    /// </summary>
    public enum LicenseStatus
    {
        /// <summary>Accepted.</summary>
        Valid = 0,

        /// <summary>No licence is installed. Not an error — the product runs in demo.</summary>
        Missing = 1,

        /// <summary>Not a licence token: wrong shape, bad encoding, unreadable JSON.</summary>
        Malformed = 2,

        /// <summary>Signed by a key this build does not trust — a different product line, or a key not yet shipped.</summary>
        UnknownKeyId = 3,

        /// <summary>The signature does not verify under the key's registered algorithm.</summary>
        SignatureInvalid = 4,

        /// <summary>A genuine licence, for a different product.</summary>
        WrongProduct = 5,

        /// <summary>Valid, but does not cover the version of the product that is running.</summary>
        WrongVersion = 6,

        /// <summary>Issued for a different machine or deployment.</summary>
        BindingMismatch = 7,

        /// <summary>Its start date has not arrived.</summary>
        NotYetValid = 8,

        /// <summary>Its term has ended.</summary>
        Expired = 9,

        /// <summary>The system clock has moved back past what was already observed.</summary>
        ClockTampered = 10,

        /// <summary>The signing key is not permitted to grant what this licence claims.</summary>
        ExceedsKeyPolicy = 11,

        /// <summary>Explicitly invalidated by a newer licence present on this host.</summary>
        Superseded = 12
    }
}
