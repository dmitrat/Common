using System.Collections.Generic;

namespace OutWit.Common.Messenger
{
    /// <summary>
    /// A fully-rendered messenger message ready to ship via an
    /// <see cref="IMessengerTransport"/>. No templating concerns — the body is
    /// already produced.
    /// </summary>
    public sealed class MessengerMessage
    {
        #region Properties

        /// <summary>
        /// Delivery target — the address of the conversation/recipient
        /// (e.g. a Telegram chat id, a channel, an <c>@username</c>).
        /// </summary>
        public string Target { get; init; } = null!;

        /// <summary>Message body (already rendered, no placeholders).</summary>
        public string Text { get; init; } = null!;

        /// <summary>
        /// Optional title/heading. Providers that support a distinct title render it
        /// above the body; others prepend it to <see cref="Text"/>.
        /// </summary>
        public string? Title { get; init; }

        /// <summary>
        /// Optional icon/emoji prepended to the rendered message so recipients can
        /// visually tell message kinds apart (e.g. <c>"⚠️"</c>, <c>"✅"</c>, <c>"❌"</c>).
        /// Set it explicitly, from <see cref="MessageEmoji"/>, or implicitly via the
        /// <c>Send*</c> severity overloads on <see cref="IMessengerTransport"/>.
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>Body rendering format. Defaults to <see cref="MessageFormat.Plain"/>.</summary>
        public MessageFormat Format { get; init; } = MessageFormat.Plain;

        /// <summary>
        /// When true, deliver silently (no push notification) where the provider
        /// supports it.
        /// </summary>
        public bool SilentNotification { get; init; }

        /// <summary>Optional provider-specific metadata (e.g. thread id, buttons).</summary>
        public IReadOnlyDictionary<string, string>? Metadata { get; init; }

        #endregion
    }
}
