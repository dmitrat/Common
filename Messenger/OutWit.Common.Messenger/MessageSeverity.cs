namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Semantic severity of a notification message. Drives the default icon/emoji used
    /// by the <c>Send*</c> overloads on <see cref="IMessengerTransport"/>.
    /// </summary>
    public enum MessageSeverity
    {
        /// <summary>No severity — no default icon.</summary>
        None,

        /// <summary>Informational.</summary>
        Info,

        /// <summary>A successful / positive outcome.</summary>
        Success,

        /// <summary>A warning that may need attention.</summary>
        Warning,

        /// <summary>An error / failure.</summary>
        Error
    }
}
