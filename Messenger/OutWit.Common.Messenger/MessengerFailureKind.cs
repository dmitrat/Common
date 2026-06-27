namespace OutWit.Common.Messenger
{
    /// <summary>
    /// Classification of a messenger send failure so callers can decide what to do:
    /// retry, alert ops, mark the target bad, or fail hard.
    /// </summary>
    public enum MessengerFailureKind
    {
        /// <summary>No failure (send succeeded).</summary>
        None,

        /// <summary>Network blip, 5xx — caller may retry.</summary>
        Transient,

        /// <summary>Bad bot token / credentials — do not retry, alert ops.</summary>
        AuthFailure,

        /// <summary>Bad chat/target (not found, blocked) — do not retry, consider marking target bad.</summary>
        InvalidRecipient,

        /// <summary>HTTP 429 throttling — retry with backoff.</summary>
        RateLimited,

        /// <summary>Any other non-transient failure — do not retry.</summary>
        Permanent
    }
}
