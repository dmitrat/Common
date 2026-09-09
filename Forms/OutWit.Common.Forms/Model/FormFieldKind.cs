namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// What a field is, which is to say what a renderer has to do with it.
    /// </summary>
    /// <remarks>
    /// Short on purpose. A kind exists when it changes how the value is entered, not when it
    /// changes what the value means: a serial number and a person's name are both
    /// <see cref="Text"/>, and nothing here needs to know the difference.
    /// </remarks>
    public enum FormFieldKind : byte
    {
        /// <summary>A line of text.</summary>
        Text = 0,

        /// <summary>Something to read and not to change. Carries no input of its own.</summary>
        Label = 1,

        /// <summary>A number, with <see cref="FormField.Minimum"/>, <see cref="FormField.Maximum"/> and <see cref="FormField.Step"/>.</summary>
        Number = 2,

        /// <summary>A length of time. The value is an invariant <c>TimeSpan</c>: <c>00:10:00</c>.</summary>
        Duration = 3,

        /// <summary>On or off. The value is <c>true</c> or <c>false</c>.</summary>
        Boolean = 4,

        /// <summary>One of <see cref="FormField.Options"/>.</summary>
        Choice = 5,

        /// <summary>Any number of <see cref="FormField.Options"/>. The value is the chosen ones, comma separated.</summary>
        MultiChoice = 6,

        /// <summary>A number chosen along a scale rather than typed.</summary>
        Range = 7,

        /// <summary>
        /// A calendar date, with no time of day. The value is an invariant <c>yyyy-MM-dd</c>.
        /// </summary>
        /// <remarks>
        /// Its own kind rather than text with a pattern, because what a date looks like is not the
        /// form's business: the same date is 03/07/1958 in one department and 7/3/58 in the next,
        /// and only whoever is drawing knows which.
        /// </remarks>
        Date = 8,

        /// <summary>
        /// A measurement, named by <see cref="FormField.QuantityKey"/> — a weight, a length. The
        /// value is the number in whatever unit the schema's world stores, and the unit on the
        /// screen is the operator's.
        /// </summary>
        /// <remarks>
        /// A weight is not a number with "kg" written after it: the same value is 70 to one
        /// operator and 154 to the next, and which one they see is a property of the workstation
        /// rather than of the form. So the form names the quantity and stops there.
        /// </remarks>
        Quantity = 9,

        /// <summary>
        /// A list of short free values — medication, indications. The value is the list, one to a
        /// line; <see cref="FormField.SuggestionsKey"/> names what may be offered while typing.
        /// </summary>
        /// <remarks>
        /// Not <see cref="MultiChoice"/>: what may be chosen there is a closed list the form
        /// carries, and this is an open one. Anything may be typed whether the suggestions have
        /// heard of it or not — a dictionary that refuses a drug is a dictionary somebody works
        /// around by leaving the field empty.
        /// </remarks>
        Tokens = 10
    }
}
