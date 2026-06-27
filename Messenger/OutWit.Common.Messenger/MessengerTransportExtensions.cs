using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Convenience overloads over <see cref="IMessengerTransport.SendAsync"/> so callers
    /// don't have to construct a <see cref="MessengerMessage"/> by hand. The severity
    /// helpers (<see cref="SendErrorAsync"/>, <see cref="SendWarningAsync"/>, …) tag the
    /// message with a default emoji from <see cref="MessageEmoji"/> for visual triage.
    /// </summary>
    public static class MessengerTransportExtensions
    {
        #region Generic

        /// <summary>Sends a message with an optional explicit icon/emoji.</summary>
        public static Task<MessageSendResult> SendAsync(this IMessengerTransport transport,
            string target, string text, string? icon = null,
            MessageFormat format = MessageFormat.Plain, bool silent = false,
            CancellationToken ct = default)
        {
            return transport.SendAsync(new MessengerMessage
            {
                Target = target,
                Text = text,
                Icon = icon,
                Format = format,
                SilentNotification = silent
            }, ct);
        }

        /// <summary>Sends a message, picking the default icon for <paramref name="severity"/>.</summary>
        public static Task<MessageSendResult> SendAsync(this IMessengerTransport transport,
            string target, string text, MessageSeverity severity,
            MessageFormat format = MessageFormat.Plain, bool silent = false,
            CancellationToken ct = default)
        {
            return transport.SendAsync(target, text, MessageEmoji.For(severity), format, silent, ct);
        }

        #endregion

        #region Severity Overloads

        /// <summary>Sends an informational message (ℹ️).</summary>
        public static Task<MessageSendResult> SendInfoAsync(this IMessengerTransport transport,
            string target, string text, MessageFormat format = MessageFormat.Plain,
            bool silent = false, CancellationToken ct = default)
            => transport.SendAsync(target, text, MessageSeverity.Info, format, silent, ct);

        /// <summary>Sends a success message (✅).</summary>
        public static Task<MessageSendResult> SendSuccessAsync(this IMessengerTransport transport,
            string target, string text, MessageFormat format = MessageFormat.Plain,
            bool silent = false, CancellationToken ct = default)
            => transport.SendAsync(target, text, MessageSeverity.Success, format, silent, ct);

        /// <summary>Sends a warning message (⚠️).</summary>
        public static Task<MessageSendResult> SendWarningAsync(this IMessengerTransport transport,
            string target, string text, MessageFormat format = MessageFormat.Plain,
            bool silent = false, CancellationToken ct = default)
            => transport.SendAsync(target, text, MessageSeverity.Warning, format, silent, ct);

        /// <summary>Sends an error message (❌).</summary>
        public static Task<MessageSendResult> SendErrorAsync(this IMessengerTransport transport,
            string target, string text, MessageFormat format = MessageFormat.Plain,
            bool silent = false, CancellationToken ct = default)
            => transport.SendAsync(target, text, MessageSeverity.Error, format, silent, ct);

        #endregion
    }
}
