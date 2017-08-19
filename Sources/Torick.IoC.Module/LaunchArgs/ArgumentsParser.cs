using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Torick.Extensions;

namespace Torick.IoC.Module.LaunchArgs
{
	public static class ArgumentsParser
	{
		public static char[] ArgumentPrefixes = new[] {'-', '/', '\\'};
		private static readonly Regex _argRegex = new Regex(@"^[-/\\](?<key>[a-zA-Z0-9-_\?\.]+):?(?<value>.*)$", RegexOptions.Compiled);

		public static IDictionary<string, string> Parse(string[] args)
		{
			return args
				.Select(Parse)
				.Where(kvp => kvp.Key.HasValue())
				.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
		}
		
		public static KeyValuePair<string, string> Parse(string arg)
		{
			var match = _argRegex.Match(arg);
			return match.Success 
				? new KeyValuePair<string, string>(
					match.Groups["key"].Value,
					match.Groups["value"].Value) 
				: default(KeyValuePair<string, string>);
		}
	}
}
