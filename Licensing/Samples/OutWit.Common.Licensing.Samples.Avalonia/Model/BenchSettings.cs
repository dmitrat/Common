using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutWit.Common.Licensing.Samples.Avalonia.Model;

/// <summary>
/// What the bench is pretending to be, and what it trusts, across restarts.
/// <para>
/// The harness stopped being only a demonstration of its own fictional product
/// the moment it had to consume a real key ring: a ring is exported <b>for a
/// product</b>, and a licence signed under it names that product. So the
/// product key and version became settings rather than constants, and the bench
/// can stand in for WitSweep, for a service, or for the Inventor add-in without
/// a rebuild.
/// </para>
/// <para>
/// Persisted for one blunt reason: a bench that forgot its key ring on every
/// launch would be re-pasted so often that people would stop using the real one.
/// </para>
/// </summary>
public sealed class BenchSettings
{
    #region Constants

    private const string FILE_NAME = "bench.json";

    /// <summary>The fictional product the harness licences when told nothing else.</summary>
    public const string DEFAULT_PRODUCT = "SampleProduct";

    /// <summary>The version the fictional product reports when told nothing else.</summary>
    public const string DEFAULT_VERSION = "1.5.0";

    #endregion

    #region Fields

    private static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };

    #endregion

    #region Functions

    /// <summary>
    /// Reads the settings beside the licences, falling back to the defaults for
    /// anything missing or unreadable. A bench that refused to start because its
    /// own scratch file was malformed would be a poor instrument.
    /// </summary>
    public static BenchSettings Load(string directory)
    {
        try
        {
            var path = Path.Combine(directory, FILE_NAME);

            if (!File.Exists(path))
                return new BenchSettings();

            return JsonSerializer.Deserialize<BenchSettings>(File.ReadAllText(path), JSON_OPTIONS)
                   ?? new BenchSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return new BenchSettings();
        }
    }

    /// <summary>Writes the settings beside the licences. Best effort.</summary>
    public void Save(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            File.WriteAllText(Path.Combine(directory, FILE_NAME), JsonSerializer.Serialize(this, JSON_OPTIONS));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>The running version, or the default when what is stored cannot be read as one.</summary>
    public Version Version()
    {
        return System.Version.TryParse(ProductVersion, out var parsed) ? parsed : new Version(1, 5, 0);
    }

    #endregion

    #region Properties

    /// <summary>
    /// The product key the bench claims. Must match the catalogue entry a
    /// licence is issued under, or every licence comes back
    /// <c>WrongProduct</c> — which is the check working, not a fault.
    /// </summary>
    [JsonPropertyName("product")]
    public string Product { get; set; } = DEFAULT_PRODUCT;

    /// <summary>
    /// The version the bench reports. Editable because it is half of what
    /// <c>appVer</c> is checked against, and a range cannot be exercised
    /// against a version that never moves.
    /// </summary>
    [JsonPropertyName("productVersion")]
    public string ProductVersion { get; set; } = DEFAULT_VERSION;

    /// <summary>
    /// A real <c>&lt;product&gt;.keyring.json</c> exported from WitLicense, as
    /// it was received. Kept verbatim rather than parsed and re-serialised, so
    /// what the bench trusts is exactly the artifact a product would embed.
    /// </summary>
    [JsonPropertyName("keyRing")]
    public string KeyRingJson { get; set; } = string.Empty;

    #endregion
}
