using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Values;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// One of the answers a field offers.
    /// </summary>
    [MemoryPackable]
    public partial class FormOption : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormOption()
        {
        }

        public FormOption(string value, string textKey)
        {
            Value = value;
            TextKey = textKey;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormOption option)
                return false;

            return Value.Is(option.Value) &&
                   TextKey.Is(option.TextKey) &&
                   IconKey.Is(option.IconKey) &&
                   VisibleWhen.Check(option.VisibleWhen);
        }

        public override FormOption Clone()
        {
            return new FormOption
            {
                Value = Value,
                TextKey = TextKey,
                IconKey = IconKey,
                VisibleWhen = VisibleWhen?.Clone()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What is stored when this is chosen: an invariant string, and for an enumeration the
        /// invariant name of the member rather than its number.
        /// </summary>
        /// <remarks>
        /// A name and not a number because the two sides of a wire version separately: a member
        /// added in the middle of an enumeration renumbers everything after it, and a stored
        /// number then means something else entirely.
        /// </remarks>
        [ToString]
        [MemoryPackOrder(0)]
        public string Value { get; set; } = string.Empty;

        /// <summary>The localisation key for what is shown.</summary>
        [ToString]
        [MemoryPackOrder(1)]
        public string TextKey { get; set; } = string.Empty;

        [MemoryPackOrder(2)]
        public string? IconKey { get; set; }

        /// <summary>
        /// When this option is offered at all. For a choice a device supports only in some
        /// configurations, which is a thing about the form and not about the value.
        /// </summary>
        [MemoryPackOrder(3)]
        public FormCondition? VisibleWhen { get; set; }

        #endregion
    }
}
