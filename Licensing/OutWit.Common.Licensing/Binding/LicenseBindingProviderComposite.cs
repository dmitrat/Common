using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Combines several providers into one factor set — for a licence tied to
    /// more than one family at once, such as a deployment that must also sit on
    /// a known host.
    /// </summary>
    public sealed class LicenseBindingProviderComposite : ILicenseBindingProvider
    {
        #region Fields

        private readonly IReadOnlyList<ILicenseBindingProvider> m_providers;

        #endregion

        #region Constructors

        public LicenseBindingProviderComposite(params ILicenseBindingProvider[] providers)
        {
            m_providers = providers;
        }

        #endregion

        #region ILicenseBindingProvider

        public LicenseBindingKind Kind => LicenseBindingKind.Composite;

        public async Task<IReadOnlyList<LicenseFactor>> CollectAsync()
        {
            var factors = new List<LicenseFactor>();
            var seen = new HashSet<string>();

            foreach (var provider in m_providers)
            {
                foreach (var factor in await provider.CollectAsync().ConfigureAwait(false))
                {
                    // First contributor wins a key. Two providers claiming the
                    // same factor name would otherwise let one duplicated match
                    // count twice toward a threshold.
                    if (seen.Add(factor.Key))
                        factors.Add(factor);
                }
            }

            return factors;
        }

        #endregion
    }
}
