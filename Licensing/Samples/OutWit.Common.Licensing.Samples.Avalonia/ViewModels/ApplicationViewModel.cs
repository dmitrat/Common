using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;

namespace OutWit.Common.Licensing.Samples.Avalonia.ViewModels;

/// <summary>
/// Root view model. Owns the two things both panes share — the clock and the
/// licence directory — and rebuilds the product whenever the issuer changes
/// something the product would have been built against.
/// </summary>
public class ApplicationViewModel : ViewModelBase<ApplicationViewModel>
{
    #region Constants

    /// <summary>The fictional product the harness licences.</summary>
    public const string PRODUCT = "SampleProduct";

    private const string LICENSE_FOLDER = "sample-licenses";

    #endregion

    #region Constructors

    public ApplicationViewModel()
        : base(null!)
    {
        LicenseDirectory = Path.Combine(AppContext.BaseDirectory, LICENSE_FOLDER);

        InitDefault();
        InitEvents();
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitDefault()
    {
        ProductVersion = new Version(1, 5, 0);
        ClockOffsetDays = 0;
        GraceDays = 0;

        Issuer = new IssuerViewModel(this);
        Product = new ProductViewModel(this);

        Product.Rebuild();

        UpdateStatus();
    }

    private void InitEvents()
    {
        PropertyChanged += OnPropertyChanged;
    }

    private void InitCommands()
    {
        // Asynchronous, because the panel's refresh returns to the UI thread by
        // design and blocking on it here would deadlock the window.
        TravelForwardCmd = new RelayCommandAsync(() => TravelAsync(1));
        TravelBackCmd = new RelayCommandAsync(() => TravelAsync(-1));
        TravelYearCmd = new RelayCommandAsync(() => TravelAsync(365));
        ResetClockCmd = new RelayCommandAsync(() => TravelAsync(-ClockOffsetDays));
        WipeCmd = new RelayCommand(_ => Wipe());
    }

    #endregion

    #region Functions

    /// <summary>
    /// The instant the product believes it is. Travel is the only honest way to
    /// exercise expiry, a staged renewal and clock tampering — waiting a year
    /// for a test is not a test.
    /// </summary>
    public DateTime Now()
    {
        return DateTime.UtcNow.AddDays(ClockOffsetDays);
    }

    private async Task TravelAsync(int days)
    {
        ClockOffsetDays += days;

        await Product.RefreshAsync();

        UpdateStatus();
    }

    /// <summary>Deletes every installed licence and the sidecar, back to a factory-fresh host.</summary>
    private void Wipe()
    {
        Check(() =>
        {
            if (Directory.Exists(LicenseDirectory))
                Directory.Delete(LicenseDirectory, recursive: true);
        });

        Product.Rebuild();
        UpdateStatus();
    }

    #endregion

    #region Tools

    private void UpdateStatus()
    {
        ClockDescription = ClockOffsetDays == 0
            ? $"real time — {Now():yyyy-MM-dd HH:mm} UTC"
            : $"{Now():yyyy-MM-dd HH:mm} UTC  ({ClockOffsetDays:+#;-#;0} days)";
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Renewal grace belongs to the build, so changing it here means shipping a
    /// different product — which is exactly what a rebuild stands for. It is a
    /// control rather than a constant because a mode nothing can reach is a mode
    /// nobody can trust, and Grace is otherwise unreachable at the default of
    /// zero days.
    /// </summary>
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.IsProperty((ApplicationViewModel vm) => vm.GraceDays))
            Product.Rebuild();
    }

    #endregion

    #region Properties

    /// <summary>Where the harness keeps its licences — a real folder, inspectable while it runs.</summary>
    public string LicenseDirectory { get; }

    /// <summary>The version the fictional product reports.</summary>
    public Version ProductVersion { get; private set; } = null!;

    [Notify]
    public int ClockOffsetDays { get; set; }

    [Notify]
    public string ClockDescription { get; set; } = string.Empty;

    /// <summary>The product's renewal grace. Zero — the default — makes expiry immediate.</summary>
    [Notify]
    public int GraceDays { get; set; }

    #endregion

    #region View Models

    public IssuerViewModel Issuer { get; private set; } = null!;

    public ProductViewModel Product { get; private set; } = null!;

    #endregion

    #region Commands

    public RelayCommandAsync TravelForwardCmd { get; private set; } = null!;

    public RelayCommandAsync TravelBackCmd { get; private set; } = null!;

    public RelayCommandAsync TravelYearCmd { get; private set; } = null!;

    public RelayCommandAsync ResetClockCmd { get; private set; } = null!;

    public RelayCommand WipeCmd { get; private set; } = null!;

    #endregion
}
