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

        /// <summary>Factor key for the installation id generated at deploy time.</summary>
        public const string FACTOR_INSTALL_ID = "installId";

        /// <summary>
        /// Factor key for the address the deployment serves.
        /// <para>
        /// The one factor that reaches a copied deployment: a clone worth having
        /// must be reachable, and reachable somewhere else. Set it to the
        /// licensed value on a host serving a different address and worker
        /// registration, OIDC redirects and every emitted link break — the clone
        /// stops being worth having. Every other candidate can be lied about for
        /// free.
        /// </para>
        /// </summary>
        public const string FACTOR_PUBLIC_BASE_URL = "publicBaseUrl";

        /// <summary>
        /// Factor key for the identity authority the deployment trusts.
        /// <para>
        /// Strongest of the three, because it is enforced by the running system
        /// rather than by convention: the <c>iss</c> claim is checked against the
        /// discovery document, so lying about it fails token validation
        /// cryptographically.
        /// </para>
        /// </summary>
        public const string FACTOR_ISSUER = "issuer";

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

        #region Functions

        /// <summary>
        /// The settled shape for a service: installation id, the address it
        /// serves, and the identity authority it trusts — issued at 3-of-3.
        /// <para>
        /// All three must match because a deployment does not drift. That is the
        /// opposite of a workstation's tolerant 2-of-3, and for the opposite
        /// reason: a container that is recreated, upgraded or moved to new
        /// hardware keeps every one of these exactly, so anything that <i>does</i>
        /// change is a different deployment.
        /// </para>
        /// <para>
        /// The list stops at three deliberately. Each further factor multiplies
        /// the ways a paying customer's deployment dies while closing no new
        /// clone scenario — refusing a clone twice buys nothing and adds a
        /// support event.
        /// </para>
        /// </summary>
        /// <param name="installId">From <c>Licensing__InstallId</c>, or <see cref="LicenseInstallId.Resolve"/>.</param>
        /// <param name="publicBaseUrl">The public address, normalised here so a trailing slash is not a different deployment.</param>
        /// <param name="issuer">The identity authority, when the service has one.</param>
        public static LicenseBindingProviderTenant ForDeployment(string? installId, string? publicBaseUrl, string? issuer = null)
        {
            return new LicenseBindingProviderTenant(new Dictionary<string, string?>
            {
                [FACTOR_INSTALL_ID] = installId,
                [FACTOR_PUBLIC_BASE_URL] = NormalizeUrl(publicBaseUrl),
                [FACTOR_ISSUER] = NormalizeUrl(issuer)
            });
        }

        /// <summary>
        /// Trims a URL to the form both sides will agree on.
        /// <para>
        /// A trailing slash is not a different deployment, and an operator who
        /// pastes <c>https://acme.example/</c> into one config and
        /// <c>https://acme.example</c> into another has not moved anything. Left
        /// alone, those hash differently and the licence dies for a reason
        /// nobody can see. Case is handled downstream by the factor hasher.
        /// </para>
        /// </summary>
        public static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            return url!.Trim().TrimEnd('/');
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
