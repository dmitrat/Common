using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Collections
{
    public class EvictingBuffer<TItem> : ModelBase, IEnumerable<TItem>
    {
        #region Fields

        private readonly object m_remainsLock = new object();
        private int m_remains = 0;

        #endregion

        #region Constructors

        private EvictingBuffer(int size, TItem[] items)
        {
            Size = size;
            Items = items.ToArray();

            Remains = Size;
            LastChunkStart = 0;
            LastChunkEnd = 0;
            ReadStart = 0;
        }

        public EvictingBuffer(int size) : 
            this(size, new TItem[size])
        {

        }

        #endregion

        #region Functions


        public void Free(int size)
        {
            if (size < Size && LastChunkEnd > size)
                Array.Copy(Items, size, Items, 0, LastChunkEnd - size);

            Remains = Math.Min(Remains + size, Size);
            ReadStart = Math.Max(ReadStart - size, 0);
            LastChunkStart = Math.Max(LastChunkStart - size, 0);
            LastChunkEnd = Math.Max(LastChunkEnd - size, 0);
        }

        public TItem[] Append(TItem[] buffer)
        {
            if (buffer.Length == 0)
                return Array.Empty<TItem>();

            if (Remains == 0)
                return buffer;

            var size = Math.Min(Remains, buffer.Length);
            Array.Copy(buffer, 0, Items, LastChunkEnd, size);

            LastChunkStart = LastChunkEnd;
            LastChunkEnd += size;

            Remains -= size;

            return buffer.Length <= size 
                ? Array.Empty<TItem>() 
                : buffer.Skip(size).ToArray();
        }

        public TItem[] LastChunk()
        {
            var buffer = new TItem[LastChunkEnd - LastChunkStart];

            Array.Copy(Items, LastChunkStart, buffer, 0, buffer.Length);

            return buffer;
        }

        public int Read(TItem[] buffer)
        {
            var size = Math.Min(LastChunkEnd - ReadStart, buffer.Length);
            Array.Copy(Items, ReadStart, buffer, 0, size);

            ReadStart = LastChunkEnd;

            return size;
        }

        #endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is EvictingBuffer<TItem> buffer))
                return false;

            return Size.Is(buffer.Size) &&
                   Items.Is(buffer.Items) &&
                   LastChunkStart.Is(buffer.LastChunkStart) &&
                   LastChunkStart.Is(buffer.LastChunkStart) &&
                   ReadStart.Is(buffer.ReadStart) &&
                   Remains.Is(buffer.Remains);
        }

        public override ModelBase Clone()
        {
            return new EvictingBuffer<TItem>(Size, Items)
            {
                LastChunkStart = LastChunkStart,
                LastChunkEnd = LastChunkEnd,
                ReadStart = ReadStart, 
                Remains = Remains
            };
        }

        #endregion


        #region IEnumerable

        public IEnumerator<TItem> GetEnumerator()
        {
            return Items.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Properties

        public int Size { get; }

        private TItem[] Items { get; }

        public int LastChunkStart { get; private set; }

        public int LastChunkEnd { get; private set; }

        public int ReadStart { get; private set; }

        public bool HasDataToRead => LastChunkEnd - ReadStart > 0;

        public int Remains
        {
            get
            {
                lock (m_remainsLock)
                {
                    return m_remains;
                }
            }
            private set
            {
                lock (m_remainsLock)
                {
                    m_remains = value;
                }
            }
        }

        #endregion
    }
}
