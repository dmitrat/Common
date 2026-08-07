using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.Abstract;

namespace OutWit.Common.Licensing.Binding
{
    /// <summary>
    /// Contributes no factors — for products that are not tied to where they
    /// run, such as anything the vendor hosts itself.
    /// </summary>
    public sealed class LicenseBindingProviderNone : ILicenseBindingProvider
    {
        #region ILicenseBindingProvider

        public LicenseBindingKind Kind => LicenseBindingKind.None;

        public Task<IReadOnlyList<LicenseFactor>> CollectAsync()
        {
            return Task.FromResult<IReadOnlyList<LicenseFactor>>(Array.Empty<LicenseFactor>());
        }

        #endregion
    }
}
