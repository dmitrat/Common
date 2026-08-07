using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using OutWit.Common.Aspects;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Storage;
using OutWit.Common.Licensing.Validation;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;

namespace OutWit.Common.Licensing.Samples.Avalonia.ViewModels;

/// <summary>
/// Stands in for a licensed product: shows what the licence grants, and gates a
/// pretend action on it.
/// <para>
/// It consumes nothing but <see cref="ILicenseService"/> — the same surface a
/// real product sees — so whatever this pane can express, a real product can
/// express too.
/// </para>
/// </summary>
public class ProductViewModel : ViewModelBase<ApplicationViewModel>
{
    #region Fields

    private ILicenseService m_licensing = null!;

    #endregion

    #region Constructors

    public ProductViewModel(ApplicationViewModel applicationVm)
        : base(applicationVm)
    {
        InitDefault();
        InitEvents();
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitDefault()
    {
        Grants = new ObservableCollection<string>();
        Warnings = new ObservableCollection<string>();
        Installed = new ObservableCollection<string>();
    }

    private void InitEvents()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void InitCommands()
    {
        InstallCmd = new RelayCommand(_ => Install(), _ => CanInstall);
        RunCmd = new RelayCommand(_ => Run(), _ => CanRun);
        RequestCmd = new RelayCommand(_ => CreateRequest());
        ClearCmd = new RelayCommand(_ => PastedToken = string.Empty);
    }

    #endregion

    #region Functions

    /// <summary>
    /// Rebuilds the service from scratch — used when the issuer's key ring
    /// changes, which in reality means the product shipped a new build.
    /// </summary>
    public void Rebuild()
    {
        var options = new LicensingOptions()
            .ForProduct(ApplicationViewModel.PRODUCT, ApplicationVm.ProductVersion)
            .WithKeyRing(ApplicationVm.Issuer.BuildRing())
            .WithBinding(new LicenseBindingProviderMachine())
            .WithStore(new LicenseStoreFile(ApplicationVm.LicenseDirectory))
            .WithDemo(TimeSpan.FromDays(30), demo => demo
                .Limit("maxVariants", 8)
                .Feature("format.inp"))
            .Declares(vocabulary => vocabulary
                .Feature("format.inp", "CalculiX / Abaqus decks")
                .Feature("format.nas", "Nastran decks")
                .Feature("integration.prepomax", "Open in PrePoMax")
                .Limit("maxVariants", "Variants per run", 64)
                .Limit("maxNodes", "Compute nodes", 4))
            .WithClock(ApplicationVm.Now);

        m_licensing = new LicenseService(options);

        Refresh();
    }

    /// <summary>Re-evaluates without rebuilding — after a clock move or an install.</summary>
    public void Refresh()
    {
        Check(() => m_licensing.ReloadAsync().GetAwaiter().GetResult());

        UpdateStatus();
    }

    private void Install()
    {
        var result = Check(() => m_licensing.InstallAsync(PastedToken).GetAwaiter().GetResult(),
            LicenseValidationResult.Failure(LicenseStatus.Malformed));

        InstallMessage = $"{result.Status} — {result.Describe()}";

        if (result.IsValid || result.Status == LicenseStatus.NotYetValid)
            PastedToken = string.Empty;

        Refresh();
    }

    private void Run()
    {
        RunMessage = $"Ran with maxVariants={m_licensing.Limit("maxVariants")} " +
                     $"at {ApplicationVm.Now():yyyy-MM-dd HH:mm} UTC.";
    }

    private void CreateRequest()
    {
        var request = Check(() => m_licensing
            .CreateRequestAsync(contact: "it@acme.example", notes: "Sample harness")
            .GetAwaiter().GetResult(), null!);

        if (request == null)
            return;

        RequestBlob = request.ToJson();
        ApplicationVm.Issuer.ImportRequest(request);
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// The token box is filled two ways — typed by a person, or pushed across
    /// from the issuer pane — and only one of those would have been noticed if
    /// gating were recomputed at the call sites instead of here.
    /// </summary>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.IsProperty((ProductViewModel vm) => vm.PastedToken))
            UpdateInstallStatus();
    }

    #endregion

    #region Tools

    private void UpdateInstallStatus()
    {
        CanInstall = !string.IsNullOrWhiteSpace(PastedToken);

        InstallCmd.RaiseCanExecuteChanged();
    }

    private void UpdateStatus()
    {
        var state = m_licensing.State;

        Status = state.Status.ToString();
        StatusDetail = state.Describe();
        IsDemo = state.IsDemo;
        CanRun = state.CanRun;
        Fingerprint = state.Fingerprint;
        Edition = state.Payload?.Edition ?? "—";
        Customer = state.Payload?.Customer?.Name ?? "—";

        Grants.Clear();
        Grants.Add($"maxVariants = {Describe(m_licensing.Limit("maxVariants"))}");
        Grants.Add($"maxNodes = {Describe(m_licensing.Limit("maxNodes"))}");
        Grants.Add($"format.inp: {Mark(m_licensing.HasFeature("format.inp"))}");
        Grants.Add($"format.nas: {Mark(m_licensing.HasFeature("format.nas"))}");
        Grants.Add($"integration.prepomax: {Mark(m_licensing.HasFeature("integration.prepomax"))}");

        Warnings.Clear();
        foreach (var key in state.UnrecognisedKeys)
            Warnings.Add($"licence grants unknown {key}");

        Installed.Clear();
        foreach (var summary in DescribeInstalled())
            Installed.Add(summary);

        UpdateInstallStatus();

        // Avalonia has no CommandManager to re-query gating on its own, so the
        // raise happens here — in the one method that recomputes it — rather
        // than being scattered across the callers that changed something.
        RunCmd.RaiseCanExecuteChanged();
    }

    private System.Collections.Generic.IEnumerable<string> DescribeInstalled()
    {
        var store = new LicenseStoreFile(ApplicationVm.LicenseDirectory);

        foreach (var token in store.ReadTokens())
        {
            var parsed = LicenseToken.Parse(token);

            yield return parsed == null
                ? "· (unreadable file)"
                : $"· {parsed.Payload.Edition} — {Term(parsed)} — kid {parsed.Header.KeyId}";
        }
    }

    private static string Term(LicenseToken token)
    {
        return token.Payload.IsUnlimited
            ? "unlimited"
            : $"{token.Payload.NotBeforeUtc:yyyy-MM-dd} → {token.Payload.ExpiresUtc:yyyy-MM-dd}";
    }

    private static string Describe(long value)
    {
        return value == long.MaxValue ? "unlimited" : value.ToString();
    }

    private static string Mark(bool granted)
    {
        return granted ? "yes" : "no";
    }

    #endregion

    #region Properties

    [Notify]
    public string Status { get; set; } = string.Empty;

    [Notify]
    public string StatusDetail { get; set; } = string.Empty;

    [Notify]
    public bool IsDemo { get; set; }

    [Notify]
    public bool CanRun { get; set; }

    [Notify]
    public string Fingerprint { get; set; } = string.Empty;

    [Notify]
    public string Edition { get; set; } = string.Empty;

    [Notify]
    public string Customer { get; set; } = string.Empty;

    [Notify(NotifyAlso = nameof(CanInstall))]
    public string PastedToken { get; set; } = string.Empty;

    [Notify]
    public bool CanInstall { get; set; }

    [Notify]
    public string InstallMessage { get; set; } = string.Empty;

    [Notify]
    public string RunMessage { get; set; } = string.Empty;

    [Notify]
    public string RequestBlob { get; set; } = string.Empty;

    public ObservableCollection<string> Grants { get; private set; } = null!;

    public ObservableCollection<string> Warnings { get; private set; } = null!;

    public ObservableCollection<string> Installed { get; private set; } = null!;

    #endregion

    #region Commands

    public RelayCommand InstallCmd { get; private set; } = null!;

    public RelayCommand RunCmd { get; private set; } = null!;

    public RelayCommand RequestCmd { get; private set; } = null!;

    public RelayCommand ClearCmd { get; private set; } = null!;

    #endregion
}
