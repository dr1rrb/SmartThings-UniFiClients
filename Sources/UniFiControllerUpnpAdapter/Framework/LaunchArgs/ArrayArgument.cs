using System;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.LaunchArgs
{
	public class ArrayArgument : Argument
	{
		public ArrayArgument(params string[] options)
			: this(typeof(string), options)
		{
		}

		protected ArrayArgument(Type type, string[] options)
			: base(options)
		{
			if (!type.IsArray)
			{
				throw new ArgumentException("Type must be an array type.");
			}

			Type = type;
		}

		public bool IsRequired { get; set; }

		public string[] DefaultValue { get; set; }

		internal Type Type { get; }

		internal virtual bool HasDefaultValue => DefaultValue?.Any() ?? false;
	}
}