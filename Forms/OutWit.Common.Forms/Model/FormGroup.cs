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
    /// A heading with things under it: fields, or more groups when the form has tabs or nesting.
    /// </summary>
    [MemoryPackable]
    public partial class FormGroup : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormGroup()
        {
        }

        public FormGroup(string key, string headerKey, FormGroupKind kind = FormGroupKind.Section)
        {
            Key = key;
            HeaderKey = headerKey;
            Kind = kind;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormGroup group)
                return false;

            return Key.Is(group.Key) &&
                   HeaderKey.Is(group.HeaderKey) &&
                   Kind.Is(group.Kind) &&
                   Columns.Is(group.Columns) &&
                   Span.Is(group.Span) &&
                   ColumnWeights.Is(group.ColumnWeights) &&
                   Groups.Is(group.Groups) &&
                   Fields.Is(group.Fields) &&
                   SwitchKey.Is(group.SwitchKey) &&
                   EnabledWhen.Check(group.EnabledWhen) &&
                   VisibleWhen.Check(group.VisibleWhen) &&
                   SummaryKey.Is(group.SummaryKey) &&
                   ActiveWhen.Check(group.ActiveWhen);
        }

        public override FormGroup Clone()
        {
            return new FormGroup
            {
                Key = Key,
                HeaderKey = HeaderKey,
                Kind = Kind,
                Columns = Columns,
                Span = Span,
                ColumnWeights = [..ColumnWeights],
                Groups = Groups.Select(one => one.Clone()).ToList(),
                Fields = Fields.Select(field => field.Clone()).ToList(),
                SwitchKey = SwitchKey,
                EnabledWhen = EnabledWhen?.Clone(),
                VisibleWhen = VisibleWhen?.Clone(),
                SummaryKey = SummaryKey,
                ActiveWhen = ActiveWhen?.Clone()
            };
        }

        #endregion

        #region Properties

        [ToString]
        [MemoryPackOrder(0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>The localisation key for the heading, or the tab's own name.</summary>
        [MemoryPackOrder(1)]
        public string HeaderKey { get; set; } = string.Empty;

        [ToString]
        [MemoryPackOrder(2)]
        public FormGroupKind Kind { get; set; }

        /// <summary>Groups underneath: the tabs of a tab strip, or sections within a section.</summary>
        [MemoryPackOrder(3)]
        public List<FormGroup> Groups { get; set; } = [];

        [MemoryPackOrder(4)]
        public List<FormField> Fields { get; set; } = [];

        /// <summary>
        /// A <see cref="FormFieldKind.Boolean"/> field of this group drawn as the group's own on
        /// and off, rather than as one more line inside it.
        /// </summary>
        /// <remarks>
        /// Because that is how such a group reads to the person using it — one thing that is on or
        /// off, with its details underneath — and drawing it as a heading plus a stray tick box
        /// leaves them wondering which of the two is in charge.
        /// </remarks>
        [MemoryPackOrder(5)]
        public string? SwitchKey { get; set; }

        [MemoryPackOrder(6)]
        public FormCondition? EnabledWhen { get; set; }

        [MemoryPackOrder(7)]
        public FormCondition? VisibleWhen { get; set; }

        /// <summary>
        /// How many columns this group's fields flow into. One — a field under a field — unless
        /// the form says otherwise.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A hint about shape and not a measurement. A form that asks for three columns is saying
        /// that its fields are short and that three of them side by side read as a row rather than
        /// as a queue; it is not saying how wide anything is, and it cannot, because it does not
        /// know what is drawing it. A renderer with no room — a phone, a narrow pane, a printed
        /// page — is expected to use fewer, and one column is always a correct answer.
        /// </para>
        /// <para>
        /// Zero or less means the same as one. There is no way to ask for "as many as fit": that is
        /// the renderer's business, and a form that tried to specify it would be specifying a
        /// window size it has never seen.
        /// </para>
        /// </remarks>
        [MemoryPackOrder(8)]
        public int Columns { get; set; } = 1;

        /// <summary>
        /// How many of its parent's columns this group takes. One unless the form says otherwise;
        /// more where a section is wider than its neighbours — a table of channels beside two short
        /// blocks, or a section that wants the whole width above them.
        /// </summary>
        [MemoryPackOrder(9)]
        public int Span { get; set; } = 1;

        /// <summary>
        /// How the room is shared between the columns: one number per column, and the columns come
        /// out in that proportion. Empty — the usual case — means share it equally.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Still shape and not measurement: <c>[2, 3]</c> says the second answer needs half again
        /// the room of the first, not that either is 148 pixels wide. A renderer that has collapsed
        /// the group to one column ignores this, as it ignores the column count itself.
        /// </para>
        /// <para>
        /// A list that does not have one number per column is ignored whole rather than padded:
        /// half a proportion is not a proportion, and guessing the rest would put widths on the
        /// screen that nobody chose.
        /// </para>
        /// </remarks>
        [MemoryPackOrder(10)]
        public List<double> ColumnWeights { get; set; } = [];

        /// <summary>
        /// For an entry of a list: the line under its name, as a key whose text names fields in
        /// braces — <c>below {BradyLimit} bpm for {BradyDuration} beats</c>. Whoever draws the form
        /// says the key and puts each field's current answer where its name stands, as that answer
        /// is shown: an option by its own caption, a number as it is. Nothing where the entry's
        /// name is all there is to say.
        /// </summary>
        [MemoryPackOrder(11)]
        public string? SummaryKey { get; set; }

        /// <summary>
        /// For an entry of a list: when it is marked as on. Where this is not given the entry's
        /// switch decides, and an entry with neither is always on. A symptom event is on when the
        /// button raises one, which is a choice and not a switch.
        /// </summary>
        [MemoryPackOrder(12)]
        public FormCondition? ActiveWhen { get; set; }

        #endregion
    }
}
