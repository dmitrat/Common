using System.Threading.Tasks;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;

namespace OutWit.Common.Licensing.MVVM
{
    /// <summary>Raised when the gateway has a new snapshot to report.</summary>
    public delegate void LicenseSnapshotEventHandler(ILicenseGateway sender, LicenseSnapshot snapshot);

    /// <summary>
    /// Everything a licence panel needs, and nothing about where the licence is.
    /// <para>
    /// This is the joint that makes one panel serve two very different consumer
    /// families. A desktop application holds an <see cref="ILicenseService"/> in
    /// the same process and the licence is a field away; a service's panel runs
    /// in a browser that has no machine factors, no store and no key ring — and
    /// must not have them — so its licence is a round trip away. Without this
    /// abstraction the two families would each need their own panel, and the
    /// eleven-arm status vocabulary would be written twice, then three times.
    /// </para>
    /// <para>
    /// Note what is absent: no key ring, no store, no binding provider, no
    /// options. A panel configures nothing — it reports and it installs.
    /// </para>
    /// </summary>
    public interface ILicenseGateway
    {
        /// <summary>
        /// Raised when the licence state changes on its own — a periodic
        /// re-evaluation crossing an expiry is the case that matters, since
        /// nobody is there to press anything.
        /// </summary>
        event LicenseSnapshotEventHandler SnapshotChanged;

        /// <summary>
        /// The last known snapshot. Never null, so a panel can render the moment
        /// it is constructed rather than showing an empty screen until the first
        /// asynchronous answer arrives.
        /// </summary>
        LicenseSnapshot Current { get; }

        /// <summary>Re-reads and re-evaluates, and returns the result.</summary>
        Task<LicenseSnapshot> RefreshAsync();

        /// <summary>Validates and installs a token. A licence that does not validate is not stored.</summary>
        Task<LicenseInstallOutcome> InstallAsync(string token);

        /// <summary>Uninstalls a licence by its <c>jti</c>. False when it was not there to remove.</summary>
        Task<bool> RemoveAsync(string licenseId);

        /// <summary>Builds the request blob a customer sends to ask for a licence.</summary>
        Task<LicenseRequest> CreateRequestAsync(string? contact = null, string? notes = null);
    }
}
