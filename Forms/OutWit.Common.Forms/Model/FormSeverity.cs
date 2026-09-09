namespace OutWit.Common.Forms.Model
{
    /// <summary>How much an issue matters.</summary>
    public enum FormSeverity : byte
    {
        /// <summary>Worth reading, and nothing is wrong.</summary>
        Info = 0,

        /// <summary>Allowed, and probably not meant.</summary>
        Warning = 1,

        /// <summary>Not a configuration the authority will accept.</summary>
        Error = 2
    }
}
