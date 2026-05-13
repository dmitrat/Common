using System.Threading;
using System.Threading.Tasks;

namespace OutWit.Common.Email
{
    /// <summary>
    /// Abstracts the byte-on-the-wire layer of sending an email. Implementations
    /// translate <see cref="EmailMessage"/> to a vendor-specific format (SMTP,
    /// Resend, AWS SES, etc.) and classify failures via <see cref="EmailFailureKind"/>.
    /// </summary>
    public interface IEmailTransport
    {
        /// <summary>
        /// Sends a fully-rendered email. Returns a result describing success or
        /// a typed failure so callers can decide whether to retry, alert ops, or
        /// mark the recipient bad.
        /// </summary>
        /// <param name="message">Fully-rendered message (subject + bodies already produced from any template).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Result with success flag and typed failure kind on error.</returns>
        Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);
    }
}
