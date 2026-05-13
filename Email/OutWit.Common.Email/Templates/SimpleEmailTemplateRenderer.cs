using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace OutWit.Common.Email.Templates
{
    /// <summary>
    /// Simple regex-based renderer. Supports:
    /// <list type="bullet">
    ///   <item><description>Subject extraction via <c>&lt;!-- subject: ... --&gt;</c> at the top of the template.</description></item>
    ///   <item><description>Sections: <c>{{#Key}}...{{/Key}}</c> — content kept only when the model value is non-empty.</description></item>
    ///   <item><description>Placeholders: <c>{{Key}}</c> — substituted from the model with HTML-escaping (except for the special <c>ActionLink</c> key, which is inserted raw).</description></item>
    /// </list>
    /// </summary>
    public class SimpleEmailTemplateRenderer : IEmailTemplateRenderer
    {
        #region Constants

        private static readonly Regex SUBJECT_RX = new(@"<!--\s*subject:\s*(.*?)\s*-->", RegexOptions.IgnoreCase);

        private static readonly Regex SECTION_RX = new(@"\{\{#(\w+)\}\}([\s\S]*?)\{\{/\1\}\}", RegexOptions.Multiline);

        private static readonly Regex TOKEN_RX = new(@"\{\{(\w+)\}\}");

        #endregion

        #region IEmailTemplateRenderer

        public (string Subject, string Html) Render(string rawHtml, IDictionary<string, string?> model)
        {
            var subject = "Notification";
            var m = SUBJECT_RX.Match(rawHtml);
            if (m.Success)
                subject = ReplaceTokens(m.Groups[1].Value, model, htmlEscape: false);

            string html = SECTION_RX.Replace(rawHtml, match =>
            {
                var key = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                return string.IsNullOrWhiteSpace(Get(model, key)) ? string.Empty : content;
            });

            html = ReplaceTokens(html, model, htmlEscape: true);

            return (subject, html);
        }

        #endregion

        #region Tools

        private static string ReplaceTokens(string text, IDictionary<string, string?> model, bool htmlEscape)
        {
            return TOKEN_RX.Replace(text, m =>
            {
                var key = m.Groups[1].Value;
                var val = Get(model, key) ?? string.Empty;
                if (!htmlEscape || key is "ActionLink")
                    return val;

                return WebUtility.HtmlEncode(val);
            });
        }

        private static string? Get(IDictionary<string, string?> dict, string key)
        {
            return dict.TryGetValue(key, out var v) ? v : null;
        }

        #endregion
    }
}
