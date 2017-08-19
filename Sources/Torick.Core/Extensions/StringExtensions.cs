using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Torick.Extensions
{
	public static class StringExtensions
	{
		public static string TrimStart(this string source, string value, StringComparison comparision) 
			=> source.StartsWith(value, comparision)
				? source.Substring(value.Length)
				: source;

		public static string TrimEnd(this string source, string value, StringComparison comparision) 
			=> source.EndsWith(value, comparision)
				? source.Substring(0, source.Length - value.Length)
				: source;

		public static bool IsNullOrWhiteSpace(this string text) 
			=> string.IsNullOrWhiteSpace(text);

		public static bool HasValue(this string text) 
			=> !string.IsNullOrWhiteSpace(text);

		public static string MustHaveValue(this string text, string paremeterName)
			=> string.IsNullOrWhiteSpace(text) ? throw new ArgumentNullException(paremeterName) : text;

		public static string OrDefault(this string text, string other)
			=> string.IsNullOrWhiteSpace(text) ? other : text;

		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] items)
			=> Enumerable.Except(source, items);

		public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer, params T[] items)
			=> Enumerable.Except(source, items, comparer);

		public static string JoinBy<T>(this IEnumerable<T> source, string separator)
			=> string.Join(separator, source);
	}
}