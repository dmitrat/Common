using System.Linq;

namespace OutWit.Common.Hash
{
	public static class MultiHash
	{
		public static int GetMultiHash(int firstHash, params int[] hashes)
		{
			unchecked
			{
				var hashCode = firstHash;

				foreach (var geometryHash in hashes.OrderBy(x => x))
					hashCode = (hashCode * 397) ^ geometryHash;

				return hashCode;
			}
		}

		public static int GetMultiHash(params double[] values)
		{
			unchecked
			{
				var hashCode = values[0].GetHashCode();

				for (var i = 1; i < values.Length; i++)
					hashCode = (hashCode * 397) ^ values[i].GetHashCode();

				return hashCode;
			}
		}

		public static int GetMultiHash<T>(params T[] values)
		{
			unchecked
			{
				var hashCode = values[0].GetHashCode();

				for (var i = 1; i < values.Length; i++)
					hashCode = (hashCode * 397) ^ values[i].GetHashCode();

				return hashCode;
			}
		}

		public static int GetMultiHash(object first, params object[] array)
		{
			return GetMultiHash(first.GetHashCode(), array.Select(x => x.GetHashCode()).ToArray());
		}

		public static long GetLongMultiHash(long firstHash, params long[] hashes)
		{
			unchecked
			{
				var hashCode = firstHash;

				foreach (var geometryHash in hashes.OrderBy(x => x))
					hashCode = (hashCode * 397) ^ geometryHash;

				return hashCode;
			}
		}

		public static int GetMultiHash<T1, T2>()
		{
			return GetMultiHash(typeof(T1).GetHashCode(), typeof(T2).GetHashCode());
		}

		public static int GetMultiHash<T1, T2, T3>()
		{
			return GetMultiHash(typeof(T1).GetHashCode(), typeof(T2).GetHashCode(), typeof(T3).GetHashCode());
		}

		public static int GetMultiHash<T1, T2, T3, T4>()
		{
			return GetMultiHash(typeof(T1).GetHashCode(), typeof(T2).GetHashCode(), typeof(T3).GetHashCode(), typeof(T4).GetHashCode());
		}
	}
}
