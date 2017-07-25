using System;
using System.Linq;
using System.Threading.Tasks;

namespace Framework.LaunchArgs
{
	public sealed class ValueArgument<T> : ValueArgument
		where T : struct
	{
		private T? _defaultValue;

		public ValueArgument(params string[] options)
			: base(typeof(T), options)
		{
		}

		public new T? DefaultValue
		{
			get => _defaultValue;
			set
			{
				_defaultValue = value;
				base.DefaultValue = ((ValueType) value)?.ToString();
			}
		}

		internal override bool HasDefaultValue => DefaultValue.HasValue;
	}
}