using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.MVVM;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.MVVM.Tests.Mock
{
    /// <summary>
    /// A gateway with no licensing behind it at all.
    /// <para>
    /// The panel is tested against this rather than against a real service on
    /// purpose: if the panel can be driven without a key ring, a store or a
    /// machine, then the abstraction genuinely holds and the channel
    /// implementation the service family needs will fit behind it too.
    /// </para>
    /// </summary>
    internal sealed class LicenseGatewayMock : ILicenseGateway
    {
        #region Events

        public event LicenseSnapshotEventHandler? SnapshotChanged;

        #endregion

        #region Constructors

        public LicenseGatewayMock(LicenseSnapshot? current = null)
        {
            Current = current ?? LicenseSnapshot.Empty();
        }

        #endregion

        #region ILicenseGateway

        public LicenseSnapshot Current { get; private set; }

        public Task<LicenseSnapshot> RefreshAsync()
        {
            Refreshes++;

            return Task.FromResult(Current);
        }

        public Task<LicenseInstallOutcome> InstallAsync(string token)
        {
            Installed.Add(token);

            if (Throws)
                throw new InvalidOperationException("the store is read-only");

            var outcome = NextOutcome ?? LicenseInstallOutcome.Accepted(LicenseStatus.Valid, "Licensed.", Current);

            if (outcome.IsAccepted && outcome.Snapshot != null)
                Push(outcome.Snapshot);

            return Task.FromResult(outcome);
        }

        public Task<bool> RemoveAsync(string licenseId)
        {
            Removed.Add(licenseId);

            return Task.FromResult(NextRemoveResult);
        }

        public Task<LicenseRequest> CreateRequestAsync(string? contact = null, string? notes = null)
        {
            Contact = contact;
            Notes = notes;

            return Task.FromResult(new LicenseRequest
            {
                Product = Current.Product,
                Fingerprint = Current.Fingerprint,
                Contact = contact,
                Notes = notes
            });
        }

        #endregion

        #region Functions

        /// <summary>Reports a new state the way a periodic re-evaluation would.</summary>
        public void Push(LicenseSnapshot snapshot)
        {
            Current = snapshot;

            SnapshotChanged?.Invoke(this, snapshot);
        }

        #endregion

        #region Properties

        public List<string> Installed { get; } = new();

        public List<string> Removed { get; } = new();

        public int Refreshes { get; private set; }

        public string? Contact { get; private set; }

        public string? Notes { get; private set; }

        public LicenseInstallOutcome? NextOutcome { get; set; }

        public bool NextRemoveResult { get; set; } = true;

        public bool Throws { get; set; }

        #endregion
    }
}
