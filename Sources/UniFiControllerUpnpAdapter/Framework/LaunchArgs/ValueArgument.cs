using System;
using System.Linq;
using Framework.Extensions;

namespace Framework.LaunchArgs
{
	public class ValueArgument : Argument
	{
		public ValueArgument(params string[] options)
			: this(typeof(string), options)
		{
		}

		protected ValueArgument(Type type, string[] options)
			: base(options)
		{
			Type = type;
		}

		public bool IsRequired { get; set; }

		public virtual string DefaultValue { get; set; }

		internal Type Type { get; }

		internal virtual bool HasDefaultValue => DefaultValue.HasValue();
	}
}