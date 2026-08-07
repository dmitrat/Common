using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Samples.Avalonia.Model;

/// <summary>
/// A throwaway key pair the harness generated, with the private half kept
/// alongside so the issuer pane can actually sign with it.
/// <para>
/// Nothing here resembles how a real vendor holds keys — a real private key
/// lives in an envelope-encrypted vault on a server, never next to the code
/// that verifies. This is a harness; the keys live and die with the process.
/// </para>
/// </summary>
public sealed class SampleKey
{
    #region Constructors

    public SampleKey(LicenseKeyInfo info, string privateKeyPem)
    {
        Info = info;
        PrivateKeyPem = privateKeyPem;
    }

    #endregion

    #region Functions

    public override string ToString()
    {
        var scope = Info.Products.Count == 0 ? "unscoped" : string.Join(", ", Info.Products);

        return $"{Info.KeyId}  ·  {Info.Algorithm}  ·  {Info.Policy}  ·  [{scope}]";
    }

    #endregion

    #region Properties

    public LicenseKeyInfo Info { get; }

    public string PrivateKeyPem { get; }

    public string KeyId => Info.KeyId;

    #endregion
}
