using System.Threading.Tasks;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing
{
    /// <summary>
    /// The product's view of its licence.
    /// <para>
    /// Nothing on this interface accepts a user, an access token or a
    /// <c>ClaimsPrincipal</c>, and that omission is deliberate. Whether a
    /// program may run on this machine and who is signed into it are separate
    /// questions with separate answers; a licence that could be asked about a
    /// user would eventually be asked, and the two would fuse.
    /// </para>
    /// </summary>
    public interface ILicenseService
    {
        /// <summary>Current state. Never null — an absent licence is a state, not a failure.</summary>
        LicenseState State { get; }

        /// <summary>This host's display code, for support and for a licence request.</summary>
        string Fingerprint { get; }

        /// <summary>
        /// True when the licence in force grants <paramref name="key"/>. Always
        /// false while the licence is not valid.
        /// </summary>
        bool HasFeature(string key);

        /// <summary>
        /// The cap for <paramref name="key"/>, or the declared default, or
        /// <paramref name="fallback"/>. An absent limit is unlimited.
        /// </summary>
        long Limit(string key, long fallback = long.MaxValue);

        /// <summary>Re-reads the store and re-evaluates. Cheap enough to call whenever state might have changed.</summary>
        Task ReloadAsync();

        /// <summary>
        /// Validates and installs a token. A licence that does not validate is
        /// not stored, so a pasted mistake cannot displace a working licence.
        /// </summary>
        Task<LicenseValidationResult> InstallAsync(string token);

        /// <summary>Builds the request blob a customer sends to ask for a licence.</summary>
        Task<LicenseRequest> CreateRequestAsync(string? host = null, string? contact = null, string? notes = null);
    }
}
