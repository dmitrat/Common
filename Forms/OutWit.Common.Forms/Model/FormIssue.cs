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
    /// One thing the authority has to say about what is currently in the form.
    /// </summary>
    /// <remarks>
    /// A message key and its arguments, never a sentence: the validator knows the rule, the
    /// renderer knows the language. A validator that answers with text has decided what language
    /// the operator reads, from the wrong side of the wire.
    /// </remarks>
    [MemoryPackable]
    public partial class FormIssue : ModelBase
    {
        #region Constructors

        /// <remarks>
        /// Marked because there is more than one, and the generator will not choose:
        /// deserialisation fills the properties, so the empty one is the one it wants.
        /// </remarks>
        [MemoryPackConstructor]
        public FormIssue()
        {
        }

        public FormIssue(string? key, string textKey, FormSeverity severity = FormSeverity.Error,
            params string[] arguments)
        {
            Key = key;
            TextKey = textKey;
            Severity = severity;
            Arguments = arguments.ToList();
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not FormIssue issue)
                return false;

            return Key.Is(issue.Key) &&
                   TextKey.Is(issue.TextKey) &&
                   Severity.Is(issue.Severity) &&
                   Arguments.Is(issue.Arguments);
        }

        public override FormIssue Clone()
        {
            return new FormIssue
            {
                Key = Key,
                TextKey = TextKey,
                Severity = Severity,
                Arguments = Arguments.ToList()
            };
        }

        #endregion

        #region Properties

        /// <summary>The field this is about, or nothing when it is about the form as a whole.</summary>
        [ToString]
        [MemoryPackOrder(0)]
        public string? Key { get; set; }

        /// <summary>The localisation key of what to say.</summary>
        [ToString]
        [MemoryPackOrder(1)]
        public string TextKey { get; set; } = string.Empty;

        [ToString]
        [MemoryPackOrder(2)]
        public FormSeverity Severity { get; set; }

        /// <summary>What to put into the message: a limit, another field's value.</summary>
        [MemoryPackOrder(3)]
        public List<string> Arguments { get; set; } = [];

        #endregion
    }
}
