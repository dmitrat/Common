using System.Text.RegularExpressions;

namespace OutWit.Common.Blazor.Logging.Highlight
{
    /// <summary>
    /// Represents a highlight level with its CSS class and regex pattern.
    /// </summary>
    public sealed class HighlightLevel
    {
        /// <summary>
        /// Zero-based depth in the filter chain.
        /// </summary>
        public required int Depth { get; init; }

        /// <summary>
        /// CSS class applied to matching spans.
        /// </summary>
        public required string CssClass { get; init; }

        /// <summary>
        /// Search terms for this level.
        /// </summary>
        public required string[] Terms { get; init; }

        /// <summary>
        /// Compiled regex matching any of the <see cref="Terms"/>.
        /// </summary>
        public required Regex Regex { get; init; }
    }
}
