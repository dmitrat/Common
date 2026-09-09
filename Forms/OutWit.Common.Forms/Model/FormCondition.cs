using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Values;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// When a group or a field is live: a comparison against another field's value, or a tree of
    /// them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the whole of the logic a form is allowed to carry, and it is here rather than with
    /// the authority for one reason: it has to be answered on every keystroke. A round trip to
    /// decide whether a control is greyed makes a form feel broken, and a form that greys nothing
    /// makes an operator read a manual.
    /// </para>
    /// <para>
    /// Anything about what the values <i>mean together</i> is not this: it belongs to whoever
    /// declared the form, who is asked after a change rather than during one.
    /// </para>
    /// </remarks>
    [MemoryPackable]
    public partial class FormCondition : ModelBase
    {
        #region Functions

        /// <summary>A condition on one field.</summary>
        public static FormCondition On(string key, FormOperator @operator, params string[] values)
        {
            return new FormCondition
            {
                Key = key,
                Operator = @operator,
                Values = values.ToList()
            };
        }

        /// <summary>All of them, and nothing of its own.</summary>
        public static FormCondition All(params FormCondition[] conditions)
        {
            return new FormCondition
            {
                Junction = FormJunction.And,
                Conditions = conditions.ToList()
            };
        }

        /// <summary>Any of them.</summary>
        public static FormCondition Any(params FormCondition[] conditions)
        {
            return new FormCondition
            {
                Junction = FormJunction.Or,
                Conditions = conditions.ToList()
            };
        }

        /// <remarks>
        /// Written out rather than composed from <c>[ToString]</c> properties, because a condition
        /// read in a log is read as an expression — <c>Mode Equals Holter</c> — and a list of its
        /// members is not one.
        /// </remarks>
        public override string ToString()
        {
            if (Conditions.Count > 0)
                return $"{Junction}({string.Join(", ", Conditions)})";

            return $"{Key} {Operator} {string.Join("|", Values)}";
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormCondition condition)
                return false;

            return Key.Is(condition.Key) &&
                   Operator.Is(condition.Operator) &&
                   Values.Is(condition.Values) &&
                   Junction.Is(condition.Junction) &&
                   Conditions.Is(condition.Conditions);
        }

        public override FormCondition Clone()
        {
            return new FormCondition
            {
                Key = Key,
                Operator = Operator,
                Values = Values.ToList(),
                Junction = Junction,
                Conditions = Conditions.Select(condition => condition.Clone()).ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>The field this looks at, or nothing when it only joins its children.</summary>
        [MemoryPackOrder(0)]
        public string? Key { get; set; }

        [MemoryPackOrder(1)]
        public FormOperator Operator { get; set; }

        /// <summary>What the value is compared with. Invariant strings, as values are.</summary>
        [MemoryPackOrder(2)]
        public List<string> Values { get; set; } = [];

        /// <summary>How <see cref="Conditions"/> are joined.</summary>
        [MemoryPackOrder(3)]
        public FormJunction Junction { get; set; }

        /// <summary>
        /// Conditions underneath this one. A condition with children and no key of its own is a
        /// bracket; one with both is its own comparison joined with theirs.
        /// </summary>
        [MemoryPackOrder(4)]
        public List<FormCondition> Conditions { get; set; } = [];

        #endregion
    }
}
