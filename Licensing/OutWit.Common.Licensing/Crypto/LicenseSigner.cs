using System;
using System.Security.Cryptography;
using System.Text;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Crypto
{
    /// <summary>
    /// Signs and verifies the licence signing input.
    /// <para>
    /// Both halves live here because they are the same BCL primitive, and
    /// hiding a twenty-line ECDSA call would buy nothing — the secret is the
    /// private key, not the code that uses it. What stays private is the key
    /// vault and the books, which live in the issuing service, not in this
    /// package.
    /// </para>
    /// </summary>
    public static class LicenseSigner
    {
        #region Functions

        /// <summary>
        /// Signs <paramref name="signingInput"/> with a PEM-encoded private key.
        /// </summary>
        /// <param name="signingInput">The exact ASCII string <c>header.payload</c>.</param>
        /// <param name="privateKeyPem">PKCS#8 (or SEC1/PKCS#1) PEM private key.</param>
        /// <param name="algorithm">Algorithm the key is registered for.</param>
        /// <returns>Raw signature bytes.</returns>
        /// <exception cref="ArgumentException">The algorithm is <see cref="LicenseAlgorithm.None"/> or unknown.</exception>
        /// <exception cref="CryptographicException">The key material cannot be read, or does not match the algorithm.</exception>
        public static byte[] Sign(string signingInput, string privateKeyPem, LicenseAlgorithm algorithm)
        {
            var data = Encoding.ASCII.GetBytes(signingInput);

            if (IsEllipticCurve(algorithm))
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportFromPem(privateKeyPem);

                return ecdsa.SignData(data, HashFor(algorithm));
            }

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            return rsa.SignData(data, HashFor(algorithm), PaddingFor(algorithm));
        }

        /// <summary>
        /// Verifies a signature against a PEM-encoded public key.
        /// <para>
        /// Returns <c>false</c> for every failure, including malformed key
        /// material: a caller checking a licence wants a verdict, and a
        /// distinction between "wrong signature" and "unreadable key" here would
        /// only tell an attacker which of the two they achieved.
        /// </para>
        /// </summary>
        public static bool Verify(string signingInput, byte[] signature, string publicKeyPem, LicenseAlgorithm algorithm)
        {
            if (algorithm == LicenseAlgorithm.None || signature.Length == 0)
                return false;

            try
            {
                var data = Encoding.ASCII.GetBytes(signingInput);

                if (IsEllipticCurve(algorithm))
                {
                    using var ecdsa = ECDsa.Create();
                    ecdsa.ImportFromPem(publicKeyPem);

                    return ecdsa.VerifyData(data, signature, HashFor(algorithm));
                }

                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKeyPem);

                return rsa.VerifyData(data, signature, HashFor(algorithm), PaddingFor(algorithm));
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a fresh key pair for <paramref name="algorithm"/> and
        /// returns it as PEM — SPKI for the public half, PKCS#8 for the private.
        /// <para>
        /// Standard formats on purpose: the keys can be generated, inspected and
        /// rotated with <c>openssl</c> and stored in any secret manager, rather
        /// than being locked inside one library's serialisation.
        /// </para>
        /// </summary>
        public static (string PublicKeyPem, string PrivateKeyPem) GenerateKeyPair(LicenseAlgorithm algorithm)
        {
            if (IsEllipticCurve(algorithm))
            {
                using var ecdsa = ECDsa.Create(CurveFor(algorithm));

                return (ecdsa.ExportSubjectPublicKeyInfoPem(), ecdsa.ExportPkcs8PrivateKeyPem());
            }

            if (algorithm is LicenseAlgorithm.RS256 or LicenseAlgorithm.PS256)
            {
                using var rsa = RSA.Create(3072);

                return (rsa.ExportSubjectPublicKeyInfoPem(), rsa.ExportPkcs8PrivateKeyPem());
            }

            throw new ArgumentException($"Cannot generate a key pair for '{algorithm}'.", nameof(algorithm));
        }

        #endregion

        #region Tools

        internal static bool IsEllipticCurve(LicenseAlgorithm algorithm)
        {
            return algorithm is LicenseAlgorithm.ES256 or LicenseAlgorithm.ES384 or LicenseAlgorithm.ES512;
        }

        private static HashAlgorithmName HashFor(LicenseAlgorithm algorithm)
        {
            return algorithm switch
            {
                LicenseAlgorithm.ES256 => HashAlgorithmName.SHA256,
                LicenseAlgorithm.ES384 => HashAlgorithmName.SHA384,
                LicenseAlgorithm.ES512 => HashAlgorithmName.SHA512,
                LicenseAlgorithm.RS256 => HashAlgorithmName.SHA256,
                LicenseAlgorithm.PS256 => HashAlgorithmName.SHA256,
                _ => throw new ArgumentException($"Unsupported algorithm '{algorithm}'.", nameof(algorithm))
            };
        }

        private static RSASignaturePadding PaddingFor(LicenseAlgorithm algorithm)
        {
            return algorithm switch
            {
                LicenseAlgorithm.RS256 => RSASignaturePadding.Pkcs1,
                LicenseAlgorithm.PS256 => RSASignaturePadding.Pss,
                _ => throw new ArgumentException($"Unsupported algorithm '{algorithm}'.", nameof(algorithm))
            };
        }

        private static ECCurve CurveFor(LicenseAlgorithm algorithm)
        {
            return algorithm switch
            {
                LicenseAlgorithm.ES256 => ECCurve.NamedCurves.nistP256,
                LicenseAlgorithm.ES384 => ECCurve.NamedCurves.nistP384,
                LicenseAlgorithm.ES512 => ECCurve.NamedCurves.nistP521,
                _ => throw new ArgumentException($"Unsupported algorithm '{algorithm}'.", nameof(algorithm))
            };
        }

        #endregion
    }
}
