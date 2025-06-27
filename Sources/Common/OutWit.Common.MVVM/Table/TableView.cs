using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Values;
using OutWit.Common.Collections;

namespace OutWit.Common.MVVM.Table
{
    public class TableView : ModelBase, IEnumerable<TableViewPage>
    {
        #region Constructors

        public TableView() : 
            this("", "", null)
        {
            
        }

        public TableView(string title, string subTitle, TableViewRow headerRow)
        {
            Pages = new List<TableViewPage>();
            Title = title;
            SubTitle = subTitle;

            HeaderRow = headerRow;
            ColumnsCount = HeaderRow?.Values.Length ?? 0;
        }

        #endregion

        #region Functrions

        public void Add(TableViewPage page)
        {
            Pages.Add(page);
        }
        
        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is TableView table))
                return false;

            return Title.Is(table.Title) &&
                   HeaderRow.Is(table.HeaderRow) &&
                   ColumnsCount.Is(table.ColumnsCount) &&
                   Pages.Is(table.Pages);
        }

        public override TableView Clone()
        {
            return new TableView
            {
                Title = Title,
                HeaderRow = HeaderRow.Clone(),
                ColumnsCount = ColumnsCount,

                Pages = Pages.Select(page => page.Clone()).ToList()
            };
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TableViewPage> GetEnumerator()
        {
            return Pages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties
        
        public string Title { get; internal set; }

        public string SubTitle { get; internal set; }

        public TableViewRow HeaderRow { get; internal set; }

        public int ColumnsCount { get; internal set; }

        public List<TableViewPage> Pages { get; internal set; }

        #endregion
    }
}
