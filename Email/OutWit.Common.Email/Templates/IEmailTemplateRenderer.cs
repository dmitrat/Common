using System.Collections.Generic;

namespace OutWit.Common.Email.Templates
{
    /// <summary>
    /// Renders a raw template text into a subject + HTML body pair by substituting
    /// placeholders against the given model.
    /// </summary>
    public interface IEmailTemplateRenderer
    {
        /// <summary>
        /// Renders the template.
        /// </summary>
        /// <param name="rawHtml">Raw template body (loaded via an <see cref="IEmailTemplateProvider"/>).</param>
        /// <param name="model">Placeholder values keyed by name. Nullable values are treated as empty strings during substitution.</param>
        /// <returns>Tuple of rendered subject (extracted via renderer convention) and rendered HTML body.</returns>
        (string Subject, string Html) Render(string rawHtml, IDictionary<string, string?> model);
    }
}
