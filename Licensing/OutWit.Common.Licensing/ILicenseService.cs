using System.Threading.Tasks;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing
{
    /// <summary>Raised when the evaluated licence state has actually changed.</summary>
    public delegate void LicenseStateEventHandler(ILicenseService sender, LicenseState state);

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
        /// <summary>
        /// Raised when the state changes in a way anyone could notice — the
        /// mode, the verdict, which licence is in force, its expiry, the days
        /// left on it, or the clock.
        /// <para>
        /// Deliberately not raised on every re-evaluation. A periodic reload
        /// that finds nothing new must not make a banner redraw, or the event
        /// stops meaning anything and consumers start filtering it themselves.
        /// </para>
        /// </summary>
        event LicenseStateEventHandler StateChanged;

        /// <summary>Current state. Never null — an absent licence is a state, not a failure.</summary>
        LicenseState State { get; }

        /// <summary>
        /// The same state flattened for a panel, a health endpoint or a wire.
        /// Recomputed with the state, so it is never stale against it.
        /// </summary>
        LicenseSnapshot Snapshot { get; }

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

        /// <summary>
        /// Uninstalls the licence with the given <c>jti</c> and re-evaluates.
        /// Returns false when it was not there — or when it came from a source
        /// the product does not own, such as an environment variable.
        /// </summary>
        Task<bool> RemoveAsync(string licenseId);

        /// <summary>Builds the request blob a customer sends to ask for a licence.</summary>
        Task<LicenseRequest> CreateRequestAsync(string? host = null, string? contact = null, string? notes = null);
    }
}
