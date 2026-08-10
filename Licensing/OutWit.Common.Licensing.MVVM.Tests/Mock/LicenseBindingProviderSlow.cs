using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;
using OutWit.Common.Licensing.Binding;

namespace OutWit.Common.Licensing.MVVM.Tests.Mock
{
    /// <summary>
    /// A binding provider that genuinely goes asynchronous.
    /// <para>
    /// Necessary rather than pedantic: the in-process providers complete
    /// synchronously, so an await over them never leaves the calling thread and
    /// a test built on one would pass whether the context was captured or not.
    /// The real machine provider hops to the thread pool — this reproduces that
    /// without reading the machine.
    /// </para>
    /// </summary>
    internal sealed class LicenseBindingProviderSlow : ILicenseBindingProvider
    {
        #region ILicenseBindingProvider

        public LicenseBindingKind Kind => LicenseBindingKind.Machine;

        public async Task<IReadOnlyList<LicenseFactor>> CollectAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);

            return new[] { FactorHasher.ToFactor("machine-id", "test-host") };
        }

        #endregion
    }
}
