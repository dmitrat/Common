using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.Licensing.MVVM.Platform;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Snapshot;
using OutWit.Common.Licensing.Validation;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.Interfaces;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;

namespace OutWit.Common.Licensing.MVVM.ViewModels
{
    /// <summary>
    /// The licence panel, minus the view: every property, command and
    /// enablement rule a licence screen needs, already computed.
    /// <para>
    /// It is <b>held</b>, never inherited from. A service's page view model
    /// derives from a MudBlazor component base and a desktop one from its own
    /// application view model; a panel that insisted on being either could serve
    /// only one of them. Deriving from the framework-neutral
    /// <see cref="ViewModelBase{TApplicationVm}"/> keeps the house's
    /// ApplicationViewModel-as-container concept intact without borrowing a
    /// single line of UI.
    /// </para>
    /// <para>
    /// What it deliberately does not do is <b>gate</b>. Six products refuse six
    /// different things for six different reasons, and a generic refusal is the
    /// one thing the design forbids — so the enforcement point stays hand-written
    /// where a reviewer can read it.
    /// </para>
    /// </summary>
    public class LicensePanelViewModel<TApplicationVm> : ViewModelBase<TApplicationVm>
        where TApplicationVm : class
    {
        #region Constants

        /// <summary>How long before expiry a licensed product starts saying so.</summary>
        public const int WARNING_DAYS = 30;

        /// <summary>How long before a demo ends that its banner escalates.</summary>
        public const int DEMO_WARNING_DAYS = 7;

        #endregion

        #region Fields

        private readonly ILicenseGateway m_gateway;
        private readonly ILicenseClipboard? m_clipboard;
        private readonly ILicenseFileTransfer? m_files;
        private readonly IDispatcher? m_dispatcher;

        private LicenseRequest? m_request;
        private bool m_disposed;

        #endregion

        #region Constructors

        /// <param name="applicationVm">The consumer's root view model, as the house pattern expects.</param>
        /// <param name="gateway">Where the licence lives — in this process, or a round trip away.</param>
        /// <param name="clipboard">Optional. Without it the copy commands are visibly disabled.</param>
        /// <param name="files">Optional. Without it the open and save commands are visibly disabled.</param>
        /// <param name="dispatcher">
        /// <b>Required for any consumer with a UI thread</b>, despite being
        /// optional in the signature — see the remarks on <c>Apply</c>. Omit it
        /// only where there is no UI thread to marshal onto, such as a Blazor
        /// page or a headless host.
        /// </param>
        public LicensePanelViewModel(
            TApplicationVm applicationVm,
            ILicenseGateway gateway,
            ILicenseClipboard? clipboard = null,
            ILicenseFileTransfer? files = null,
            IDispatcher? dispatcher = null)
            : base(applicationVm)
        {
            m_gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            m_clipboard = clipboard;
            m_files = files;
            m_dispatcher = dispatcher;

            InitDefault();
            InitEvents();
            InitCommands();

            // Rendered from what the gateway already knows rather than from a
            // fire-and-forget load: a panel whose first frame is empty is the
            // "not evaluated yet" state the runtime was designed not to have.
            ApplyCore(m_gateway.Current);
        }

        #endregion

        #region Initialization

        private void InitDefault()
        {
            Grants = new ObservableCollection<LicenseGrant>();
            Warnings = new ObservableCollection<string>();
            Installed = new ObservableCollection<LicenseDocument>();

            Snapshot = LicenseSnapshot.Empty();
        }

        private void InitEvents()
        {
            m_gateway.SnapshotChanged += OnSnapshotChanged;

            PropertyChanged += OnPropertyChanged;
        }

        private void InitCommands()
        {
            RefreshCmd = new RelayCommandAsync(RefreshAsync);
            InstallCmd = new RelayCommandAsync(InstallAsync, () => CanInstall);
            ClearCmd = new RelayCommand(_ => PastedToken = string.Empty);
            RemoveCmd = new RelayCommandAsync(RemoveAsync, () => CanRemove);
            CreateRequestCmd = new RelayCommandAsync(CreateRequestAsync);

            CopyFingerprintCmd = new RelayCommandAsync(() => CopyAsync(Fingerprint), () => CanCopy);
            CopyRequestCmd = new RelayCommandAsync(() => CopyAsync(RequestBlob), () => CanCopyRequest);
            OpenLicenseFileCmd = new RelayCommandAsync(OpenLicenseFileAndInstallAsync, () => CanUseFiles);
            SaveRequestCmd = new RelayCommandAsync(SaveRequestAsync, () => CanSaveRequest);
        }

        #endregion

        #region Functions

        /// <summary>Re-reads the licence and refreshes everything shown.</summary>
        public Task RefreshAsync()
        {
            return GuardAsync(async () => Apply(await m_gateway.RefreshAsync()));
        }

        /// <summary>Installs whatever is in <see cref="PastedToken"/>.</summary>
        public Task InstallAsync()
        {
            return GuardAsync(InstallCoreAsync);
        }

        /// <summary>Uninstalls <see cref="SelectedDocument"/>.</summary>
        public Task RemoveAsync()
        {
            var document = SelectedDocument;
            if (document == null)
                return Task.CompletedTask;

            return GuardAsync(async () =>
            {
                var removed = await m_gateway.RemoveAsync(document.Id);

                InstallMessage = removed
                    ? $"Removed {document.Edition} ({document.Id})."
                    : $"{document.Edition} could not be removed — it was not installed by this product.";
                InstallSucceeded = removed;

                SelectedDocument = null;

                Apply(await m_gateway.RefreshAsync());
            });
        }

        /// <summary>Builds the request blob a customer sends to ask for a licence.</summary>
        public Task CreateRequestAsync()
        {
            return GuardAsync(async () =>
            {
                m_request = await m_gateway.CreateRequestAsync(
                    string.IsNullOrWhiteSpace(RequestContact) ? null : RequestContact,
                    string.IsNullOrWhiteSpace(RequestNotes) ? null : RequestNotes);

                RequestBlob = m_request.ToJson();
            });
        }

        /// <summary>
        /// Picks a licence file and installs it. The two halves are one command
        /// on purpose: a file dialog that filled a text box and then waited to be
        /// told to install would be a step nobody expects.
        /// </summary>
        public Task OpenLicenseFileAndInstallAsync()
        {
            if (m_files == null)
                return Task.CompletedTask;

            return GuardAsync(async () =>
            {
                var content = await m_files.OpenTextAsync();
                if (string.IsNullOrWhiteSpace(content))
                    return;

                PastedToken = content!.Trim();

                await InstallCoreAsync();
            });
        }

        /// <summary>Writes the built request out under a self-identifying name.</summary>
        public Task SaveRequestAsync()
        {
            if (m_files == null || m_request == null)
                return Task.CompletedTask;

            return GuardAsync(() => m_files.SaveTextAsync(m_request.SuggestedFileName(), RequestBlob));
        }

        #endregion

        #region Tools

        /// <summary>
        /// Applies a snapshot, arriving on whichever thread produced it.
        /// <para>
        /// That thread is very often not the caller's. Awaiting the gateway does
        /// come back to whoever asked — but <c>StateChanged</c> is not awaited
        /// by anybody: the runtime raises it from wherever its own
        /// re-evaluation finished, and every await inside the library suppresses
        /// the synchronization context by design, so a desktop host can block on
        /// its first evaluation without deadlocking. The consequence is that a
        /// snapshot reaches this method on a thread-pool thread after an
        /// ordinary install, not only when a periodic reload is switched on.
        /// </para>
        /// <para>
        /// So for any consumer with a UI thread the dispatcher is
        /// <b>required</b>. Without one the collections below are touched from
        /// the wrong thread, and Avalonia and WPF both fault — visibly, but with
        /// a message that names reflection rather than threading.
        /// </para>
        /// </summary>
        private void Apply(LicenseSnapshot snapshot)
        {
            if (m_dispatcher == null || m_dispatcher.CheckAccess())
            {
                ApplyCore(snapshot);
                return;
            }

            m_dispatcher.Invoke(() => ApplyCore(snapshot));
        }

        /// <summary>
        /// Shared by the paste box and the file dialog, so the two doors into
        /// the same act cannot drift apart.
        /// </summary>
        private async Task InstallCoreAsync()
        {
            var outcome = await m_gateway.InstallAsync(PastedToken);

            InstallMessage = outcome.Message;
            InstallSucceeded = outcome.IsAccepted;

            // Cleared only on success, so a customer who pasted half a licence
            // still has the half they pasted to look at.
            if (outcome.IsAccepted)
                PastedToken = string.Empty;

            Apply(outcome.Snapshot ?? await m_gateway.RefreshAsync());
        }

        private void ApplyCore(LicenseSnapshot snapshot)
        {
            Snapshot = snapshot;

            Mode = snapshot.Mode;
            ModeText = snapshot.Mode.ToString();
            Severity = ResolveSeverity(snapshot);

            Status = snapshot.Status.ToString();
            StatusDetail = snapshot.Description;

            CanRun = snapshot.CanRun;
            IsDemo = snapshot.IsDemo;
            IsGrace = snapshot.Mode == LicenseMode.Grace;
            IsRestricted = snapshot.Mode == LicenseMode.Restricted;
            IsClockSuspect = snapshot.IsClockSuspect;

            Fingerprint = snapshot.Fingerprint;
            Edition = Or(snapshot.Edition);
            Customer = Or(snapshot.CustomerName);
            LicenseId = snapshot.LicenseId;

            ExpiresUtc = snapshot.ExpiresUtc;
            DaysRemaining = snapshot.DaysRemaining;
            ExpiryText = ResolveExpiryText(snapshot);
            GracePolicyText = snapshot.GracePolicy;

            Replace(Grants, snapshot.Grants);
            Replace(Warnings, snapshot.UnrecognisedKeys);
            Replace(Installed, snapshot.Installed);

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            CanInstall = !string.IsNullOrWhiteSpace(PastedToken) && !IsBusy;
            CanRemove = SelectedDocument != null && !IsBusy;
            CanCopy = m_clipboard != null && !string.IsNullOrWhiteSpace(Fingerprint);
            CanCopyRequest = m_clipboard != null && !string.IsNullOrWhiteSpace(RequestBlob);
            CanUseFiles = m_files != null;
            CanSaveRequest = m_files != null && m_request != null;

            // Avalonia has no CommandManager to re-query gating on its own, and
            // Blazor has none either, so the raise happens here — in the one
            // method that recomputes it — rather than scattered across whatever
            // changed something.
            InstallCmd.RaiseCanExecuteChanged();
            RemoveCmd.RaiseCanExecuteChanged();
            CopyFingerprintCmd.RaiseCanExecuteChanged();
            CopyRequestCmd.RaiseCanExecuteChanged();
            OpenLicenseFileCmd.RaiseCanExecuteChanged();
            SaveRequestCmd.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Runs a gateway call with the busy flag set and nothing allowed to
        /// escape. A licence panel is the screen a customer reaches <i>because</i>
        /// something is wrong; it is the last place that may throw at them.
        /// </summary>
        private async Task GuardAsync(Func<Task> body)
        {
            IsBusy = true;
            UpdateStatus();

            try
            {
                await body();
            }
            catch (Exception exception)
            {
                InstallMessage = $"The operation could not be completed: {Innermost(exception).Message}";
                InstallSucceeded = false;
            }
            finally
            {
                IsBusy = false;
                UpdateStatus();
            }
        }

        /// <summary>
        /// The exception a person can do something about. Notification runs
        /// subscribers through reflection, so anything a bound view throws comes
        /// back wrapped as "Exception has been thrown by the target of an
        /// invocation" — a sentence that tells a customer nothing on the one
        /// screen whose entire job is to tell them something.
        /// </summary>
        private static Exception Innermost(Exception exception)
        {
            while (exception.InnerException != null)
                exception = exception.InnerException;

            return exception;
        }

        private Task CopyAsync(string text)
        {
            return m_clipboard == null ? Task.CompletedTask : m_clipboard.SetTextAsync(text);
        }

        private static LicenseSeverity ResolveSeverity(LicenseSnapshot snapshot)
        {
            if (snapshot.Mode == LicenseMode.Restricted)
                return LicenseSeverity.Error;

            // Grace is loud on purpose. It is not a comfortable state to sit in
            // — it is a fortnight of borrowed time that somebody has to spend.
            if (snapshot.Mode == LicenseMode.Grace)
                return LicenseSeverity.Error;

            if (snapshot.Mode == LicenseMode.Demo)
                return snapshot.DaysRemaining <= DEMO_WARNING_DAYS ? LicenseSeverity.Warning : LicenseSeverity.Info;

            return snapshot.DaysRemaining != null && snapshot.DaysRemaining <= WARNING_DAYS
                ? LicenseSeverity.Warning
                : LicenseSeverity.None;
        }

        private static string ResolveExpiryText(LicenseSnapshot snapshot)
        {
            if (snapshot.IsUnlimited)
                return "unlimited";

            var days = snapshot.DaysRemaining ?? 0;

            return days < 0
                ? $"{snapshot.ExpiresUtc:yyyy-MM-dd} ({-days} day(s) ago)"
                : $"{snapshot.ExpiresUtc:yyyy-MM-dd} ({days} day(s) remaining)";
        }

        private static string Or(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        private static void Replace<TItem>(ObservableCollection<TItem> target, System.Collections.Generic.IEnumerable<TItem> items)
        {
            target.Clear();

            foreach (var item in items)
                target.Add(item);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// The token box is filled three ways — typed, opened from a file, or
        /// pushed across by a host — and only one of those would have been
        /// noticed if gating were recomputed at the call sites.
        /// </summary>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.IsProperty((LicensePanelViewModel<TApplicationVm> panel) => panel.PastedToken) ||
                e.IsProperty((LicensePanelViewModel<TApplicationVm> panel) => panel.SelectedDocument))
                UpdateStatus();
        }

        private void OnSnapshotChanged(ILicenseGateway sender, LicenseSnapshot snapshot)
        {
            Apply(snapshot);
        }

        #endregion

        #region IDisposable

        public override void Dispose()
        {
            if (m_disposed)
                return;

            m_disposed = true;

            m_gateway.SnapshotChanged -= OnSnapshotChanged;
            PropertyChanged -= OnPropertyChanged;

            base.Dispose();
        }

        #endregion

        #region Properties

        /// <summary>The projection everything else here was derived from.</summary>
        [Notify]
        public LicenseSnapshot Snapshot { get; private set; } = null!;

        /// <summary>How the product should behave.</summary>
        [Notify]
        public LicenseMode Mode { get; private set; }

        /// <summary>The mode as a word, for a badge.</summary>
        [Notify]
        public string ModeText { get; private set; } = string.Empty;

        /// <summary>How loudly a banner should say it.</summary>
        [Notify]
        public LicenseSeverity Severity { get; private set; }

        /// <summary>The underlying verdict, as a word.</summary>
        [Notify]
        public string Status { get; private set; } = string.Empty;

        /// <summary>The sentence a person can act on.</summary>
        [Notify]
        public string StatusDetail { get; private set; } = string.Empty;

        /// <summary>Whether the product may do its licensed work.</summary>
        [Notify]
        public bool CanRun { get; private set; }

        [Notify]
        public bool IsDemo { get; private set; }

        [Notify]
        public bool IsGrace { get; private set; }

        [Notify]
        public bool IsRestricted { get; private set; }

        /// <summary>True when the clock, not the licence, is the problem.</summary>
        [Notify]
        public bool IsClockSuspect { get; private set; }

        /// <summary>The host's display code — the thing a customer reads to support.</summary>
        [Notify]
        public string Fingerprint { get; private set; } = string.Empty;

        [Notify]
        public string Edition { get; private set; } = string.Empty;

        [Notify]
        public string Customer { get; private set; } = string.Empty;

        /// <summary>The <c>jti</c> of the licence in force, for support to quote.</summary>
        [Notify]
        public string LicenseId { get; private set; } = string.Empty;

        [Notify]
        public DateTime? ExpiresUtc { get; private set; }

        [Notify]
        public int? DaysRemaining { get; private set; }

        /// <summary>Expiry and how far away it is, in one line.</summary>
        [Notify]
        public string ExpiryText { get; private set; } = string.Empty;

        /// <summary>The renewal grace in words — disclosed whether or not there is one.</summary>
        [Notify]
        public string GracePolicyText { get; private set; } = string.Empty;

        /// <summary>What the licence grants, one line per declared key.</summary>
        public ObservableCollection<LicenseGrant> Grants { get; private set; } = null!;

        /// <summary>Keys the licence carries that this build does not recognise.</summary>
        public ObservableCollection<string> Warnings { get; private set; } = null!;

        /// <summary>Every licence installed on this host.</summary>
        public ObservableCollection<LicenseDocument> Installed { get; private set; } = null!;

        /// <summary>The document <see cref="RemoveCmd"/> acts on.</summary>
        [Notify]
        public LicenseDocument? SelectedDocument { get; set; }

        /// <summary>The licence being pasted in.</summary>
        [Notify]
        public string PastedToken { get; set; } = string.Empty;

        /// <summary>Where a licence should be sent, carried into the request.</summary>
        [Notify]
        public string RequestContact { get; set; } = string.Empty;

        /// <summary>Anything the customer wants the vendor to know.</summary>
        [Notify]
        public string RequestNotes { get; set; } = string.Empty;

        /// <summary>The request blob, once one has been built.</summary>
        [Notify]
        public string RequestBlob { get; private set; } = string.Empty;

        /// <summary>What the last install or uninstall said.</summary>
        [Notify]
        public string InstallMessage { get; private set; } = string.Empty;

        /// <summary>Whether that last attempt was accepted.</summary>
        [Notify]
        public bool InstallSucceeded { get; private set; }

        /// <summary>True while a gateway call is in flight.</summary>
        [Notify]
        public bool IsBusy { get; private set; }

        [Notify]
        public bool CanInstall { get; private set; }

        [Notify]
        public bool CanRemove { get; private set; }

        [Notify]
        public bool CanCopy { get; private set; }

        [Notify]
        public bool CanCopyRequest { get; private set; }

        [Notify]
        public bool CanUseFiles { get; private set; }

        [Notify]
        public bool CanSaveRequest { get; private set; }

        #endregion

        #region Commands

        public RelayCommandAsync RefreshCmd { get; private set; } = null!;

        public RelayCommandAsync InstallCmd { get; private set; } = null!;

        public RelayCommand ClearCmd { get; private set; } = null!;

        public RelayCommandAsync RemoveCmd { get; private set; } = null!;

        public RelayCommandAsync CreateRequestCmd { get; private set; } = null!;

        public RelayCommandAsync CopyFingerprintCmd { get; private set; } = null!;

        public RelayCommandAsync CopyRequestCmd { get; private set; } = null!;

        public RelayCommandAsync OpenLicenseFileCmd { get; private set; } = null!;

        public RelayCommandAsync SaveRequestCmd { get; private set; } = null!;

        #endregion
    }
}
