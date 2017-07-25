using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Extensions
{
	public static partial class FuncExtensions
	{
		/// <summary>
		/// Memoizer with one parameter, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam">The return type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam, TResult> AsMemoized<TParam, TResult>(this Func<TParam, TResult> func)
		{
			Dictionary<TParam, TResult> values = new Dictionary<TParam, TResult>();
			// It's safe to use default(TParam) as this won't get called anyway if TParam is a value type.
			var nullValue = new Lazy<TResult>(() => func(default(TParam)));

			return (v) =>
			{
				TResult value;

				if (v == null)
				{
					value = nullValue.Value;
				}
				else if (!values.TryGetValue(v, out value))
				{
					value = values[v] = func(v);
				}

				return value;
			};
		}

		/// <summary>
		/// Memoizer with two parameters, used to perform a lazy-cached evaluation. (see http://en.wikipedia.org/wiki/Memoization)
		/// </summary>
		/// <typeparam name="TParam1">The first parameter type to memoize</typeparam>
		/// <typeparam name="TParam2">The second parameter type to memoize</typeparam>
		/// <param name="func">the function to evaluate</param>
		/// <returns>A memoized value</returns>
		public static Func<TParam1, TParam2, TResult> AsMemoized<TParam1, TParam2, TResult>(this Func<TParam1, TParam2, TResult> func)
		{
			Dictionary<CachedTuple<TParam1, TParam2>, TResult> values = new Dictionary<CachedTuple<TParam1, TParam2>, TResult>(CachedTuple<TParam1, TParam2>.Comparer);

			return (arg1, arg2) =>
			{
				var tuple = CachedTuple.Create(arg1, arg2);
				TResult value;

				if (!values.TryGetValue(tuple, out value))
				{
					value = values[tuple] = func(arg1, arg2);
				}

				return value;
			};
		}
	}
}