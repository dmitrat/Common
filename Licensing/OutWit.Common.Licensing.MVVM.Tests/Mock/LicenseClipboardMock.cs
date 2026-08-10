using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.MVVM.Platform;

namespace OutWit.Common.Licensing.MVVM.Tests.Mock
{
    /// <summary>A clipboard that remembers instead of copying.</summary>
    internal sealed class LicenseClipboardMock : ILicenseClipboard
    {
        #region ILicenseClipboard

        public Task SetTextAsync(string text)
        {
            Copied.Add(text);

            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        public List<string> Copied { get; } = new();

        #endregion
    }
}
