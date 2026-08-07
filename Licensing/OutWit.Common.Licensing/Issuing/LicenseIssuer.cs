using System;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Issuing
{
    /// <summary>
    /// Turns a payload into a signed licence token.
    /// <para>
    /// It lives in the public package alongside verification because they are
    /// the same primitive, and because a product that can only verify cannot be
    /// tested end to end. What stays private is not this code — it is the key
    /// vault and the books, which belong to the issuing service.
    /// </para>
    /// </summary>
    public static class LicenseIssuer
    {
        #region Functions

        /// <summary>
        /// Signs <paramref name="payload"/> and returns the token.
        /// </summary>
        /// <param name="payload">What is being granted.</param>
        /// <param name="keyId">The <c>kid</c> to record in the header.</param>
        /// <param name="algorithm">Algorithm the key is registered for.</param>
        /// <param name="privateKeyPem">PEM private key.</param>
        public static string Issue(LicensePayload payload, string keyId, LicenseAlgorithm algorithm, string privateKeyPem)
        {
            var header = new LicenseTokenHeader
            {
                Algorithm = algorithm,
                KeyId = keyId,
                Type = LicenseTokenHeader.TOKEN_TYPE
            };

            var signingInput = LicenseToken.BuildSigningInput(header, payload);
            var signature = LicenseSigner.Sign(signingInput, privateKeyPem, algorithm);

            return LicenseToken.Compose(signingInput, signature);
        }

        /// <summary>
        /// Signs using a registered key's metadata, refusing when that key is no
        /// longer allowed to sign or is not scoped to the payload's product.
        /// <para>
        /// The same rules the verifier enforces, applied at issue time — so a
        /// mis-scoped licence is caught by the operator who can fix it, rather
        /// than by a customer who cannot.
        /// </para>
        /// </summary>
        public static string Issue(LicensePayload payload, LicenseKeyInfo key, string privateKeyPem, DateTime utcNow)
        {
            if (!key.CanSign(utcNow))
                throw new InvalidOperationException($"Key '{key.KeyId}' was retired on {key.RetiredUtc:yyyy-MM-dd} and may no longer sign.");

            if (!key.CoversProduct(payload.Product))
                throw new InvalidOperationException($"Key '{key.KeyId}' is not scoped to product '{payload.Product}'.");

            return Issue(payload, key.KeyId, key.Algorithm, privateKeyPem);
        }

        #endregion
    }
}
