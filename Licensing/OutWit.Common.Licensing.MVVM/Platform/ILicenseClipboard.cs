using System.Threading.Tasks;

namespace OutWit.Common.Licensing.MVVM.Platform
{
    /// <summary>
    /// Puts text on the clipboard.
    /// <para>
    /// One of exactly two things a licence panel needs that no framework-neutral
    /// package can do for itself. It is a seam rather than a dependency: a
    /// consumer that supplies one gets working copy buttons, and a consumer that
    /// does not gets them visibly disabled instead of silently inert.
    /// </para>
    /// </summary>
    public interface ILicenseClipboard
    {
        /// <summary>Copies <paramref name="text"/> to the clipboard.</summary>
        Task SetTextAsync(string text);
    }
}
