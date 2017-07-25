using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.Extensions
{
	public static class EnumerableExtensions
	{
		public static bool None<T>(this IEnumerable<T> source) => !source.Any();
	}
}