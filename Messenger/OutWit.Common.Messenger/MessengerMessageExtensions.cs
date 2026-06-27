namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Helpers for turning a <see cref="MessengerMessage"/> into the final text a
    /// transport sends. Centralised here so every provider renders icon/title/body
    /// consistently.
    /// </summary>
    public static class MessengerMessageExtensions
    {
        /// <summary>
        /// Composes the full text: <c>"[Icon ][Title\n\n]Text"</c>. The optional
        /// <see cref="MessengerMessage.Icon"/> is prefixed and the optional
        /// <see cref="MessengerMessage.Title"/> is placed on its own line above the body.
        /// </summary>
        public static string RenderText(this MessengerMessage message)
        {
            var body = string.IsNullOrEmpty(message.Title)
                ? message.Text
                : $"{message.Title}\n\n{message.Text}";

            return string.IsNullOrEmpty(message.Icon)
                ? body
                : $"{message.Icon} {body}";
        }
    }
}
