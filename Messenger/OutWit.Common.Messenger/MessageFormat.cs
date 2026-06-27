namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Rendering format for a messenger message body. Transports map this to their
    /// own parse mode (e.g. Telegram's <c>MarkdownV2</c> / <c>HTML</c>); providers
    /// that don't support a format fall back to plain text.
    /// </summary>
    public enum MessageFormat
    {
        /// <summary>Plain text, no markup.</summary>
        Plain,

        /// <summary>Markdown markup.</summary>
        Markdown,

        /// <summary>HTML markup.</summary>
        Html
    }
}
