using System.Text;
using System.Text.RegularExpressions;
using OutWit.Common.Blazor.Logging.Model;

namespace OutWit.Common.Blazor.Logging.Highlight
{
    /// <summary>
    /// Provides multi-color highlighting for log entries based on filter chain hierarchy.
    /// Each level in the filter tree gets a distinct color (up to 10 levels).
    /// </summary>
    public sealed class HighlighterLog
    {
        #region Constants

        private static readonly string[] HIGHLIGHT_CLASSES =
        [
            "log-highlight-level-0",
            "log-highlight-level-1",
            "log-highlight-level-2",
            "log-highlight-level-3",
            "log-highlight-level-4",
            "log-highlight-level-5",
            "log-highlight-level-6",
            "log-highlight-level-7",
            "log-highlight-level-8",
            "log-highlight-level-9"
        ];

        #endregion

        #region Fields

        private readonly List<HighlightLevel> m_levels = [];
        private int m_lastFilterHash;

        #endregion

        #region Public Methods

        /// <summary>
        /// Highlights search terms in the given text using multi-color spans.
        /// </summary>
        public string Highlight(string? text, LogFilterNode? selectedFilter)
        {
            if (string.IsNullOrEmpty(text) || selectedFilter == null)
                return text ?? string.Empty;

            RebuildLevelsIfNeeded(selectedFilter);

            if (m_levels.Count == 0)
                return text;

            var result = text;
            for (int i = m_levels.Count - 1; i >= 0; i--)
            {
                var level = m_levels[i];
                result = ApplyHighlight(result, level);
            }

            return result;
        }

        /// <summary>
        /// Returns the list of highlight terms grouped by level.
        /// </summary>
        public IReadOnlyList<(int Depth, string[] Terms)> GetHighlightLevels(LogFilterNode? selectedFilter)
        {
            if (selectedFilter == null)
                return Array.Empty<(int, string[])>();

            RebuildLevelsIfNeeded(selectedFilter);

            return m_levels
                .Select(l => (l.Depth, l.Terms))
                .ToArray();
        }

        #endregion

        #region Private Helpers

        private void RebuildLevelsIfNeeded(LogFilterNode selectedFilter)
        {
            var currentHash = GetFilterChainHash(selectedFilter);
            if (currentHash == m_lastFilterHash)
                return;

            m_lastFilterHash = currentHash;
            m_levels.Clear();

            var ancestors = selectedFilter
                .AncestorsAndSelf()
                .Reverse()
                .Where(n => !n.IsDisabled)
                .ToArray();

            int depth = 0;
            foreach (var node in ancestors)
            {
                var terms = node.GetHighlightTerms()
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (terms.Length == 0)
                    continue;

                var cssClass = HIGHLIGHT_CLASSES[Math.Min(depth, HIGHLIGHT_CLASSES.Length - 1)];
                var regex = BuildRegex(terms);

                m_levels.Add(new HighlightLevel
                {
                    Depth = depth,
                    CssClass = cssClass,
                    Terms = terms,
                    Regex = regex
                });

                depth++;
            }
        }

        private static Regex BuildRegex(string[] terms)
        {
            var escaped = terms.Select(Regex.Escape).ToArray();
            var pattern = string.Join("|", escaped);
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        private static string ApplyHighlight(string text, HighlightLevel level)
        {
            var result = new StringBuilder();
            int lastIndex = 0;

            foreach (Match match in level.Regex.Matches(text))
            {
                if (IsInsideHtmlTag(text, match.Index))
                    continue;

                result.Append(text[lastIndex..match.Index]);
                result.Append($"<span class=\"{level.CssClass}\">{match.Value}</span>");
                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
                result.Append(text[lastIndex..]);

            return result.ToString();
        }

        private static bool IsInsideHtmlTag(string text, int position)
        {
            int openBrackets = 0;
            int closeBrackets = 0;

            for (int i = 0; i < position; i++)
            {
                if (text[i] == '<')
                    openBrackets++;
                else if (text[i] == '>')
                    closeBrackets++;
            }

            return openBrackets > closeBrackets;
        }

        private static int GetFilterChainHash(LogFilterNode selectedFilter)
        {
            var hash = new HashCode();

            foreach (var node in selectedFilter.AncestorsAndSelf())
            {
                if (node.IsDisabled)
                    continue;

                hash.Add(node.Title);
                hash.Add(node.FullTextSearch);
                hash.Add(node.IsExclusion);
                hash.Add(node.Filters.Count);
            }

            return hash.ToHashCode();
        }

        #endregion
    }
}
