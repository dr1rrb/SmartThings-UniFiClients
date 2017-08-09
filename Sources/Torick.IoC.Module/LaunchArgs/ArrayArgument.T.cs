using System;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.IoC.Module.LaunchArgs
{
	public sealed class ArrayArgument<T> : ArrayArgument
		where T : struct
	{
		private T[] _defaultValue;

		public ArrayArgument(params string[] options)
			: base(typeof(T[]), options)
		{
		}

		public new T[] DefaultValue
		{
			get => _defaultValue;
			set
			{
				_defaultValue = value;
				base.DefaultValue = value?.Select(v => v.ToString()).ToArray<string>() ?? new string[0];
			}
		}

		internal override bool HasDefaultValue => _defaultValue?.Any() ?? false;
	}
}