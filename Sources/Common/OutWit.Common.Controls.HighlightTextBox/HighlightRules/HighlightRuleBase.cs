using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using OutWit.Common.Controls.HighlightTextBox.Interfaces;
using OutWit.Common.Controls.HighlightTextBox.Utils;
using OutWit.Common.Utils;

namespace OutWit.Common.Controls.HighlightTextBox.HighlightRules
{
    public abstract class HighlightRuleBase : IHighlighter
    {
        #region Constants

        private const string IGNORE_CASE_ELEMENT = "IgnoreCase";
        private const string FOREGROUND_ELEMENT = "Foreground";
        private const string FONT_WEIGHT_ELEMENT = "FontWeight";
        private const string FONT_STYLE_ELEMENT = "FontStyle";

        #endregion

        #region Constructors

        protected HighlightRuleBase()
        {

        }

        protected HighlightRuleBase(XElement rule) 
            : this()
        {
            IgnoreCase = rule.ReadBoolean(IGNORE_CASE_ELEMENT);
            Foreground = rule.ReadBrush(FOREGROUND_ELEMENT);
            FontWeight = rule.ReadFontWeight(FONT_WEIGHT_ELEMENT);
            FontStyle = rule.ReadFontStyle(FONT_STYLE_ELEMENT);
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            var str = $"IgnoreCase: {IgnoreCase}; ";

            if (Foreground != null)
                str += $"Foreground: {Foreground}; ";

            if (FontWeight != null)
                str += $"FontWeight: {FontWeight}; ";

            if (FontStyle != null)
                str += $"FontStyle: {FontStyle}; ";

            return str.TrimEnd(2);
        }

        protected void Highlight(FormattedText text, Match match)
        {
            if (Foreground != null)
                text.SetForegroundBrush(Foreground, match.Index, match.Length);

            if (FontWeight != null)
                text.SetFontWeight(FontWeight.Value, match.Index, match.Length);

            if (FontStyle != null)
                text.SetFontStyle(FontStyle.Value, match.Index, match.Length);
        }

        public abstract void Highlight(FormattedText text, int previousBlockCode);

        #endregion

        #region Proprties

        public bool IgnoreCase { get; set; }

        public Brush Foreground { get; set; }

        public FontWeight? FontWeight { get; set; }

        public FontStyle? FontStyle { get; set; } 

        #endregion
    }
}
