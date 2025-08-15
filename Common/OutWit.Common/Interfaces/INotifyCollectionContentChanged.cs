using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OutWit.Common.Interfaces
{
    public interface INotifyCollectionContentChanged
    {
       event NotifyCollectionContentChangedEventHandler CollectionContentChanged;
    }

    public delegate void NotifyCollectionContentChangedEventHandler(object sender, PropertyChangedEventArgs e);
}
