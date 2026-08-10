using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Aspects;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.Samples.Avalonia.Model;
using OutWit.Common.Licensing.Samples.Avalonia.Platform;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;
using OutWit.Common.Utils;

namespace OutWit.Common.Licensing.Samples.Avalonia.ViewModels;

/// <summary>
/// Root view model. Owns what both panes share — the clock, the licence
/// directory, and what the bench is currently pretending to be — and rebuilds
/// the product whenever one of those changes.
/// <para>
/// The product key and version are settings rather than constants because the
/// bench has to be able to stand in for a real product: a key ring is exported
/// <b>for a product</b> and a licence names one, so a bench permanently called
/// <c>SampleProduct</c> could never consume either.
/// </para>
/// </summary>
public class ApplicationViewModel : ViewModelBase<ApplicationViewModel>
{
    #region Constants

    private const string LICENSE_FOLDER = "sample-licenses";

    #endregion

    #region Fields

    private readonly BenchSettings m_settings;

    #endregion

    #region Constructors

    public ApplicationViewModel()
        : base(null!)
    {
        LicenseDirectory = Path.Combine(AppContext.BaseDirectory, LICENSE_FOLDER);

        m_settings = BenchSettings.Load(LicenseDirectory);

        InitDefault();
        InitEvents();
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitDefault()
    {
        ProductKey = m_settings.Product;
        ProductVersionText = m_settings.ProductVersion;
        KeyRingJson = m_settings.KeyRingJson;

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

        ApplyIdentityCmd = new RelayCommand(_ => Apply());
        LoadKeyRingCmd = new RelayCommandAsync(LoadKeyRingAsync);
        ClearKeyRingCmd = new RelayCommand(_ => SetKeyRing(string.Empty));
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

    /// <summary>
    /// Applies the bench identity and rebuilds.
    /// <para>
    /// Explicit rather than reactive: rebuilding collects the machine's factors,
    /// which reaches WMI on Windows, and doing that on every keystroke of a text
    /// box would make the field unusable.
    /// </para>
    /// </summary>
    public void Apply()
    {
        m_settings.Product = string.IsNullOrWhiteSpace(ProductKey) ? BenchSettings.DEFAULT_PRODUCT : ProductKey.Trim();
        m_settings.ProductVersion = string.IsNullOrWhiteSpace(ProductVersionText)
            ? BenchSettings.DEFAULT_VERSION
            : ProductVersionText.Trim();
        m_settings.KeyRingJson = KeyRingJson;

        m_settings.Save(LicenseDirectory);

        ProductKey = m_settings.Product;
        ProductVersionText = m_settings.ProductVersion;

        // The issuer's throwaway keys are scoped to the product name, so a bench
        // that changed product without regenerating them would refuse its own
        // licences with ExceedsKeyPolicy — a real refusal, for a reason that has
        // nothing to do with what is being tested.
        Issuer.Rescope();

        Product.Rebuild();

        UpdateStatus();
    }

    private async Task LoadKeyRingAsync()
    {
        var content = await new LicenseFileTransferAvalonia().OpenKeyRingAsync();

        if (!string.IsNullOrWhiteSpace(content))
            SetKeyRing(content!.Trim());
    }

    private void SetKeyRing(string json)
    {
        KeyRingJson = json;

        Apply();
    }

    /// <summary>Deletes every installed licence and the sidecar, back to a factory-fresh host.</summary>
    private void Wipe()
    {
        Check(() =>
        {
            foreach (var file in Directory.Exists(LicenseDirectory)
                         ? Directory.EnumerateFiles(LicenseDirectory)
                         : Enumerable.Empty<string>())
            {
                // The bench settings survive a wipe on purpose: a real key ring
                // is something an operator went and fetched, and losing it every
                // time the licences are cleared is how people stop using it.
                if (!file.EndsWith("bench.json", StringComparison.OrdinalIgnoreCase))
                    File.Delete(file);
            }
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

        HasKeyRing = !string.IsNullOrWhiteSpace(KeyRingJson);
        KeyRingSummary = DescribeKeyRing();
    }

    /// <summary>
    /// What the loaded ring actually contains, read back through the same
    /// parser a product would use. A ring that says nothing here is a ring the
    /// product does not trust either — which is exactly what needs to be
    /// visible the first time one is loaded.
    /// </summary>
    private string DescribeKeyRing()
    {
        if (string.IsNullOrWhiteSpace(KeyRingJson))
            return "No real ring loaded — the product trusts only the throwaway keys in this process.";

        var ring = LicenseKeyRing.FromJson(KeyRingJson);

        if (ring.Keys.Count == 0)
            return "Loaded, but it parses to nothing. A product embedding this would refuse every licence with UnknownKeyId.";

        var keys = ring.Keys.Select(key =>
            $"{key.KeyId} · {key.Algorithm} · {key.Policy} · [{string.Join(", ", key.Products)}]");

        return $"{ring.Keys.Count} key(s): {string.Join("   ", keys)}";
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

    /// <summary>
    /// The product key the runtime was actually built with — distinct from
    /// <see cref="ProductKey"/>, which is what is currently typed in the box.
    /// </summary>
    public string AppliedProductKey => m_settings.Product;

    /// <summary>The version in force, as the runtime was built with it.</summary>
    public Version ProductVersion => m_settings.Version();

    /// <summary>The product key being edited. Takes effect on Apply.</summary>
    [Notify]
    public string ProductKey { get; set; } = string.Empty;

    /// <summary>The version being edited. Takes effect on Apply.</summary>
    [Notify]
    public string ProductVersionText { get; set; } = string.Empty;

    /// <summary>A real exported ring, verbatim. Empty means throwaway keys only.</summary>
    [Notify]
    public string KeyRingJson { get; set; } = string.Empty;

    /// <summary>What that ring contains, read back through the product's own parser.</summary>
    [Notify]
    public string KeyRingSummary { get; set; } = string.Empty;

    /// <summary>
    /// True when a real ring is loaded. A stored property rather than a
    /// computed one because a binding needs to be told it changed, and a
    /// getter over another property is never announced.
    /// </summary>
    [Notify]
    public bool HasKeyRing { get; set; }

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

    public RelayCommand ApplyIdentityCmd { get; private set; } = null!;

    public RelayCommandAsync LoadKeyRingCmd { get; private set; } = null!;

    public RelayCommand ClearKeyRingCmd { get; private set; } = null!;

    #endregion
}
