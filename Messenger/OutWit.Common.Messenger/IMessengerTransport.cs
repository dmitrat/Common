using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Abstracts the byte-on-the-wire layer of sending a messenger (instant-messaging)
    /// message. Implementations translate <see cref="MessengerMessage"/> to a
    /// vendor-specific format (Telegram, Slack, Discord, etc.) and classify failures
    /// via <see cref="MessengerFailureKind"/>.
    /// </summary>
    public interface IMessengerTransport
    {
        /// <summary>
        /// Sends a fully-rendered message. Returns a result describing success or a
        /// typed failure so callers can decide whether to retry, alert ops, or mark
        /// the target bad.
        /// </summary>
        /// <param name="message">Fully-rendered message (body already produced from any template).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result with success flag and typed failure kind on error.</returns>
        Task<MessageSendResult> SendAsync(MessengerMessage message, CancellationToken ct = default);
    }
}
