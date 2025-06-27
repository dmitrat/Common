using OutWit.Common.Abstract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OutWit.Common.Collections
{
    /// <summary>
    /// A  queue which automatically evicts elements from the head of the queue when attempting to add new elements onto the queue and it is full.
    /// This queue orders elements FIFO (first-in-first-out).
    /// </summary>
    public class EvictingQueue<TItem> : ModelBase, IEnumerable<TItem>
    {
        #region Constructors

        public EvictingQueue(int size)
        {
            Size = size;
            Count = 0;

            Items = new List<TItem>(Size);
        }

        private EvictingQueue(EvictingQueue<TItem> queue) : 
            this(queue.Size)
        {
            foreach (var item in queue)
                Enqueue(item);
        }

        #endregion

        #region Functions

        public void Enqueue(TItem item)
        {
            if(Count >= Size)
                Items.RemoveAt(0);

            Items.Add(item);

            Count = Items.Count;
        }

        public TItem Dequeue()
        {
            if (Count == 0)
                return default;

            var lastItem = Items[Items.Count - 1];

            Items.RemoveAt(Items.Count - 1);

            Count = Items.Count;

            return lastItem;
        }

        public TItem Peek()
        {
            return Count == 0 ? default : Items.Last();
        }

        public bool Contains(TItem item)
        {
            return Items.Contains(item);
        }

        public void Clear()
        {
            Items.Clear();
            Count = 0;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is EvictingQueue<TItem> buffer))
                return false;

            return buffer.Items.Is<TItem>(Items);
        }

        public override ModelBase Clone()
        {
            return new EvictingQueue<TItem>(this);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<TItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public int Size { get; }

        public int Count { get; private set; }

        private List<TItem> Items { get; }

        #endregion
    }
}
