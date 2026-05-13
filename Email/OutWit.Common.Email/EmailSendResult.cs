namespace OutWit.Common.Email
{
    /// <summary>
    /// Outcome of an <see cref="IEmailTransport.SendAsync"/> call.
    /// </summary>
    public sealed class EmailSendResult
    {
        #region Constructors

        public EmailSendResult(bool succeeded,
            EmailFailureKind failureKind = EmailFailureKind.None,
            string? providerMessageId = null,
            string? errorMessage = null)
        {
            Succeeded = succeeded;
            FailureKind = failureKind;
            ProviderMessageId = providerMessageId;
            ErrorMessage = errorMessage;
        }

        #endregion

        #region Factory

        public static EmailSendResult Success(string? providerMessageId = null)
        {
            return new EmailSendResult(true, EmailFailureKind.None, providerMessageId);
        }

        public static EmailSendResult Failure(EmailFailureKind kind, string? errorMessage = null)
        {
            return new EmailSendResult(false, kind, errorMessage: errorMessage);
        }

        #endregion

        #region Properties

        public bool Succeeded { get; }

        public EmailFailureKind FailureKind { get; }

        public string? ProviderMessageId { get; }

        public string? ErrorMessage { get; }

        #endregion
    }
}
