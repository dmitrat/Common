using System;
using System.Xml.Linq;
using OutWit.Common.Controls.HighlightTextBox.HighlightRules;

namespace OutWit.Common.Controls.HighlightTextBox.Highlighters
{
    public class HighlighterXml : Highlighter
    {
        #region Constants

        private const string WORDS_RULES_ELEMENT = "HighlightRuleWords";

        private const string LINE_RULES_ELEMENT = "HighlightRuleLine";

        private const string EXPRESSION_RULES_ELEMENT = "HighlightRuleExpression";

        #endregion

        #region Constructors

        public HighlighterXml(XElement root)
        {
            foreach (var elem in root.Elements()) 
            {
                switch (elem.Name.ToString()) 
                {
                    case WORDS_RULES_ELEMENT: Add(new HighlightRuleWords(elem)); break;

                    case LINE_RULES_ELEMENT: Add(new HighlightRuleLine(elem)); break;

                    case EXPRESSION_RULES_ELEMENT: Add(new HighlightRuleExpression(elem)); break;
                }
            }
        }

        #endregion
    }
}
