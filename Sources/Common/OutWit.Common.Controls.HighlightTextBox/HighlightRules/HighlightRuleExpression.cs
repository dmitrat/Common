using System;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Linq;
using OutWit.Common.Controls.HighlightTextBox.Utils;

namespace OutWit.Common.Controls.HighlightTextBox.HighlightRules
{
    public class HighlightRuleExpression : HighlightRuleBase
    {
        #region Constants

        private const string EXPRESSION_ELEMENT = "Expression";

        #endregion

        #region Constructors

        public HighlightRuleExpression() 
            : base()
        {

        }

        public HighlightRuleExpression(XElement rule) :
            base(rule)
        {
            Expression = rule.ReadString(EXPRESSION_ELEMENT);
        }

        #endregion

        #region Functions

        public override void Highlight(FormattedText text, int previousBlockCode)
        {
            var expression = new Regex(Expression);

            foreach (Match match in expression.Matches(text.Text))
                Highlight(text, match);
        }

        public override string ToString()
        {
            return $"Expression: {Expression??""}; {base.ToString()}";
        }

        #endregion

        #region Properties

        public string Expression { get; set; }

        #endregion
    }
}
