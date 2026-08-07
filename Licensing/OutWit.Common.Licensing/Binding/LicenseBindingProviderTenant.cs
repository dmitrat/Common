using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Ties a licence to a deployment rather than to hardware — a tenant slug,
    /// an installation id.
    /// <para>
    /// This is the correct binding for a containerised server, and the reason is
    /// concrete: inside a container the OS machine identity is not stable across
    /// a recreated container, so a hardware-bound server licence would die on an
    /// ordinary <c>docker compose up --force-recreate</c>. A deployment
    /// identity survives that, and still fails when the licence is dropped into
    /// somebody else's deployment.
    /// </para>
    /// </summary>
    public sealed class LicenseBindingProviderTenant : ILicenseBindingProvider
    {
        #region Constants

        /// <summary>Factor key for the tenant slug — the same string that appears in the contract.</summary>
        public const string FACTOR_TENANT = "tenant";

        /// <summary>Factor key for the installation id generated at first start and persisted in a volume.</summary>
        public const string FACTOR_INSTALL_ID = "installId";

        #endregion

        #region Fields

        private readonly IReadOnlyDictionary<string, string?> m_values;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a provider for the usual pair. Either value may be null or
        /// blank — it is then simply not contributed, and threshold matching
        /// accounts for it.
        /// </summary>
        public LicenseBindingProviderTenant(string? tenant, string? installId = null)
            : this(new Dictionary<string, string?>
            {
                [FACTOR_TENANT] = tenant,
                [FACTOR_INSTALL_ID] = installId
            })
        {
        }

        /// <summary>Creates a provider over an arbitrary set of deployment-level values.</summary>
        public LicenseBindingProviderTenant(IReadOnlyDictionary<string, string?> values)
        {
            m_values = values;
        }

        #endregion

        #region ILicenseBindingProvider

        public LicenseBindingKind Kind => LicenseBindingKind.Tenant;

        public Task<IReadOnlyList<LicenseFactor>> CollectAsync()
        {
            IReadOnlyList<LicenseFactor> factors = m_values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => FactorHasher.ToFactor(pair.Key, pair.Value))
                .ToList();

            return Task.FromResult(factors);
        }

        #endregion
    }
}
