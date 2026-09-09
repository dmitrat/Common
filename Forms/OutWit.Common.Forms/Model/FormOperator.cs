namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// How a condition compares a field's value with what it was given.
    /// </summary>
    /// <remarks>
    /// Deliberately finite. Everything a form can decide for itself is here; everything else is a
    /// question for whoever declared the form, one call away — see <c>IFormValidator</c>.
    /// </remarks>
    public enum FormOperator : byte
    {
        /// <summary>No comparison: the condition is whatever its children say (see <see cref="FormCondition.Conditions"/>).</summary>
        None = 0,

        Equals = 1,

        NotEquals = 2,

        /// <summary>The value is one of those given.</summary>
        In = 3,

        NotIn = 4,

        IsTrue = 5,

        IsFalse = 6,

        GreaterThan = 7,

        LessThan = 8,

        /// <summary>There is a value at all, and it is not empty.</summary>
        IsSet = 9,

        IsNotSet = 10
    }
}
