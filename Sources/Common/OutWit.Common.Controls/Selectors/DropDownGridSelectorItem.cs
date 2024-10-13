using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using OutWit.Common.Aspects;

namespace OutWit.Common.Controls.Selectors
{
    public class DropDownGridSelectorItem : INotifyPropertyChanged
    {
        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Constructors

        public DropDownGridSelectorItem(int row, int column)
        {
            Row = row;
            Column = column;

            IsSelected = false;
            IsMouseOver = false;
        }

        #endregion

        #region Fucntrions

        public override string ToString()
        {
            return $"[{Row}, {Column}]: {IsSelected}";
        }

        #endregion

        #region Properties

        public int Row { get; }

        public int Column { get; }

        [Notify]
        public bool IsSelected { get; set; }

        [Notify]
        public bool IsMouseOver { get; set; }

        #endregion
    }
}
