using System.Collections;
using System.Collections.Generic;
using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace OutWit.Common.Collections
{
	public class RingBuffer<TItem> : ModelBase, IEnumerable<TItem>
	{
		#region Constructors

		public RingBuffer()
		{
			Items = new List<TItem>();
			Index = 0;
		}

		public RingBuffer(IEnumerable<TItem> items)
		{
			Items = new List<TItem>(items);
			Index = 0;
		} 

		#endregion

		#region Functions

		public void Add(TItem item)
		{
			Items.Add(item);
		}

		public void Add(IEnumerable<TItem> item)
		{
			Items.AddRange(item);
		}

		public TItem Next()
		{
			if (++Index >= Items.Count)
				Index = 0;

			return Items[Index];
		}

		public TItem Current()
		{
			return Items[Index];
		}

		#endregion

        #region Model Base

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (!(modelBase is RingBuffer<TItem> buffer))
                return false;

            return buffer.Index.Is(Index) && buffer.Items.Is(Items);
        }

        public override ModelBase Clone()
        {
            return new RingBuffer<TItem>(Items);
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

		private int Index { get; set; }
		private List<TItem> Items { get; }

		#endregion


	}
}
