using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OutWit.Common.Controls.Interfaces
{
    public interface IColumnSorter : IComparer
    {
        ListSortDirection SortDirection { get; set; }
    }
}
