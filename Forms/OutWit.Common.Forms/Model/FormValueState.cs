namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// Where a value came from, which is not the same question as what it is.
    /// </summary>
    /// <remarks>
    /// The last three exist because a form filled from somewhere else is not an empty form, and an
    /// operator who cannot tell <c>0</c> from "unknown" from "could not be read" will act on the
    /// wrong one.
    /// </remarks>
    public enum FormValueState : byte
    {
        /// <summary>Somebody typed or chose it.</summary>
        Set = 0,

        /// <summary>The schema said so and nobody has touched it.</summary>
        Default = 1,

        /// <summary>It came from the thing being configured.</summary>
        Read = 2,

        /// <summary>Worked out from something else, and worth showing as such.</summary>
        Estimated = 3,

        /// <summary>It should have been readable and was not. An error, and worth showing as one.</summary>
        NotRead = 4,

        /// <summary>This source cannot answer, which is not an error.</summary>
        NotAvailable = 5
    }
}
