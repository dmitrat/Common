using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.Forms.Model
{
    /// <summary>
    /// What the authority says about a filled-in form.
    /// </summary>
    [MemoryPackable]
    public partial class FormValidation : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormValidation()
        {
        }

        public FormValidation(IEnumerable<FormIssue> issues)
        {
            Issues = issues.ToList();
        }

        #endregion

        #region Functions

        /// <summary>Nothing to say, which is the usual answer.</summary>
        public static FormValidation Good() => new();

        /// <summary>What the authority has to say, issue by issue.</summary>
        public static FormValidation Of(params FormIssue[] issues) => new(issues);

        /// <summary>What was said about one field.</summary>
        public IReadOnlyList<FormIssue> For(string key)
        {
            return Issues.Where(issue => issue.Key == key).ToList();
        }

        public override string ToString() => IsGood ? "good" : $"{Issues.Count} issues";

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            return modelBase is FormValidation validation && Issues.Is(validation.Issues);
        }

        public override FormValidation Clone()
        {
            return new FormValidation(Issues.Select(issue => issue.Clone()));
        }

        #endregion

        #region Properties

        [MemoryPackOrder(0)]
        public List<FormIssue> Issues { get; set; } = [];

        /// <summary>
        /// Whether this can be acted on.
        /// </summary>
        /// <remarks>
        /// Warnings do not stop anything: a warning is the authority saying "that is allowed and
        /// probably not what you meant", which is exactly the kind of thing an operator standing in
        /// front of the patient is entitled to overrule.
        /// </remarks>
        [MemoryPackIgnore]
        public bool IsGood => Issues.All(issue => issue.Severity != FormSeverity.Error);

        #endregion
    }
}
