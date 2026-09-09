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
    /// A whole form, described as data.
    /// </summary>
    /// <remarks>
    /// Enough to draw the form and to keep it consistent while somebody types, and no more: what
    /// the values mean together belongs to whoever declared it, who is asked through
    /// <c>IFormValidator</c> rather than copied into the renderer.
    /// </remarks>
    [MemoryPackable]
    public partial class FormSchema : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormSchema()
        {
        }

        public FormSchema(string key, string titleKey)
        {
            Key = key;
            TitleKey = titleKey;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormSchema schema)
                return false;

            return Key.Is(schema.Key) &&
                   TitleKey.Is(schema.TitleKey) &&
                   Version.Is(schema.Version) &&
                   Columns.Is(schema.Columns) &&
                   ColumnWeights.Is(schema.ColumnWeights) &&
                   Flow.Is(schema.Flow) &&
                   Groups.Is(schema.Groups);
        }

        public override FormSchema Clone()
        {
            return new FormSchema
            {
                Key = Key,
                TitleKey = TitleKey,
                Version = Version,
                Columns = Columns,
                ColumnWeights = [..ColumnWeights],
                Flow = Flow,
                Groups = Groups.Select(group => group.Clone()).ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>
        /// What this form is, in the words of whoever declared it: <c>Patch.EventRecorder</c>.
        /// </summary>
        [ToString]
        [MemoryPackOrder(0)]
        public string Key { get; set; } = string.Empty;

        /// <summary>The localisation key for the form's own title.</summary>
        [MemoryPackOrder(1)]
        public string TitleKey { get; set; } = string.Empty;

        /// <summary>
        /// Which shape of this form it is, where the declaring side keeps count.
        /// </summary>
        /// <remarks>
        /// Not the version of anything else. It exists so that values kept from an earlier session
        /// can be recognised as belonging to a form that has since changed, instead of being
        /// silently applied to fields that mean something different now.
        /// </remarks>
        [ToString]
        [MemoryPackOrder(2)]
        public string? Version { get; set; }

        [MemoryPackOrder(3)]
        public List<FormGroup> Groups { get; set; } = [];

        /// <summary>
        /// How many columns the form's own groups flow into. One — a section under a section —
        /// unless the form says otherwise.
        /// </summary>
        /// <remarks>
        /// The same idea as <see cref="FormGroup.Columns"/> one level up, and under the same rule:
        /// shape, not measurement. A form of four short sections is wider than it is tall when it
        /// is drawn in two columns, and taller than the screen when it is drawn in one — which of
        /// those it should be is something the form knows and the renderer does not. A renderer
        /// with no room still uses fewer, and one column is always a correct answer.
        /// </remarks>
        [MemoryPackOrder(4)]
        public int Columns { get; set; } = 1;

        /// <summary>
        /// Which way the groups fill those columns: across and then down, or down and then across.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Across is the default and is the reading order of a page. Down is what a form of several
        /// short sections wants: filled across, a section of one field sits beside a section of
        /// three and the row is half empty; filled down, the two columns come out near the same
        /// length and each is a list of related things.
        /// </para>
        /// <para>
        /// Filled down, a group takes one column whatever its <see cref="FormGroup.Span"/> says —
        /// a section wider than the column it is being stacked in has nothing to be stacked in.
        /// </para>
        /// </remarks>
        [MemoryPackOrder(5)]
        public FormFlow Flow { get; set; }

        /// <summary>
        /// How the room is shared between those columns: one number per column, and the columns
        /// come out in that proportion. Empty — the usual case — means share it equally.
        /// </summary>
        /// <remarks>
        /// The same as <see cref="FormGroup.ColumnWeights"/> one level up, and under the same rule:
        /// shape, not measurement. A form of two long sections and two short ones says which side
        /// is the wider one; how much wider in pixels is the renderer's business.
        /// </remarks>
        [MemoryPackOrder(6)]
        public List<double> ColumnWeights { get; set; } = [];

        #endregion
    }
}
