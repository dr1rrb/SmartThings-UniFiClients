using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Extensions
{
	public static partial class FuncExtensions
	{
		/// <summary>
		/// A tuple implementation that caches the GetHashCode value for faster lookup performance.
		/// </summary>
		private class CachedTuple
		{
			/// <summary>
			/// Creates a tuple with two values.
			/// </summary>
			public static CachedTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
			{
				return new CachedTuple<T1, T2>(item1, item2);
			}
		}

		/// <summary>
		/// A tuple with two values implementation that caches the GetHashCode value for faster lookup performance.
		/// </summary>
		private class CachedTuple<T1, T2>
		{
			private readonly int _cachedHashCode;

			public T1 Item1 { get; }
			public T2 Item2 { get; }

			/// <summary>
			/// Gets a comparer for the current tuple
			/// </summary>
			public static readonly IEqualityComparer<CachedTuple<T1, T2>> Comparer = new EqualityComparer();

			public CachedTuple(T1 item1, T2 item2)
			{
				Item1 = item1;
				Item2 = item2;

				_cachedHashCode = item1?.GetHashCode() ?? 0
								^ item2?.GetHashCode() ?? 0;
			}

			public override int GetHashCode() => _cachedHashCode;

			public override bool Equals(object obj)
			{
				var tuple = obj as CachedTuple<T1, T2>;

				if (tuple != null)
				{
					return InternalEquals(this, tuple);
				}

				return false;
			}

			private static bool InternalEquals(CachedTuple<T1, T2> t1, CachedTuple<T1, T2> t2)
			{
				return ReferenceEquals(t1, t2) || (
							Equals(t1.Item1, t2.Item1)
							&& Equals(t1.Item2, t2.Item2)
						);
			}

			private class EqualityComparer : IEqualityComparer<CachedTuple<T1, T2>>
			{
				public bool Equals(CachedTuple<T1, T2> x, CachedTuple<T1, T2> y)
				{
					return InternalEquals(x, y);
				}

				public int GetHashCode(CachedTuple<T1, T2> obj)
				{
					return obj._cachedHashCode;
				}
			}
		}
	}
}