using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.MVVM.Table
{
    public class TableViewPage : ModelBase, IEnumerable<TableViewRow>
    {
        #region Constructors

        public TableViewPage() : 
            this(0)
        {
            PageHeader = "";
        }

        public TableViewPage(int index)
        {
            Index = index;
            Rows = new List<TableViewRow>();

            PageHeader = $"{Index}";
        }

        #endregion

        #region Functions

        public void Add(TableViewRow row)
        {
            Rows.Add(row);
        }

        public void SetHeader(string pageHeader)
        {
            PageHeader = pageHeader;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is TableViewPage page))
                return false;

            return Rows.Is(page.Rows);
        }

        public override TableViewPage Clone()
        {
            return new TableViewPage {Rows = Rows.Select(row => row.Clone()).ToList()};
        } 

        #endregion

        #region IEnumerable

        public IEnumerator<TableViewRow> GetEnumerator()
        {
            return Rows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties
        
        public int Index { get; internal set; }

        public string PageHeader { get; internal set; }

        public List<TableViewRow> Rows { get; internal set; }

        #endregion
    }
}
