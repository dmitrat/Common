namespace OutWit.Common.Email
{
    /// <summary>
    /// Classification of an email send failure so callers can decide what to do:
    /// retry, alert ops, mark the recipient bad, or fail hard.
    /// </summary>
    public enum EmailFailureKind
    {
        /// <summary>No failure (send succeeded).</summary>
        None,

        /// <summary>Network blip, 5xx — caller may retry.</summary>
        Transient,

        /// <summary>Bad API token / SMTP creds — do not retry, alert ops.</summary>
        AuthFailure,

        /// <summary>Bad address — do not retry, consider marking user bad.</summary>
        InvalidRecipient,

        /// <summary>HTTP 429 / SMTP 4xx throttling — retry with backoff.</summary>
        RateLimited,

        /// <summary>Any other non-transient failure — do not retry.</summary>
        Permanent
    }
}
