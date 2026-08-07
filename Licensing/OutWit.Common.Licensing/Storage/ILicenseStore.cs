using System.Collections.Generic;

namespace OutWit.Common.Licensing.Storage
{
    /// <summary>
    /// Where installed licences and the runtime's sidecar state live.
    /// <para>
    /// The store holds <b>several</b> documents rather than one. That is what
    /// makes a renewal safe: the new licence can be installed while the old one
    /// is still running, and the runtime picks whichever is currently best.
    /// A single-slot store forces the swap to happen exactly at expiry, which is
    /// how "renewed on the right day, still had an outage" happens.
    /// </para>
    /// </summary>
    public interface ILicenseStore
    {
        /// <summary>Every installed licence token, in no particular order.</summary>
        IReadOnlyList<string> ReadTokens();

        /// <summary>
        /// Installs a token. Re-installing the same document is a no-op rather
        /// than a duplicate.
        /// </summary>
        void Save(string token);

        /// <summary>Removes an installed token by its <c>jti</c>. Returns false when it was not there.</summary>
        bool Remove(string licenseId);

        /// <summary>Reads the sidecar state, creating a fresh one when absent.</summary>
        LicenseStoreState ReadState();

        /// <summary>Persists the sidecar state.</summary>
        void WriteState(LicenseStoreState state);
    }
}
