using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Linq;
using OutWit.Common.Controls.HighlightTextBox.Utils;

namespace OutWit.Common.Controls.HighlightTextBox.HighlightRules
{
    public class HighlightRuleLine: HighlightRuleBase
    {
        #region Constants

        private const string LINE_START_ELEMENT = "LineStart";

        #endregion

        #region Constructors

        public HighlightRuleLine() 
            : base()
        {

        }

        public HighlightRuleLine(XElement rule) :
            base(rule)
        {
            LineStart = rule.ReadString(LINE_START_ELEMENT);
        }

        #endregion

        #region Functions

        public override void Highlight(FormattedText text, int previousBlockCode)
        {
            var expression = new Regex(Regex.Escape(LineStart) + ".*");

            foreach (Match match in expression.Matches(text.Text))
                Highlight(text, match);
        }

        public override string ToString()
        {
            return $"LineStart: {LineStart??""}; {base.ToString()}";
        }

        #endregion

        #region Properties

        public string LineStart { get; set; }

        #endregion
    }
}
