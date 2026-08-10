using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using OutWit.Common.Aspects;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Issuing;
using OutWit.Common.Licensing.Keys;
using OutWit.Common.Licensing.Requests;
using OutWit.Common.Licensing.Samples.Avalonia.Model;
using OutWit.Common.MVVM.Commands;
using OutWit.Common.MVVM.ViewModels;

namespace OutWit.Common.Licensing.Samples.Avalonia.ViewModels;

/// <summary>
/// Stands in for the issuing service: holds throwaway keys, builds a payload,
/// and signs it — optionally with a deliberate defect.
/// </summary>
public class IssuerViewModel : ViewModelBase<ApplicationViewModel>
{
    #region Constants

    private const string PRIMARY_KEY = "sample-2026";
    private const string TRIAL_KEY = "sample-trial-2026";
    private const string FOREIGN_KEY = "other-product-2026";
    private const string UNTRUSTED_KEY = "not-in-the-ring";

    #endregion

    #region Fields

    private readonly Dictionary<string, SampleKey> m_keys = new(StringComparer.OrdinalIgnoreCase);

    private LicenseRequest? m_request;

    #endregion

    #region Constructors

    public IssuerViewModel(ApplicationViewModel applicationVm)
        : base(applicationVm)
    {
        InitDefault();
        InitCommands();
    }

    #endregion

    #region Initialization

    private void InitDefault()
    {
        Keys = new ObservableCollection<SampleKey>();
        ForgeModes = new ObservableCollection<ForgeMode>(Enum.GetValues<ForgeMode>());
        Terms = new ObservableCollection<string> { "30 days", "1 year", "3 years", "Expired last week", "Starts in 20 days", "Unlimited" };

        AddKey(PRIMARY_KEY, LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial, ApplicationViewModel.PRODUCT);
        AddKey(TRIAL_KEY, LicenseAlgorithm.ES256, LicenseKeyPolicy.TrialOnly, ApplicationViewModel.PRODUCT);
        AddKey(FOREIGN_KEY, LicenseAlgorithm.ES384, LicenseKeyPolicy.Commercial, "SomeOtherProduct");

        // Generated but never put in the ring — the product must refuse it.
        AddKey(UNTRUSTED_KEY, LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial, ApplicationViewModel.PRODUCT, trusted: false);

        SelectedKey = m_keys[PRIMARY_KEY];
        SelectedForgeMode = ForgeMode.None;
        SelectedTerm = Terms[1];

        Edition = "Enterprise";
        Features = "format.inp, format.nas";
        Limits = "maxVariants=512, maxNodes=50";
        VersionRange = ">=1.5.0 <2.0.0";
        BindToThisMachine = true;
        Threshold = 2;
    }

    private void InitCommands()
    {
        IssueCmd = new RelayCommand(_ => Issue());
        SendToProductCmd = new RelayCommand(_ => SendToProduct(), _ => !string.IsNullOrWhiteSpace(Token));
    }

    #endregion

    #region Functions

    /// <summary>The ring the product embeds — every key except the untrusted one.</summary>
    public LicenseKeyRing BuildRing()
    {
        return new LicenseKeyRing(Keys
            .Where(key => key.KeyId != UNTRUSTED_KEY)
            .Select(key => key.Info));
    }

    /// <summary>Takes the factors from a request the product produced.</summary>
    public void ImportRequest(LicenseRequest request)
    {
        m_request = request;
        RequestSummary = $"{request.Fingerprint} — {request.Factors.Count} factor(s) from {request.Host}";

        UpdateStatus();
    }

    private void Issue()
    {
        Token = Check(BuildToken, string.Empty);

        SendToProductCmd.RaiseCanExecuteChanged();
        UpdateStatus();
    }

    private void SendToProduct()
    {
        ApplicationVm.Product.Panel.PastedToken = Token;
    }

    #endregion

    #region Tools

    private string BuildToken()
    {
        var mode = SelectedForgeMode;

        var key = mode switch
        {
            ForgeMode.UnknownKey => m_keys[UNTRUSTED_KEY],
            ForgeMode.OutOfScopeKey => m_keys[FOREIGN_KEY],
            ForgeMode.TrialOverreach => m_keys[TRIAL_KEY],
            _ => SelectedKey
        };

        var payload = BuildPayload(mode);

        if (mode == ForgeMode.MismatchedAlgorithm)
            return ForgeMismatchedAlgorithm(payload, key);

        var token = LicenseIssuer.Issue(payload, key.KeyId, key.Info.Algorithm, key.PrivateKeyPem);

        return mode switch
        {
            ForgeMode.TamperedPayload => ForgeTamperedPayload(token),
            ForgeMode.BrokenToken => token.Substring(0, token.Length / 2),
            _ => token
        };
    }

    private LicensePayload BuildPayload(ForgeMode mode)
    {
        var now = ApplicationVm.Now();
        var (notBefore, expires) = ResolveTerm(now, mode);

        return new LicensePayload
        {
            Id = Guid.NewGuid().ToString("N"),
            IssuedUtc = now,
            Product = mode == ForgeMode.WrongProduct ? "SomethingElse" : ApplicationViewModel.PRODUCT,
            Edition = Edition,
            AppVersionRange = mode == ForgeMode.WrongVersion ? ">=9.0.0" : VersionRange,
            Customer = new LicenseCustomer { Id = "acme", Name = "ACME GmbH", Contact = "it@acme.example" },
            NotBeforeUtc = notBefore,
            ExpiresUtc = expires,
            Binding = ResolveBinding(mode),
            Features = ParseList(Features),
            Limits = ParseLimits(Limits)
        };
    }

    private (DateTime NotBefore, DateTime? Expires) ResolveTerm(DateTime now, ForgeMode mode)
    {
        // A trial-only key signing an unlimited term is the whole point of that
        // forge mode, so it overrides whatever term is selected.
        if (mode == ForgeMode.TrialOverreach)
            return (now.AddDays(-1), null);

        return SelectedTerm switch
        {
            "30 days" => (now.AddDays(-1), now.AddDays(30)),
            "3 years" => (now.AddDays(-1), now.AddYears(3)),
            "Expired last week" => (now.AddYears(-1), now.AddDays(-7)),
            "Starts in 20 days" => (now.AddDays(20), now.AddDays(385)),
            "Unlimited" => (now.AddDays(-1), null),
            _ => (now.AddDays(-1), now.AddYears(1))
        };
    }

    private LicenseBinding ResolveBinding(ForgeMode mode)
    {
        if (mode == ForgeMode.ForeignMachine)
            return new LicenseBinding
            {
                Kind = LicenseBindingKind.Machine,
                Threshold = 2,
                Factors = new[]
                {
                    FactorHasher.ToFactor("machine-id", "some-other-host"),
                    FactorHasher.ToFactor("primary-mac", "00:00:00:00:00:01"),
                    FactorHasher.ToFactor("machine-name", "OTHER-PC")
                }
            };

        if (!BindToThisMachine || m_request == null)
            return LicenseBinding.None();

        return new LicenseBinding
        {
            Kind = LicenseBindingKind.Machine,
            Threshold = Math.Min(Threshold, m_request.Factors.Count),
            Factors = m_request.Factors
        };
    }

    /// <summary>
    /// Signs with one algorithm while the header claims another — the classic
    /// substitution probe. The product must refuse it on the strength of what
    /// its ring says the key is for.
    /// </summary>
    private static string ForgeMismatchedAlgorithm(LicensePayload payload, SampleKey key)
    {
        var header = new LicenseTokenHeader
        {
            Algorithm = LicenseAlgorithm.ES512,
            KeyId = key.KeyId
        };

        var signingInput = LicenseToken.BuildSigningInput(header, payload);
        var signature = LicenseSigner.Sign(signingInput, key.PrivateKeyPem, key.Info.Algorithm);

        return LicenseToken.Compose(signingInput, signature);
    }

    /// <summary>Raises the edition after signing, keeping the original signature.</summary>
    private static string ForgeTamperedPayload(string token)
    {
        var parts = token.Split('.');
        var json = Encoding.UTF8.GetString(FromBase64Url(parts[1]));

        var edited = json.Replace("\"limits\":{}", "\"limits\":{\"maxNodes\":9999}");
        if (edited == json)
            edited = json.Replace("\"edition\":\"", "\"edition\":\"Forged-");

        return $"{parts[0]}.{ToBase64Url(Encoding.UTF8.GetBytes(edited))}.{parts[2]}";
    }

    private void AddKey(string keyId, LicenseAlgorithm algorithm, LicenseKeyPolicy policy, string product, bool trusted = true)
    {
        var (publicPem, privatePem) = LicenseSigner.GenerateKeyPair(algorithm);

        var key = new SampleKey(new LicenseKeyInfo
        {
            KeyId = keyId,
            Algorithm = algorithm,
            PublicKeyPem = publicPem,
            Policy = policy,
            Products = new[] { product }
        }, privatePem);

        m_keys[keyId] = key;

        if (trusted || keyId == UNTRUSTED_KEY)
            Keys.Add(key);
    }

    private void UpdateStatus()
    {
        HasRequest = m_request != null;

        TokenSummary = string.IsNullOrWhiteSpace(Token)
            ? "nothing issued yet"
            : $"{Token.Length} characters, signed by {SelectedKey.KeyId}";
    }

    private static IReadOnlyList<string> ParseList(string? value)
    {
        return (value ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToList();
    }

    private static IReadOnlyDictionary<string, long> ParseLimits(string? value)
    {
        var limits = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in ParseList(value))
        {
            var parts = entry.Split('=');

            if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out var parsed))
                limits[parts[0].Trim()] = parsed;
        }

        return limits;
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };

        return Convert.FromBase64String(padded);
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    #endregion

    #region Properties

    [Notify]
    public SampleKey SelectedKey { get; set; } = null!;

    [Notify]
    public ForgeMode SelectedForgeMode { get; set; }

    [Notify]
    public string SelectedTerm { get; set; } = string.Empty;

    [Notify]
    public string Edition { get; set; } = string.Empty;

    [Notify]
    public string Features { get; set; } = string.Empty;

    [Notify]
    public string Limits { get; set; } = string.Empty;

    [Notify]
    public string VersionRange { get; set; } = string.Empty;

    [Notify]
    public bool BindToThisMachine { get; set; }

    [Notify]
    public int Threshold { get; set; }

    [Notify]
    public string Token { get; set; } = string.Empty;

    [Notify]
    public string TokenSummary { get; set; } = "nothing issued yet";

    [Notify]
    public string RequestSummary { get; set; } = "no request imported — press “Create request” on the left";

    [Notify]
    public bool HasRequest { get; set; }

    public ObservableCollection<SampleKey> Keys { get; private set; } = null!;

    public ObservableCollection<ForgeMode> ForgeModes { get; private set; } = null!;

    public ObservableCollection<string> Terms { get; private set; } = null!;

    #endregion

    #region Commands

    public RelayCommand IssueCmd { get; private set; } = null!;

    public RelayCommand SendToProductCmd { get; private set; } = null!;

    #endregion
}
