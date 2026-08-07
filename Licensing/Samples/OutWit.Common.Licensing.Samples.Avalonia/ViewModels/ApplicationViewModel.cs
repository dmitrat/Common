using System;
using System.IO;
using OutWit.Common.Aspects;
using OutWit.Common.Licensing.Samples.Avalonia.Model;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;

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
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitDefault()
    {
        ProductVersion = new Version(1, 5, 0);
        ClockOffsetDays = 0;

        Issuer = new IssuerViewModel(this);
        Product = new ProductViewModel(this);

        Product.Rebuild();

        UpdateStatus();
    }

    private void InitCommands()
    {
        TravelForwardCmd = new RelayCommand(_ => Travel(1));
        TravelBackCmd = new RelayCommand(_ => Travel(-1));
        TravelYearCmd = new RelayCommand(_ => Travel(365));
        ResetClockCmd = new RelayCommand(_ => Travel(-ClockOffsetDays));
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

    private void Travel(int days)
    {
        ClockOffsetDays += days;
        Product.Refresh();

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

    #region Properties

    /// <summary>Where the harness keeps its licences — a real folder, inspectable while it runs.</summary>
    public string LicenseDirectory { get; }

    /// <summary>The version the fictional product reports.</summary>
    public Version ProductVersion { get; private set; } = null!;

    [Notify]
    public int ClockOffsetDays { get; set; }

    [Notify]
    public string ClockDescription { get; set; } = string.Empty;

    #endregion

    #region View Models

    public IssuerViewModel Issuer { get; private set; } = null!;

    public ProductViewModel Product { get; private set; } = null!;

    #endregion

    #region Commands

    public RelayCommand TravelForwardCmd { get; private set; } = null!;

    public RelayCommand TravelBackCmd { get; private set; } = null!;

    public RelayCommand TravelYearCmd { get; private set; } = null!;

    public RelayCommand ResetClockCmd { get; private set; } = null!;

    public RelayCommand WipeCmd { get; private set; } = null!;

    #endregion
}
