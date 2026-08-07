namespace OutWit.Common.Licensing.Keys
{
    /// <summary>
    /// Signature algorithms a licence key may be registered for.
    /// <para>
    /// The algorithm is a property of the <b>key</b>, never of the token. The
    /// <c>alg</c> header is only ever checked against what the key ring says the
    /// key is for — it never gets to choose. That is what closes the classic
    /// algorithm-substitution hole, where a forged header talks a verifier into
    /// using a weaker primitive, or into treating a public key as a secret.
    /// </para>
    /// </summary>
    public enum LicenseAlgorithm
    {
        /// <summary>Unset — never valid for signing or verification.</summary>
        None = 0,

        /// <summary>ECDSA P-256 with SHA-256. The default: 64-byte signature, in the BCL everywhere.</summary>
        ES256 = 1,

        /// <summary>ECDSA P-384 with SHA-384.</summary>
        ES384 = 2,

        /// <summary>ECDSA P-521 with SHA-512.</summary>
        ES512 = 3,

        /// <summary>RSA PKCS#1 v1.5 with SHA-256 — for a product line that must interoperate with existing RSA tooling.</summary>
        RS256 = 4,

        /// <summary>RSA PSS with SHA-256.</summary>
        PS256 = 5
    }
}
