using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Linq;
using OutWit.Common.Controls.HighlightTextBox.Utils;

namespace OutWit.Common.Controls.HighlightTextBox.HighlightRules
{
    public class HighlightRuleWords: HighlightRuleBase
    {
        #region Constants

        private const string WORDS_ELEMENT = "Words";

        #endregion

        #region Constructors

        public HighlightRuleWords() 
            : base()
        {

        }

        public HighlightRuleWords(XElement rule) :
            base(rule)
        {
            Words = rule.ReadStringList(WORDS_ELEMENT);
        }

        #endregion

        #region Functions

        public override void Highlight(FormattedText text, int previousBlockCode)
        {
            var expression = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");

            foreach (Match match in expression.Matches(text.Text))
            {
                if(!ShouldHighlight(match.Value))
                    continue;

                Highlight(text, match);
            }
        }

        public override string ToString()
        {
            return $"{Words?.Count ?? 0} words; {base.ToString()}";
        }

        private bool ShouldHighlight(string word)
        {
            var comparison = IgnoreCase
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;

            return Words.Any(w => word.Equals(w, comparison));
        }

        #endregion

        #region Properties

        public IReadOnlyCollection<string> Words { get; set; }

        #endregion
    }
}
