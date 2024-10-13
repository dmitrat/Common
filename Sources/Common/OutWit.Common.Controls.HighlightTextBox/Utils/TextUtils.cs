using System;

namespace OutWit.Common.Controls.HighlightTextBox.Utils
{
    public static class TextUtils
    {
        /// <summary>
        /// Returns the raw number of the current line count.
        /// </summary>
        public static int GetLineCount(this string me) 
        {
            int count = 1;

            for (int i = 0; i < me.Length; i++) 
            {
                if (me[i] == '\n')
                    count += 1;
            }

            return count;
        }

        /// <summary>
        /// Returns the index of the first character of the
        /// specified line. If the index is greater than the current
        /// line count, the method returns the index of the last
        /// character. The line index is zero-based.
        /// </summary>
        public static int GetFirstCharIndexFromLineIndex(this string me, int lineIndex) 
        {
            if (me == null)
                throw new ArgumentNullException();

            if (lineIndex <= 0)
                return 0;

            int currentLineIndex = 0;
            for (int i = 0; i < me.Length - 1; i++)
            {
                if (me[i] != '\n') 
                    continue;

                currentLineIndex += 1;
                if (currentLineIndex == lineIndex)
                    return Math.Min(i + 1, me.Length - 1);
            }

            return Math.Max(me.Length - 1, 0);
        }

        /// <summary>
        /// Returns the index of the last character of the
        /// specified line. If the index is greater than the current
        /// line count, the method returns the index of the last
        /// character. The line-index is zero-based.
        /// </summary>
        public static int GetLastCharIndexFromLineIndex(this string me, int lineIndex) 
        {
            if (me == null)
                throw new ArgumentNullException();

            if (lineIndex < 0)
                return 0;

            int currentLineIndex = 0;
            for (int i = 0; i < me.Length - 1; i++)
            {
                if (me[i] != '\n') 
                    continue;

                if (currentLineIndex == lineIndex)
                    return i;

                currentLineIndex += 1;
            }

            return Math.Max(me.Length - 1, 0);
        }
    }
}
