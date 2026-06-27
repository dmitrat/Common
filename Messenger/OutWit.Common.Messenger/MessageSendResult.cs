namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Outcome of an <see cref="IMessengerTransport.SendAsync"/> call.
    /// </summary>
    public sealed class MessageSendResult
    {
        #region Constructors

        public MessageSendResult(bool succeeded,
            MessengerFailureKind failureKind = MessengerFailureKind.None,
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

        public static MessageSendResult Success(string? providerMessageId = null)
        {
            return new MessageSendResult(true, MessengerFailureKind.None, providerMessageId);
        }

        public static MessageSendResult Failure(MessengerFailureKind kind, string? errorMessage = null)
        {
            return new MessageSendResult(false, kind, errorMessage: errorMessage);
        }

        #endregion

        #region Properties

        public bool Succeeded { get; }

        public MessengerFailureKind FailureKind { get; }

        public string? ProviderMessageId { get; }

        public string? ErrorMessage { get; }

        #endregion
    }
}
