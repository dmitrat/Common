using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OutWit.Common.Abstract;
using OutWit.Common.Collections;

namespace OutWit.Common.MVVM.Table
{
    public class TableViewSet : ModelBase, IEnumerable<TableView>
    {
        #region Constructors

        public TableViewSet()
        {
            Tables = new List<TableView>();
        }

        #endregion

        #region Functions

        public void Add(TableView table)
        {
            Tables.Add(table);
        }

        public void Clear()
        {
            Tables.Clear();
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is TableViewSet tableSet))
                return false;

            return Tables.Is(tableSet.Tables);

        }

        public override TableViewSet Clone()
        {
            return new TableViewSet { Tables = Tables.Select(table => table.Clone()).ToList() };
        } 

        #endregion

        #region IEnumerable

        public IEnumerator<TableView> GetEnumerator()
        {
            return Tables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        } 

        #endregion

        #region Properties

        public List<TableView> Tables { get; internal set; }

        #endregion
    }
}
