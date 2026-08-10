using System;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;

namespace OutWit.Common.Licensing.MVVM
{
    /// <summary>
    /// The gateway for a product that holds its own licence: a desktop
    /// application, a host add-in, the harness.
    /// <para>
    /// It wraps rather than owns. The service's lifetime belongs to whoever
    /// composed it — a container, or an <c>App.axaml.cs</c> — and disposing this
    /// only lets go of the subscription.
    /// </para>
    /// </summary>
    public sealed class LicenseGatewayLocal : ILicenseGateway, IDisposable
    {
        #region Events

        public event LicenseSnapshotEventHandler? SnapshotChanged;

        #endregion

        #region Fields

        private readonly ILicenseService m_service;

        private bool m_disposed;

        #endregion

        #region Constructors

        public LicenseGatewayLocal(ILicenseService service)
        {
            m_service = service ?? throw new ArgumentNullException(nameof(service));

            InitEvents();
        }

        #endregion

        #region Initialization

        private void InitEvents()
        {
            m_service.StateChanged += OnStateChanged;
        }

        #endregion

        #region ILicenseGateway

        public LicenseSnapshot Current => m_service.Snapshot;

        // Nothing below captures ConfigureAwait(false), and that is the one
        // deliberate difference between this layer and the library under it.
        // The core suppresses the context because a desktop host blocks on its
        // first evaluation and would otherwise deadlock at startup. A gateway is
        // the opposite case: it is the boundary a view model awaits, so a caller
        // on a UI thread must come back to it. Suppressing the context here
        // would return every answer on a thread-pool thread and every panel
        // would fault on the first collection it touched.
        public async Task<LicenseSnapshot> RefreshAsync()
        {
            await m_service.ReloadAsync();

            return m_service.Snapshot;
        }

        public async Task<LicenseInstallOutcome> InstallAsync(string token)
        {
            var result = await m_service.InstallAsync(token);

            // NotYetValid counts as accepted: staging a renewal ahead of its
            // start date is the intended way to renew without an outage, and a
            // panel that called it a failure would train people not to do it.
            var accepted = result.Status is LicenseStatus.Valid or LicenseStatus.NotYetValid;

            return accepted
                ? LicenseInstallOutcome.Accepted(result.Status, result.Describe(), m_service.Snapshot)
                : LicenseInstallOutcome.Rejected(result.Status, result.Describe());
        }

        public Task<bool> RemoveAsync(string licenseId)
        {
            return m_service.RemoveAsync(licenseId);
        }

        public Task<LicenseRequest> CreateRequestAsync(string? contact = null, string? notes = null)
        {
            return m_service.CreateRequestAsync(contact: contact, notes: notes);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            m_service.StateChanged -= OnStateChanged;
        }

        #endregion

        #region Event Handlers

        private void OnStateChanged(ILicenseService sender, LicenseState state)
        {
            SnapshotChanged?.Invoke(this, sender.Snapshot);
        }

        #endregion
    }
}
