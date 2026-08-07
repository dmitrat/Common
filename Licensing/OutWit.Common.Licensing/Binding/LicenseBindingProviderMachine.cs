using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Providers;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Ties a licence to a workstation, from the factors
    /// <c>OutWit.Common.Platform</c> reads — the OS machine identity, the
    /// primary physical network adapter, and the host name.
    /// <para>
    /// What is sold is the right to run the program on this machine, so nothing
    /// here observes who is signed in: several engineers share one licensed
    /// workstation, each working under their own account, and that is the
    /// intended arrangement rather than an abuse of it.
    /// </para>
    /// </summary>
    public sealed class LicenseBindingProviderMachine : ILicenseBindingProvider
    {
        #region Fields

        private readonly IMachineFactorsProvider m_factorsProvider;

        #endregion

        #region Constructors

        public LicenseBindingProviderMachine()
            : this(new MachineFactorsProvider())
        {
        }

        public LicenseBindingProviderMachine(IMachineFactorsProvider factorsProvider)
        {
            m_factorsProvider = factorsProvider;
        }

        #endregion

        #region ILicenseBindingProvider

        public LicenseBindingKind Kind => LicenseBindingKind.Machine;

        public async Task<IReadOnlyList<LicenseFactor>> CollectAsync()
        {
            var factors = await m_factorsProvider.CollectAsync().ConfigureAwait(false);

            return factors
                .Select(factor => FactorHasher.ToFactor(factor.Key, factor.Value))
                .ToList();
        }

        #endregion
    }
}
