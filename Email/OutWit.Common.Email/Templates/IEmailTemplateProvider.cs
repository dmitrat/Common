using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Email.Templates
{
    /// <summary>
    /// Resolves a template by name to its raw text (typically HTML with embedded
    /// placeholders, optionally with an HTML-comment subject directive that an
    /// <see cref="IEmailTemplateRenderer"/> understands).
    /// </summary>
    public interface IEmailTemplateProvider
    {
        /// <summary>
        /// Returns the raw template text for the given name.
        /// </summary>
        /// <param name="name">Template name (provider-specific resolution; for the file provider, "verify-email" maps to "Templates/verify-email.html").</param>
        /// <param name="ct">Cancellation token.</param>
        Task<string> GetTemplateAsync(string name, CancellationToken ct = default);
    }
}
