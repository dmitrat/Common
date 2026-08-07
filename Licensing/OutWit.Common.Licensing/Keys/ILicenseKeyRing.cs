using System.Collections.Generic;

namespace OutWit.Common.Licensing.Keys
{
    /// <summary>
    /// The set of public keys a product trusts.
    /// <para>
    /// A product embeds the ring of <b>its own product line</b> — not a company
    /// key. That is what bounds a compromise to one line and makes recovery
    /// mechanical: retire the key, ship a build with a new <c>kid</c> for that
    /// line, reissue that line's licences.
    /// </para>
    /// </summary>
    public interface ILicenseKeyRing
    {
        /// <summary>
        /// Returns the key registered under <paramref name="keyId"/>, or
        /// <c>null</c> when this build does not trust it.
        /// </summary>
        LicenseKeyInfo? Find(string? keyId);

        /// <summary>Every key in the ring.</summary>
        IReadOnlyList<LicenseKeyInfo> Keys { get; }
    }
}
