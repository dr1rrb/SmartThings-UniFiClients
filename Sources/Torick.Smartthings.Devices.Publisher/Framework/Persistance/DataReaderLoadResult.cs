using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;

namespace Torick.Persistence
{
	/// <summary>
	/// Represent the result of a _Read_ operation.
	/// </summary>
	/// <typeparam name="T">
	/// Type of the value
	/// </typeparam>
	public class DataReaderLoadResult<T>
	{
		private readonly T _value;

		/// <summary>
		/// Initialize a `DataReaderLoadResult` from a <paramref name="value"/> and custom <paramref name="isValuePresent"/>.
		/// </summary>
		public DataReaderLoadResult(T value, bool isValuePresent)
		{
			_value = value;
			IsValuePresent = isValuePresent;
			IsError = false;
		}

		/// <summary>
		/// Initialize a `DataReaderLoadResult` from a <paramref name="value"/> (considered as present)
		/// </summary>
		public DataReaderLoadResult(T value)
		{
			_value = value;
			IsValuePresent = true;
			IsError = false;
		}

		/// <summary>
		/// Initialize a `DataReaderLoadResult` from a captured <paramref name="exceptionInfo"/>.
		/// </summary>
		/// <param name="exception"></param>
		public DataReaderLoadResult(ExceptionDispatchInfo exceptionInfo)
		{
			IsError = true;
			ExceptionInfo = exceptionInfo;//.Validation().NotNull(nameof(exceptionInfo));
			_value = default(T);
			IsValuePresent = false;
		}

		/// <summary>
		/// Initialize a `DataReaderLoadResult` as empty (non-present value).
		/// </summary>
		public DataReaderLoadResult()
		{
			_value = default(T);
			IsValuePresent = false;
			IsError = false;
		}

		/// <summary>
		/// Initialize a `DataReaderLoadResult` using the state of another one.
		/// </summary>
		protected DataReaderLoadResult(DataReaderLoadResult<T> fromOtherResult)
		{
			if (fromOtherResult == null)
			{
				throw new ArgumentNullException(nameof(fromOtherResult));
			}

			if (fromOtherResult.IsError)
			{
				IsError = true;
				ExceptionInfo = fromOtherResult.ExceptionInfo;
				_value = default(T); // can't use fromOtherResult.Value because it will throw an exception
				IsValuePresent = false;
			}
			else
			{
				_value = fromOtherResult.Value;
				IsValuePresent = fromOtherResult.IsValuePresent;
				IsError = false;
			}
		}

		/// <summary>
		/// The read value
		/// </summary>
		/// <remarks>
		/// This value should now be used if <see cref="IsValuePresent"/> is false.
		/// *IMPORTANT: WILL THROW AN EXCEPTION IF `IsError` is `true`*.
		/// </remarks>
		public T Value
		{
			get
			{
				CheckThrowException();
				return _value;
			}
		}

		private void CheckThrowException()
		{
			if (IsError)
			{
				ExceptionInfo.Throw();
			}
		}

		/// <summary>
		/// If the value exists
		/// </summary>
		public bool IsValuePresent { get; }

		/// <summary>
		/// _DispatchInfo_ of the exception.  Used to rethrow the exception.
		/// </summary>
		/// <remarks>
		/// This is for persister/decorator implementation, should not be used
		/// from application code.
		/// </remarks>
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		public ExceptionDispatchInfo ExceptionInfo { get; }

		/// <summary>
		/// This is the exception catched, if any
		/// </summary>
		public Exception Exception => ExceptionInfo?.SourceException;

		/// <summary>
		/// If the result of the read operation is an error (exception).
		/// </summary>
		public bool IsError { get; }

		/// <summary>
		/// Get the value, with a fallback to a default value when non-present.
		/// *IMPORTANT: WILL THROW AN EXCEPTION IF `IsError` is `true`*.
		/// </summary>
		/// <param name="defaultValue">Custom default value</param>
		/// <returns></returns>
		public T GetValueOrDefault(T defaultValue = default(T))
		{
			CheckThrowException();
			return IsValuePresent ? Value : defaultValue;
		}
	}
}