using System.Collections.Generic;
using System.Threading.Tasks;
using OutWit.Common.Licensing.MVVM.Platform;

namespace OutWit.Common.Licensing.MVVM.Tests.Mock
{
    /// <summary>A file transfer that reads from a field and writes to a list.</summary>
    internal sealed class LicenseFileTransferMock : ILicenseFileTransfer
    {
        #region ILicenseFileTransfer

        public Task<string?> OpenTextAsync()
        {
            return Task.FromResult(Opened);
        }

        public Task SaveTextAsync(string fileName, string content)
        {
            Saved.Add((fileName, content));

            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        /// <summary>What the next open returns. <c>null</c> stands for a cancelled dialog.</summary>
        public string? Opened { get; set; }

        public List<(string FileName, string Content)> Saved { get; } = new();

        #endregion
    }
}
