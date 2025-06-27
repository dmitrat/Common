using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;
using OutWit.Common.Controls.HighlightTextBox.HighlightRules;
using OutWit.Common.Controls.HighlightTextBox.Interfaces;

namespace OutWit.Common.Controls.HighlightTextBox.Highlighters
{
    public class Highlighter : IHighlighter, ICollection<HighlightRuleBase>
    {
        #region Constructors

        public Highlighter()
        {
            Rules = new List<HighlightRuleBase>();
        }

        #endregion

        #region Functions

        public void Highlight(FormattedText text, int previousBlockCode)
        {
            foreach (var rule in Rules)
                rule.Highlight(text, previousBlockCode);
        } 

        #endregion

        #region ICollection

        public void Add(HighlightRuleBase rule)
        {
            Rules.Add(rule);
        }

        public void CopyTo(HighlightRuleBase[] array, int arrayIndex)
        {
            Rules.CopyTo(array, arrayIndex);
        }

        public bool Remove(HighlightRuleBase rule)
        {
            return Rules.Remove(rule);
        }

        public void RemoveAt(int index)
        {
            Rules.RemoveAt(index);
        }

        public void Clear()
        {
            Rules.Clear();
        }

        public bool Contains(HighlightRuleBase item)
        {
            return Rules.Contains(item);
        } 

        #endregion

        #region IEnumerable

        public IEnumerator<HighlightRuleBase> GetEnumerator()
        {
            return Rules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 

        #endregion

        #region Properties

        private List<HighlightRuleBase> Rules { get; }

        public int Count => Rules.Count;

        public bool IsReadOnly { get; } = true;

        #endregion
    }
}
