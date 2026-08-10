using System.Threading.Tasks;

namespace OutWit.Common.Licensing.MVVM.Platform
{
    /// <summary>
    /// Reads a licence in from a file and writes a request out to one.
    /// <para>
    /// The other of the two non-portable things a panel needs. A desktop app
    /// opens a file dialog, a browser downloads a blob, and an air-gapped
    /// customer carries a <c>.lic</c> in on a USB stick — the panel does not
    /// need to know which.
    /// </para>
    /// </summary>
    public interface ILicenseFileTransfer
    {
        /// <summary>
        /// Asks the user for a licence file and returns its contents, or
        /// <c>null</c> when they cancelled.
        /// </summary>
        Task<string?> OpenTextAsync();

        /// <summary>Saves <paramref name="content"/> under the suggested name.</summary>
        Task SaveTextAsync(string fileName, string content);
    }
}
