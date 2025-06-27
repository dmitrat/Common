using System.Collections.Generic;

namespace OutWit.Common.Interfaces
{
    public interface IArray<T> : IReadOnlyList<T>
    {
        int IndexOf(T item);
    }
}
