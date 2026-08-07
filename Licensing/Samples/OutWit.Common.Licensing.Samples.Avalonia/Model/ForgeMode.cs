namespace OutWit.Common.Licensing.Samples.Avalonia.Model;

/// <summary>
/// A deliberate defect the issuer pane can introduce.
/// <para>
/// Every arm here exists to drive one refusal path in the validator. A status
/// that no test and no demo can reach is a status nobody can trust — this is how
/// the harness proves each one is real, and how a human sees what the product
/// says when it happens.
/// </para>
/// </summary>
public enum ForgeMode
{
    /// <summary>Issue an honest licence.</summary>
    None,

    /// <summary>Sign with a key the product does not trust → <c>UnknownKeyId</c>.</summary>
    UnknownKey,

    /// <summary>Claim a different product → <c>WrongProduct</c>.</summary>
    WrongProduct,

    /// <summary>Use a key not scoped to this product → <c>ExceedsKeyPolicy</c>.</summary>
    OutOfScopeKey,

    /// <summary>Put a different algorithm in the header than the key is registered for → <c>SignatureInvalid</c>.</summary>
    MismatchedAlgorithm,

    /// <summary>Edit the payload after signing → <c>SignatureInvalid</c>.</summary>
    TamperedPayload,

    /// <summary>Corrupt the token's shape → <c>Malformed</c>.</summary>
    BrokenToken,

    /// <summary>Bind to a machine that is not this one → <c>BindingMismatch</c>.</summary>
    ForeignMachine,

    /// <summary>Ask a trial-only key for an unlimited term → <c>ExceedsKeyPolicy</c>.</summary>
    TrialOverreach,

    /// <summary>Demand a version range the running build is outside of → <c>WrongVersion</c>.</summary>
    WrongVersion
}
