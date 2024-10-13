using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;
using OutWit.Common.Utils;
using OutWit.Common.Values;

namespace OutWit.Common.MVVM.Table
{
    public class TableViewRow : ModelBase, IEnumerable<TableViewCell>
    {
        #region Constructors

        public TableViewRow()
        {
            Values = new TableViewCell[0];
            RowType = DbTableRowType.BodyEven;
        }

        public TableViewRow(int page, int index, string[] values, DbTableRowType rowType)
        {
            Page = page;
            Index = index;

            Values = new TableViewCell[values.Length];

            Values[0] = new TableViewCell(values[0], rowType, true);

            for (int i = 1; i < values.Length; i++)
                Values[i] = new TableViewCell(values[i], rowType);

            RowType = rowType;
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            if (Values.Length == 0)
                return "";

            var str = "";

            foreach (var value in Values)
                str += $"{value}|";

            return str.TrimEnd(1);
        } 

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is TableViewRow row))
                return false;

            return Values.Is(row.Values) &&
                   RowType.Is(row.RowType) &&
                   Page.Is(row.Page) &&
                   Index.Is(row.Index);
        }

        public override TableViewRow Clone()
        {
            return new TableViewRow
            {
                Values = Values.ToArray(),
                RowType = RowType,
                Page = Page,
                Index = Index
            };
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TableViewCell> GetEnumerator()
        {
            return Values.Cast<TableViewCell>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 

        #endregion

        #region Properties

        public int Page { get; internal set; }

        public int Index { get; internal set; }

        public TableViewCell this[int index] => Values[index];

        public TableViewCell[] Values { get; internal set; }

        public DbTableRowType RowType { get; internal set; }

        #endregion
    }

    public enum DbTableRowType
    {
        Header,
        BodyEven,
        BodyOdd,
        Footer
    }
}
