using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OutWit.Common.MVVM.Table
{
    public class TableViewCell
    {
        #region Constructors

        public TableViewCell()
        {

        }

        public TableViewCell(string text, DbTableRowType rowType, bool isFirst = false)
        {
            Text = text;
            RowType = rowType;
            IsFirst = isFirst;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return $"Text: {Text}, Type: {RowType}";
        }

        #endregion

        #region Properties

        public string Text { get; internal set; }

        public bool IsFirst { get; internal set; }

        public DbTableRowType RowType { get; internal set; } 

        #endregion
    }
}
