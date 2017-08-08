using System;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.LaunchArgs
{
	public class FlagArgument : Argument
	{
		public FlagArgument(params string[] options)
			: base(options)
		{
		}
	}
}