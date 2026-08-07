using System;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;
using OutWit.Common.Licensing.Crypto;
using OutWit.Common.Licensing.Issuing;
using OutWit.Common.Licensing.Keys;

namespace OutWit.Common.Licensing.Tests
{
    /// <summary>
    /// A throwaway issuing authority for tests: generates key pairs, mints
    /// licences, and hands back a matching key ring.
    /// <para>
    /// Tests mint real, signed licences rather than asserting against
    /// hand-written fixtures — a fixture would freeze whatever the format
    /// happened to be on the day it was written, and would keep passing after
    /// the format drifted away from it.
    /// </para>
    /// </summary>
    internal sealed class LicenseTestContext
    {
        #region Constants

        public const string PRODUCT = "TestProduct";
        public const string KEY_ID = "test-key-1";

        #endregion

        #region Fields

        private readonly Dictionary<string, string> m_privateKeys = new();
        private readonly List<LicenseKeyInfo> m_keys = new();

        #endregion

        #region Constructors

        public LicenseTestContext()
        {
            AddKey(KEY_ID, LicenseAlgorithm.ES256, LicenseKeyPolicy.Commercial, PRODUCT);
        }

        #endregion

        #region Functions

        /// <summary>Registers a key pair and returns its public metadata.</summary>
        public LicenseKeyInfo AddKey(string keyId, LicenseAlgorithm algorithm, LicenseKeyPolicy policy, params string[] products)
        {
            var (publicPem, privatePem) = LicenseSigner.GenerateKeyPair(algorithm);

            var info = new LicenseKeyInfo
            {
                KeyId = keyId,
                Algorithm = algorithm,
                PublicKeyPem = publicPem,
                Policy = policy,
                Products = products.ToList()
            };

            m_privateKeys[keyId] = privatePem;
            m_keys.RemoveAll(key => key.KeyId == keyId);
            m_keys.Add(info);

            return info;
        }

        /// <summary>The ring a product would embed.</summary>
        public LicenseKeyRing Ring()
        {
            return new LicenseKeyRing(m_keys);
        }

        public string PrivateKey(string keyId)
        {
            return m_privateKeys[keyId];
        }

        /// <summary>
        /// Builds a payload with sane defaults; every part is overridable so a
        /// test states only what it is about.
        /// </summary>
        public static LicensePayload Payload(
            string? product = null,
            DateTime? notBefore = null,
            DateTime? expires = null,
            LicenseBinding? binding = null,
            string appVersionRange = "",
            IReadOnlyList<string>? features = null,
            IReadOnlyDictionary<string, long>? limits = null,
            string edition = "Standard",
            bool unlimited = false)
        {
            var start = notBefore ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return new LicensePayload
            {
                Id = Guid.NewGuid().ToString("N"),
                IssuedUtc = start,
                Product = product ?? PRODUCT,
                Edition = edition,
                AppVersionRange = appVersionRange,
                NotBeforeUtc = start,
                ExpiresUtc = unlimited ? null : expires ?? start.AddYears(1),
                Binding = binding ?? LicenseBinding.None(),
                Features = features ?? Array.Empty<string>(),
                Limits = limits ?? new Dictionary<string, long>(),
                Customer = new LicenseCustomer { Id = "acme", Name = "ACME GmbH" }
            };
        }

        /// <summary>Mints a token signed by the named key.</summary>
        public string Issue(LicensePayload payload, string? keyId = null)
        {
            var id = keyId ?? KEY_ID;
            var key = m_keys.Single(candidate => candidate.KeyId == id);

            return LicenseIssuer.Issue(payload, id, key.Algorithm, PrivateKey(id));
        }

        /// <summary>Hashed host factors, as a binding provider would produce.</summary>
        public static IReadOnlyList<LicenseFactor> Factors(params (string Key, string Value)[] values)
        {
            return values.Select(pair => FactorHasher.ToFactor(pair.Key, pair.Value)).ToList();
        }

        /// <summary>A machine binding over the given raw values.</summary>
        public static LicenseBinding MachineBinding(int threshold, params (string Key, string Value)[] values)
        {
            return new LicenseBinding
            {
                Kind = LicenseBindingKind.Machine,
                Threshold = threshold,
                Factors = Factors(values)
            };
        }

        #endregion
    }
}
