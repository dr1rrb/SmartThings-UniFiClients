using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Torick.IoC.Module.Extensions;

namespace Torick.IoC.Module.LaunchArgs
{
	public abstract class Argument
	{
		protected Argument(string[] options)
		{
			if (options.None())
			{
				throw new ArgumentOutOfRangeException(nameof(options), "No options configured");
			}

			Requires = new List<Func<Argument>>();
			Options = options.Select(opt => opt.TrimStart(ArgumentsParser.ArgumentPrefixes)).ToArray();
		}

		internal string[] Options { get; }

		public string Name { get; set; }

		public string Description { get; set; }

		public List<Func<Argument>> Requires { get; set; }
	}
}