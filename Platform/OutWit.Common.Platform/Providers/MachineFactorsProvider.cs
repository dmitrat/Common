using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Platform.Interfaces;
using OutWit.Common.Platform.Internal;
using OutWit.Common.Platform.Models.MachineIdentity;

namespace OutWit.Common.Platform.Providers
{
    /// <summary>
    /// Collects the machine's individual identity factors.
    /// <para>
    /// The OS-specific part — the machine identity itself — is delegated to a
    /// per-OS <c>IPlatformProbe</c>, exactly as
    /// <see cref="MachineIdentityProvider"/> does. The remaining factors are
    /// read through the BCL and are therefore the same code on every platform.
    /// </para>
    /// </summary>
    public sealed class MachineFactorsProvider : IMachineFactorsProvider
    {
        #region Fields

        private readonly IPlatformProbe m_probe;

        #endregion

        #region Constructors

        public MachineFactorsProvider()
            : this(PlatformProbeFactory.ForCurrentPlatform())
        {
        }

        internal MachineFactorsProvider(IPlatformProbe probe)
        {
            m_probe = probe;
        }

        #endregion

        #region IMachineFactorsProvider

        public Task<IReadOnlyList<MachineFactor>> CollectAsync()
        {
            return Task.Run<IReadOnlyList<MachineFactor>>(() =>
            {
                var factors = new List<MachineFactor>();

                Add(factors, MachineFactorKeys.MACHINE_ID, ReadMachineId());
                Add(factors, MachineFactorKeys.PRIMARY_MAC, NetworkAdapterReader.ReadPrimaryMacAddress());
                Add(factors, MachineFactorKeys.MACHINE_NAME, ReadMachineName());

                return factors;
            });
        }

        #endregion

        #region Tools

        /// <summary>
        /// Appends a factor only when it carries a value. Blank factors are
        /// dropped rather than added empty — two machines that both failed to
        /// read something must not look alike because of it.
        /// </summary>
        private static void Add(ICollection<MachineFactor> factors, string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            factors.Add(new MachineFactor
            {
                Key = key,
                Value = value!.Trim()
            });
        }

        private string? ReadMachineId()
        {
            try
            {
                return m_probe.GetRawMachineIdentity();
            }
            catch
            {
                return null;
            }
        }

        private static string? ReadMachineName()
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
