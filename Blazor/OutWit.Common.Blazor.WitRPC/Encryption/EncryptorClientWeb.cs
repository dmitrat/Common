using Microsoft.JSInterop;
using OutWit.Communication.Interfaces;
using System.Text;
using System.Text.Json;
using OutWit.Common.Blazor.WitRPC.Extensions;

namespace OutWit.Common.Blazor.WitRPC.Encryption
{
    /// <summary>
    /// Client-side encryptor using browser's Web Crypto API.
    /// Handles RSA key exchange and AES encryption for WitRPC communication.
    /// </summary>
    public sealed class EncryptorClientWeb : IEncryptorClient
    {
        #region Constructors

        public EncryptorClientWeb(IJSRuntime jsRuntime)
        {
            Runtime = jsRuntime;
        }

        #endregion

        #region Initialization

        public async Task<bool> InitAsync()
        {
            if (IsInitialized)
                return true;

            try
            {
                await Runtime.InvokeVoidAsync("cryptoInterop.generateKeys", 2048);

                var options = new JsonSerializerOptions
                {
                    Converters = { new DualNameJsonConverter<RSAParametersWeb>() }
                };

                var publicKeyString = await Runtime.InvokeAsync<string>("cryptoInterop.getPublicKey");
                var publicKey = JsonSerializer.Deserialize<RSAParametersWeb>(publicKeyString, options);
                var publicKeyJson = JsonSerializer.Serialize(publicKey, options);
                PublicKey = Encoding.UTF8.GetBytes(publicKeyJson);

                var privateKeyString = await Runtime.InvokeAsync<string>("cryptoInterop.getPrivateKey");
                var privateKey = JsonSerializer.Deserialize<RSAParametersWeb>(privateKeyString, options);
                var privateKeyJson = JsonSerializer.Serialize(privateKey, options);
                PrivateKey = Encoding.UTF8.GetBytes(privateKeyJson);

                IsInitialized = true;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }

        }

        #endregion

        #region IEncryptor

        public byte[] GetPublicKey()
        {
            return PublicKey ?? Array.Empty<byte>();
        }

        public byte[] GetPrivateKey()
        {
            return PrivateKey ?? Array.Empty<byte>();
        }

        public async Task<byte[]> DecryptRsa(byte[] data)
        {
            var result = await Runtime.InvokeAsync<string>("cryptoInterop.decryptRSA", Convert.ToBase64String(data));

            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        public bool ResetAes(byte[] symmetricKey, byte[] vector)
        {
            try
            {
                AesKey = Convert.ToBase64String(symmetricKey);
                AesIv = Convert.ToBase64String(vector);

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public async Task<byte[]> Encrypt(byte[] data)
        {
            var result = await Runtime.InvokeAsync<string>("cryptoInterop.encryptAes", AesKey, AesIv, Convert.ToBase64String(data));

            var bytes = Convert.FromBase64String(result.Base64UrlToBase64());

            return bytes;
        }

        public async Task<byte[]> Decrypt(byte[] data)
        {
            var result = await Runtime.InvokeAsync<string>("cryptoInterop.decryptAes", AesKey, AesIv, Convert.ToBase64String(data));

            return Convert.FromBase64String(result.Base64UrlToBase64());
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {

        }

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }

        public IJSRuntime Runtime { get; }

        private byte[]? PublicKey { get; set; }

        private byte[]? PrivateKey { get; set; }

        private string? AesKey { get; set; }

        private string? AesIv { get; set; }

        #endregion
    }
}
