namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Default emoji icons for message severities. Use the constants as an explicit
    /// <see cref="MessengerMessage.Icon"/>, or let the <c>Send*</c> overloads pick one
    /// from a <see cref="MessageSeverity"/>.
    /// </summary>
    public static class MessageEmoji
    {
        #region Constants

        public const string Info = "ℹ️";

        public const string Success = "✅";

        public const string Warning = "⚠️";

        public const string Error = "❌";

        #endregion

        #region Functions

        /// <summary>
        /// Returns the default emoji for a severity, or <c>null</c> for
        /// <see cref="MessageSeverity.None"/>.
        /// </summary>
        public static string? For(MessageSeverity severity)
        {
            return severity switch
            {
                MessageSeverity.Info => Info,
                MessageSeverity.Success => Success,
                MessageSeverity.Warning => Warning,
                MessageSeverity.Error => Error,
                _ => null
            };
        }

        #endregion
    }
}
