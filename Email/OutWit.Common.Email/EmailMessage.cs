using System.Collections.Generic;

namespace OutWit.Common.Email
{
    /// <summary>
    /// A fully-rendered email ready to ship via an <see cref="IEmailTransport"/>.
    /// No templating concerns — body/subject are already produced.
    /// </summary>
    public sealed class EmailMessage
    {
        #region Properties

        /// <summary>Recipient address.</summary>
        public string To { get; init; } = null!;

        /// <summary>Sender address.</summary>
        public string From { get; init; } = null!;

        /// <summary>Subject line (already rendered, no placeholders).</summary>
        public string Subject { get; init; } = null!;

        /// <summary>HTML body (already rendered, no placeholders).</summary>
        public string HtmlBody { get; init; } = null!;

        /// <summary>Optional plain-text alternative body.</summary>
        public string? TextBody { get; init; }

        /// <summary>Optional Reply-To address override.</summary>
        public string? ReplyTo { get; init; }

        /// <summary>Optional extra SMTP/transport headers (e.g. List-Unsubscribe).</summary>
        public IReadOnlyDictionary<string, string>? Headers { get; init; }

        #endregion
    }
}
