using System;
using System.ComponentModel;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Avalonia.Abstractions;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.MVVM;
using OutWit.Common.Licensing.MVVM.ViewModels;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Samples.Avalonia.Platform;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;

namespace OutWit.Common.Licensing.Samples.Avalonia.ViewModels;

/// <summary>
/// Stands in for a licensed product.
/// <para>
/// What is left here after the panel moved into
/// <c>OutWit.Common.Licensing.MVVM</c> is exactly what a real product keeps:
/// the composition, and the <b>gate</b>. The gate stays hand-written on purpose
/// — six products refuse six different things for six different reasons, and a
/// generic refusal is the one thing the design forbids.
/// </para>
/// <para>
/// The harness is the panel's first consumer rather than its last, which is why
/// it migrated before any shipping product did: the abstraction gets exercised
/// while it is still free to change.
/// </para>
/// </summary>
public class ProductViewModel : ViewModelBase<ApplicationViewModel>
{
    #region Fields

    private ILicenseService? m_licensing;
    private LicenseGatewayLocal? m_gateway;

    #endregion

    #region Constructors

    public ProductViewModel(ApplicationViewModel applicationVm)
        : base(applicationVm)
    {
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitCommands()
    {
        RunCmd = new RelayCommand(_ => Run(), _ => CanRun);
    }

    #endregion

    #region Functions

    /// <summary>
    /// Rebuilds the runtime from scratch — used when the issuer's key ring or
    /// the grace policy changes, which in reality means the product shipped a
    /// new build.
    /// </summary>
    public void Rebuild()
    {
        Teardown();

        // Licensing.Create rather than a container: a desktop app composes by
        // hand, and this is the path WitSweep and the add-in will take.
        m_licensing = Licensing.Create(options => options
            .ForProduct(ApplicationVm.AppliedProductKey, ApplicationVm.ProductVersion)
            .WithKeyRing(ApplicationVm.Issuer.BuildRing())
            .WithBinding(new LicenseBindingProviderMachine())
            .WithStore(new LicenseStoreFile(ApplicationVm.LicenseDirectory))
            .WithGrace(TimeSpan.FromDays(ApplicationVm.GraceDays))
            .WithDemo(TimeSpan.FromDays(30), demo => demo
                .Limit("maxVariants", 8)
                .Feature("format.inp"))
            .Declares(vocabulary => vocabulary
                .Feature("format.inp", "CalculiX / Abaqus decks")
                .Feature("format.nas", "Nastran decks")
                .Feature("integration.prepomax", "Open in PrePoMax")
                .Limit("maxVariants", "Variants per run", 64)
                .Limit("maxNodes", "Compute nodes", 4))
            .WithClock(ApplicationVm.Now));

        m_gateway = new LicenseGatewayLocal(m_licensing);

        // All three seams supplied. The dispatcher is not optional decoration:
        // a snapshot pushed by a periodic re-evaluation arrives on a timer
        // thread, and no amount of context capture can fix an event that was
        // never awaited.
        Panel = new LicensePanelViewModel<ApplicationViewModel>(
            ApplicationVm,
            m_gateway,
            new LicenseClipboardAvalonia(),
            new LicenseFileTransferAvalonia(),
            AvaloniaDispatcher.UIThread);

        Panel.PropertyChanged += OnPanelChanged;

        UpdateStatus();
    }

    /// <summary>
    /// Re-evaluates without rebuilding — after a clock move.
    /// <para>
    /// Awaited rather than blocked on. The gateway deliberately resumes on the
    /// caller's synchronization context so a panel lands back on the UI thread;
    /// blocking a UI thread on something that wants to return to it is the
    /// classic deadlock, and it is the mirror image of why the library core
    /// suppresses the context everywhere.
    /// </para>
    /// </summary>
    public async Task RefreshAsync()
    {
        await Panel.RefreshAsync();

        UpdateStatus();
    }

    /// <summary>
    /// The gated action. One line of licensing, which is the whole claim the
    /// integration kit makes about what a product pays.
    /// </summary>
    private void Run()
    {
        if (m_licensing == null)
            return;

        RunMessage = $"Ran with maxVariants={m_licensing.Limit("maxVariants")} " +
                     $"at {ApplicationVm.Now():yyyy-MM-dd HH:mm} UTC.";
    }

    private void Teardown()
    {
        if (Panel != null)
        {
            Panel.PropertyChanged -= OnPanelChanged;
            Panel.Dispose();
        }

        m_gateway?.Dispose();
        (m_licensing as IDisposable)?.Dispose();
    }

    #endregion

    #region Tools

    private void UpdateStatus()
    {
        CanRun = Panel?.CanRun == true;

        // Avalonia has no CommandManager to re-query gating on its own.
        RunCmd.RaiseCanExecuteChanged();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// The two things the harness needs that the package deliberately does not
    /// offer: the gate, and the wire back to the issuer pane. Both are read off
    /// the panel's published properties rather than reimplemented beside it —
    /// which is the behaviour a real product should copy.
    /// </summary>
    private void OnPanelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.IsProperty((LicensePanelViewModel<ApplicationViewModel> panel) => panel.CanRun))
            UpdateStatus();

        if (e.IsProperty((LicensePanelViewModel<ApplicationViewModel> panel) => panel.RequestBlob))
            SendRequestToIssuer();
    }

    private void SendRequestToIssuer()
    {
        var request = LicenseRequest.FromJson(Panel.RequestBlob);

        if (request != null)
            ApplicationVm.Issuer.ImportRequest(request);
    }

    #endregion

    #region IDisposable

    public override void Dispose()
    {
        Teardown();

        base.Dispose();
    }

    #endregion

    #region Properties

    /// <summary>Everything the licence screen shows, supplied by the package.</summary>
    [Notify]
    public LicensePanelViewModel<ApplicationViewModel> Panel { get; private set; } = null!;

    /// <summary>Whether the pretend action may be performed.</summary>
    [Notify]
    public bool CanRun { get; set; }

    [Notify]
    public string RunMessage { get; set; } = string.Empty;

    #endregion

    #region Commands

    public RelayCommand RunCmd { get; private set; } = null!;

    #endregion
}
