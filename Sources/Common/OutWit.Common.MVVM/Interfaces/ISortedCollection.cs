using System.Collections.Generic;
using OutWit.Common.MVVM.Collections;

namespace OutWit.Common.MVVM.Interfaces
{
    public interface ISortedCollection<TValue> : IReadOnlyCollection<TValue>
    {
        event SortedCollectionEventHandler<TValue> ItemsAdded;
        event SortedCollectionEventHandler<TValue> ItemsRemoved;
        event SortedCollectionEventHandler<TValue> CollectionClear;
        event SortedCollectionEventHandler<TValue> CollectionReset;
        
    }
}
