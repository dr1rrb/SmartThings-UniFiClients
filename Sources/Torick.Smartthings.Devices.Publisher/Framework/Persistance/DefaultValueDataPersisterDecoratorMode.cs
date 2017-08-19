using System;

namespace Torick.Persistence
{
	/// <summary>
	/// [Flags] Describe the behavior of the <see cref="DefaultValueDataPersisterDecorator{T}"/>.
	/// </summary>
	[Flags]
	public enum DefaultValueDataPersisterDecoratorMode
	{
		/// <summary>
		/// Apply no default value transformation.
		/// </summary>
		None = 0,

		/// <summary>
		/// When an empty value is read, it is converted to `CustomDefaultValue`.
		/// </summary>
		ReadEmptyToCustomDefault = 0x01,

		/// <summary>
		/// When a `default(T)` is read (usually `null`), it is converted to `CustomDefaultValue`
		/// </summary>
		ReadDefaultToCustomDefault = 0x02,

		/// <summary>
		/// When the read result on an error, it is converted to `CustomDefaultValue`
		/// </summary>
		ReadErrorToCustomDefault = 0x04,

		/// <summary>
		/// Write operation with `CustomDefaultValue` as value is stored as empty
		/// </summary>
		WriteCustomDefaultToEmpty = 0x08,

		/// <summary>
		/// Write operation with `default(T)` as value is stored as empty
		/// </summary>
		WriteDefaultToEmpty = 0x10,

		/// <summary>
		/// All flags set
		/// </summary>
		All = 0x1F
	}
}