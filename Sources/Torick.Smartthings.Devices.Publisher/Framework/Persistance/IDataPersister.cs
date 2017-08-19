using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Torick.Persistence
{
	/// <summary>
	/// Abstraction over the persistence of an entity ensuring transactional operations when possible.
	/// </summary>
	public interface IDataPersister<T> : IDataReader<T>
	{
		
		/// <summary>
		/// Atomic load + update with optional control
		/// </summary>
		/// <remarks>
		/// CONCURRENCY WARNING: Most implementations will have a lock ensuring no concurrent operation on this instance could
		/// be done at the same time, but since you can have more than one instance and/or many processes, there's no absolute
		/// protection against concurrent update.  That's why there's a retry mecanism: if a collision is detected, your updater
		/// will be called again with a more recent value.
		/// IMPORTANT: YOU MUST CALL context.Commit() or context.RemoveAndCommit() FOR THE SAVE/REMOVAL TO OCCUR.
		/// </remarks>
		/// <returns>
		/// The updated value reading context (if updated) or the currently saved value (if not updated)
		/// </returns>
		Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterUpdaterWithContext<T> updater);

		/// <summary>
		/// Atomic load + update + save with optional control
		/// </summary>
		/// <remarks>
		/// CONCURRENCY WARNING: Most implementations will have a lock ensuring no concurrent operation on this instance could
		/// be done at the same time, but since you can have more than one instance and/or many processes, there's no absolute
		/// protection against concurrent update.  That's why there's a retry mecanism: if a collision is detected, your updater
		/// will be called again with a more recent value.
		/// IMPORTANT: YOU MUST CALL context.Commit() or context.RemoveAndCommit() FOR THE SAVE/REMOVAL TO OCCUR.
		/// </remarks>
		/// <returns>
		/// The updated value reading context (if updated) or the currently saved value (if not updated)
		/// </returns>
		Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterAsyncUpdaterWithContext<T> asyncUpdater);
	}

	/// <summary>
	/// Callback delegate for the Update method (sync version).
	/// </summary>
	public delegate void DataPersisterUpdaterWithContext<T>(DataPersisterTransactionContext<T> transactionContext);

	/// <summary>
	/// Callback delegate for the Update method (async version).
	/// </summary>
	public delegate Task DataPersisterAsyncUpdaterWithContext<T>(CancellationToken ct, DataPersisterTransactionContext<T> transactionContext);

	/// <summary>
	/// Transactional Context for the Update callback.
	/// </summary>
	public class DataPersisterTransactionContext<T> : DataReaderLoadResult<T>
	{
		/// <summary>
		/// Create an instance from a value
		/// </summary>
		public DataPersisterTransactionContext(T value, bool isValuePresent = true) : base(value, isValuePresent)
		{
		}

		/// <summary>
		/// Create an instance from an exception info
		/// </summary>
		public DataPersisterTransactionContext(ExceptionDispatchInfo exceptionInfo) : base(exceptionInfo)
		{
		}

		/// <summary>
		/// Create an instance from a load result
		/// </summary>
		public DataPersisterTransactionContext(DataReaderLoadResult<T> fromResult) : base(fromResult)
		{
		}

		/// <summary>
		/// If the entity needs to be saved back
		/// </summary>
		public bool IsCommitted { get; private set; } = false;

		/// <summary>
		/// If the entity is removed.
		/// </summary>
		public bool IsRemoved { get; private  set; } = false;

		/// <summary>
		/// The committed value.
		/// </summary>
		/// <remarks>
		/// You should take care to avoid confusion with base.Value who is the initial value.
		/// </remarks>
		public T CommittedValue { get; private set; }

		/// <summary>
		/// Enlist the transaction to "committed" and set committed value as updated result.
		/// </summary>
		public void Commit(T committedValue)
		{
			CommittedValue = committedValue;
			IsCommitted = true;
		}

		/// <summary>
		/// Tell the persister to Remove the value as updated result.
		/// </summary>
		public void RemoveAndCommit()
		{
			IsRemoved = true;
			IsCommitted = true;
		}

		/// <summary>
		/// Reset the state of the context to uncommitted.
		/// </summary>
		public void Reset()
		{
			IsCommitted = false;
			IsRemoved = false;
			CommittedValue = default(T);
		}
	}

	/// <summary>
	/// Result of a Update method call.
	/// </summary>
	public class DataPersisterUpdateResult<T> : DataReaderLoadResult<T>
	{
		/// <summary>
		/// If the data has been updated by the persister.
		/// </summary>
		/// <remarks>
		/// Usually mean the transaction has been committed.
		/// </remarks>
		public bool IsUpdated { get; }

		/// <summary>
		/// ctor
		/// </summary>
		public DataPersisterUpdateResult(T value, bool isValuePresent, bool isUpdated) : base(value, isValuePresent)
		{
			IsUpdated = isUpdated;
		}

		/// <summary>
		/// ctor using a DataReaderLoadResult<T> as result state
		/// </summary>
		public DataPersisterUpdateResult(DataReaderLoadResult<T> result, bool isUpdated) : base(result)
		{
			IsUpdated = isUpdated;
		}
	}
}
