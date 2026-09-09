using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Attributes;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// One thing on the form: what it is called, what kind of answer it takes, and when it is live.
    /// </summary>
    /// <remarks>
    /// Nothing here says what the field <i>means</i>. A renderer that draws this correctly has no
    /// idea whether it is setting a recording length or a font size, and that is what lets the same
    /// renderer draw both.
    /// </remarks>
    [MemoryPackable]
    public partial class FormField : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormField()
        {
        }

        public FormField(string key, string headerKey, FormFieldKind kind)
        {
            Key = key;
            HeaderKey = headerKey;
            Kind = kind;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormField field)
                return false;

            return Key.Is(field.Key) &&
                   HeaderKey.Is(field.HeaderKey) &&
                   HintKey.Is(field.HintKey) &&
                   Kind.Is(field.Kind) &&
                   IsReadOnly.Is(field.IsReadOnly) &&
                   Minimum.Is(field.Minimum, tolerance) &&
                   Maximum.Is(field.Maximum, tolerance) &&
                   Step.Is(field.Step, tolerance) &&
                   UnitKey.Is(field.UnitKey) &&
                   QuantityKey.Is(field.QuantityKey) &&
                   IsOptional.Is(field.IsOptional) &&
                   IsMirror.Is(field.IsMirror) &&
                   SuggestionsKey.Is(field.SuggestionsKey) &&
                   DefaultValue.Is(field.DefaultValue) &&
                   Options.Is(field.Options) &&
                   Presentation.Is(field.Presentation) &&
                   Span.Is(field.Span) &&
                   EnabledWhen.Check(field.EnabledWhen) &&
                   VisibleWhen.Check(field.VisibleWhen);
        }

        public override FormField Clone()
        {
            return new FormField
            {
                Key = Key,
                HeaderKey = HeaderKey,
                HintKey = HintKey,
                Kind = Kind,
                IsReadOnly = IsReadOnly,
                Minimum = Minimum,
                Maximum = Maximum,
                Step = Step,
                UnitKey = UnitKey,
                QuantityKey = QuantityKey,
                IsOptional = IsOptional,
                IsMirror = IsMirror,
                SuggestionsKey = SuggestionsKey,
                DefaultValue = DefaultValue,
                Options = Options.Select(option => option.Clone()).ToList(),
                Presentation = Presentation,
                Span = Span,
                EnabledWhen = EnabledWhen?.Clone(),
                VisibleWhen = VisibleWhen?.Clone()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What this field is called in the values, and in every condition that looks at it. Unique
        /// within a schema.
        /// </summary>
        [ToString]
        [MemoryPackOrder(0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>The localisation key for the label.</summary>
        [MemoryPackOrder(1)]
        public string HeaderKey { get; set; } = string.Empty;

        /// <summary>The localisation key for the line underneath, where there is one.</summary>
        [MemoryPackOrder(2)]
        public string? HintKey { get; set; }

        [ToString]
        [MemoryPackOrder(3)]
        public FormFieldKind Kind { get; set; }

        /// <summary>
        /// Shown and not changed. Different from a disabled field: a read-only field is never
        /// editable, a disabled one is not editable right now.
        /// </summary>
        [MemoryPackOrder(4)]
        public bool IsReadOnly { get; set; }

        [MemoryPackOrder(5)]
        public double? Minimum { get; set; }

        [MemoryPackOrder(6)]
        public double? Maximum { get; set; }

        [MemoryPackOrder(7)]
        public double? Step { get; set; }

        /// <summary>The localisation key for the unit shown beside the value — minutes, hertz.</summary>
        [MemoryPackOrder(8)]
        public string? UnitKey { get; set; }

        /// <summary>
        /// What the field holds until somebody changes it, as an invariant string.
        /// </summary>
        /// <remarks>
        /// In the schema rather than in the values, so that a form arrives complete: whoever draws
        /// it can fill an empty bag of values from the description alone, and the difference
        /// between "the default" and "somebody chose this" survives in
        /// <see cref="FormValueState"/>.
        /// </remarks>
        [MemoryPackOrder(9)]
        public string? DefaultValue { get; set; }

        /// <summary>What can be chosen, for <see cref="FormFieldKind.Choice"/> and its cousins.</summary>
        [MemoryPackOrder(10)]
        public List<FormOption> Options { get; set; } = [];

        [MemoryPackOrder(11)]
        public FormPresentation Presentation { get; set; }

        /// <summary>When this field can be changed. Live everywhere it is not said otherwise.</summary>
        [MemoryPackOrder(12)]
        public FormCondition? EnabledWhen { get; set; }

        /// <summary>When this field is on the screen at all.</summary>
        [MemoryPackOrder(13)]
        public FormCondition? VisibleWhen { get; set; }

        /// <summary>
        /// How many of the group's columns this field takes. One unless the form says otherwise;
        /// more where an answer needs the room — an address beside two short boxes, a note under
        /// them.
        /// </summary>
        /// <remarks>
        /// Trimmed to what the group actually has: a field asking for three columns of a group
        /// drawn in two takes both, rather than pushing a third one into existence. A renderer that
        /// has collapsed to one column ignores this entirely, which is the same thing said at the
        /// other end.
        /// </remarks>
        [MemoryPackOrder(14)]
        public int Span { get; set; } = 1;

        /// <summary>
        /// What is being measured, for <see cref="FormFieldKind.Quantity"/>: a weight, a length.
        /// </summary>
        /// <remarks>
        /// A name and not a unit, on the rule this model keeps everywhere: the form says what the
        /// number is, and whoever draws it says what it is shown in. A workstation set to imperial
        /// shows the same weight in pounds without the form knowing there is such a thing.
        /// </remarks>
        [MemoryPackOrder(15)]
        public string? QuantityKey { get; set; }

        /// <summary>
        /// Where the suggestions for a <see cref="FormFieldKind.Tokens"/> field come from, by name.
        /// </summary>
        /// <remarks>
        /// A name rather than the list itself, because the list is not the form's to carry: it is
        /// kept by the workstation, it is edited where it is used, and it changes between the
        /// moment a form is described and the moment it is drawn. A name nobody can resolve means
        /// a field with no suggestions, which is a field that still works.
        /// </remarks>
        [MemoryPackOrder(16)]
        public string? SuggestionsKey { get; set; }

        /// <summary>
        /// Whether having no answer is an answer. False everywhere it is not said, which is a field
        /// that is expected to end up with something in it.
        /// </summary>
        /// <remarks>
        /// About meaning rather than about a control: a sample rate is one of three and a patient's
        /// sex is one of two or neither — an ordinary study says nothing about it. What that looks
        /// like belongs to whoever draws the form; on segments it is the way back out of a choice
        /// already made, which a row of segments otherwise does not have.
        /// </remarks>
        [MemoryPackOrder(17)]
        public bool IsOptional { get; set; }

        /// <summary>
        /// The same field again, somewhere else on the form. Drawn again, bound to the same key,
        /// and not a second field: what is typed into either is the one answer.
        /// </summary>
        /// <remarks>
        /// For an answer that belongs to several places at once — what is kept around a brady, a
        /// tachy or a pause episode is one pair of numbers for the three, and a technician on the
        /// pause page should not have to know that it lives on the brady page. A mirror carries no
        /// default and declares no options of its own: the original does, and a mirror with no
        /// original is reported as a key nobody declared.
        /// </remarks>
        [MemoryPackOrder(18)]
        public bool IsMirror { get; set; }

        #endregion
    }
}
