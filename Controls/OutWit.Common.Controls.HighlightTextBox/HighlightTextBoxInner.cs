using System;
using System.Windows;
using System.Windows.Media;

namespace OutWit.Common.Controls.HighlightTextBox
{
    public class HighlightTextBoxInner
    {
        #region Constructors

        public HighlightTextBoxInner(int charStart, int charEnd, int lineStart, int lineEnd, double lineHeight)
        {
            CharStartIndex = charStart;
            CharEndIndex = charEnd;
            LineStartIndex = lineStart;
            LineEndIndex = lineEnd;
            LineHeight = lineHeight;
            IsLast = false;
        } 

        #endregion

        #region Functions

        public string GetSubString(string text)
        {
            return text.Substring(CharStartIndex, CharEndIndex - CharStartIndex + 1);
        }

        public Point GetPosition()
        {
            return new Point(0, LineStartIndex * LineHeight);
        }

        public override string ToString()
        {
            return $"L: {LineStartIndex}/{LineEndIndex}, C: {CharStartIndex}/{CharEndIndex}, {FormattedText.Text}";
        }

        #endregion

        #region Properties

        public string RawText { get; set; }

        public FormattedText FormattedText { get; set; }

        public FormattedText LineNumbers { get; set; }

        public bool IsLast { get; set; }

        public int Code { get; set; }


        public int CharStartIndex { get; private set; }

        public int CharEndIndex { get; private set; }

        public int LineStartIndex { get; private set; }

        public int LineEndIndex { get; private set; }

        public double LineHeight { get; private set; }

        #endregion
    }
}
