using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// What is in one field, and where it came from.
    /// </summary>
    /// <remarks>
    /// A string, whatever the field's kind: <c>true</c>, <c>12</c>, <c>00:10:00</c>, the invariant
    /// name of an enumeration member, or a comma-separated list of them. Parsing is pushed to the
    /// edges, and in exchange a filled-in form crosses a wire, a process and a browser without any
    /// serialiser knowing one of the types involved — which is the whole reason for describing a
    /// form as data.
    /// </remarks>
    [MemoryPackable]
    public partial class FormValue : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormValue()
        {
        }

        public FormValue(string? value, FormValueState state = FormValueState.Set)
        {
            Value = value;
            State = state;
        }

        #endregion

        #region Functions

        /// <summary>Whether there is anything here to act on.</summary>
        public bool IsEmpty() => string.IsNullOrEmpty(Value);

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormValue value)
                return false;

            return Value.Is(value.Value) && State.Is(value.State);
        }

        public override FormValue Clone() => new(Value, State);

        #endregion

        #region Properties

        [ToString]
        [MemoryPackOrder(0)]
        public string? Value { get; set; }

        [ToString]
        [MemoryPackOrder(1)]
        public FormValueState State { get; set; }

        #endregion
    }
}
