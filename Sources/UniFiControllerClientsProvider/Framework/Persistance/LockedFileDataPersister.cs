using System;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Framework.Concurrency;
using Framework.Serialization;

namespace Framework.Persistence
{
	/// <summary>
	/// This is an implementation of <see cref="IDataPersister{T}"/> base on System.IO namespace. [IMPORTANT: see remarks]
	/// </summary>
	/// <remarks>
	/// Designed to work on a filesystem not supporting transactions natively.
	/// IMPORTANT: do NOT use this if you can accessing this file from another method, because
	/// a custom mecanism is done to ensure transactional operations and accessing it from
	/// elsewhere could break this protection.
	/// ALSO IMPORTANT: All concurrent access to this file SHOULD be done throught the same implementation of this class
	/// or the behavior could become unpredictible and even get deadlocks.
	/// </remarks>
	public sealed class LockedFileDataPersister<T> : IDataPersister<T>
	{
		/*
		 * -- TRANSACTIONAL MECANISM --
		 * 
		 * This class is using a manual transactional file access. Because it's
		 * designed to work on any file system, any operating system, this class
		 * won't rely on any external transaction system.
		 * 
		 * FILES ON DISK:
		 *   - COMMITTED: <filename> - last committed version of the file
		 *   - NEW: <filename>.new - an ongoing transaction file
		 *   - LOCK: <filename>.lck - an active lock from a process for a transaction.  This file is opened in exclusive mode during all the writing process.
		 *   - OLD: <filename>.old - last committed file renamed to let the ongoing becoming the new committed.
		 * 		 
		 * PROCESS FOR UPDATE OPERATION:
		 * 
		 * 1) Lock the file (see LOCKING PROCESS below)
		 * 2) Read the COMMITTED file
		 * 3) Call code updater (update callback func)
		 * 4) If the context is not committed, go to step 9
		 * 5) Save the result in the NEW file, flush & close the file.
		 * 6) Rename the COMMITTED as OLD file (atomic operation in OS)  -- STARTING HERE THE CHANGE IS DURABLE
		 * 7) Rename the NEW as COMMITTED file (atomic operation in OS)
		 * 8) Delete the OLD file
		 * 9) Close & delete the LOCK file
		 * 
		 * PROCESS FOR READ OPERATION:
		 * 
		 * 1) Lock the file (see LOCKING PROCESS below)
		 * 2) Read the COMMITTED file (update only)
		 * 3) Close & delete the LOCK file
		 * 
		 * LOCKING PROCESS:
		 * 1) Try to open the LOCK file in exclusive mode (even if it exists) & keep the file opened until the end of the operation, wait & retry as needed
		 * 2) If all files (OLD, COMMITTED and NEW exists), Delete the OLD and rename the COMMITTED as OLD  --- that's an odd situation who should be reported
		 * 3) If both OLD and NEW exists, rename the NEW as COMMITTED  -- This is a FORWARD resolution (previous change was completed - rolling forward)
		 * 4) If both OLD and COMMITTED exists, delete the OLD
		 * 5) Delete any existing NEW file (uncompleted)  -- This is a BACKWARD resolution (previous change was uncompleted - rolling back)
		 * 
		 */

		private readonly string _committedFile;
		private readonly string _oldFile;
		private readonly string _newFile;
		private readonly string _lockFile;
		private readonly FuncAsync<Stream, T> _read;
		private readonly ActionAsync<T, Stream> _write;
		private readonly int _numberOfRetries;
		private readonly int _retryDelay;

		private readonly AsyncLock _lock = new AsyncLock();

		#region Constructors
		/// <summary>
		/// Constructor with callbacks for read & write operations.
		/// </summary>
		public LockedFileDataPersister(
			string fullFilename,
			FuncAsync<Stream, T> read,
			ActionAsync<T, Stream> write,
			int numberOfRetries = 3,
			int retryDelay = 100)
		{
			_committedFile = new FileInfo(fullFilename/*.Validation().NotNull(nameof(fullFilename))*/).FullName;
			_read = read;
			_write = write;
			_numberOfRetries = numberOfRetries;
			_retryDelay = retryDelay;

			// Create other working file names
			_oldFile = _committedFile + ".old";
			_newFile = _committedFile + ".new";
			_lockFile = _committedFile + ".lck";
		}

		/// <summary>
		/// Constructor using an <see cref="IObjectSerializer"/> for persistence.
		/// </summary>
		public LockedFileDataPersister(
			string fullFilename,
			IObjectSerializer serializer,
			int numberOfRetries = 3,
			int retryDelay = 100)
		{
			_committedFile = new FileInfo(fullFilename/*.Validation().NotNull(nameof(fullFilename))*/).FullName;
			_read = async (ct, stream) => (T) serializer.FromStream(stream, typeof(T));
			_write = async (ct, entity, stream) => serializer.WriteToStream(entity, typeof(T), stream, canDisposeStream: true);
			_numberOfRetries = numberOfRetries;
			_retryDelay = retryDelay;

			// Create other working file names
			_oldFile = _committedFile + ".old";
			_newFile = _committedFile + ".new";
			_lockFile = _committedFile + ".lck";
		}
		#endregion

		#region Implementation if IDataPersister<T>
				
		/// <inheritdoc />
		public async Task<DataReaderLoadResult<T>> Load(CancellationToken ct)
		{
			using (await _lock.LockAsync(ct))
			{
				// 1) Lock the file (see LOCKING PROCESS below)
				using (await GetFileLock(ct))
				{
					if (!File.Exists(_committedFile))
					{
						return new DataReaderLoadResult<T>();
					}

					// 2) Read the COMMITTED file (update only)
					try
					{
						using (FileStream stream = File.OpenRead(_committedFile))
						{
							var entity = await _read(ct, stream);
							return new DataReaderLoadResult<T>(entity);
						}
					}
					catch (FileNotFoundException)
					{
						return new DataReaderLoadResult<T>();
					}
					catch(Exception ex)
					{
						var exceptionInfo = ExceptionDispatchInfo.Capture(ex);
						return new DataPersisterTransactionContext<T>(exceptionInfo);
					}

					// 3) Close & delete the LOCK file
				}
			}
		}

		/// <inheritdoc />
		public bool IsDataConstant { get; } = false;
		
		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterUpdaterWithContext<T> updater)
		{
			using (await _lock.LockAsync(ct))
			{
				// 1) Lock the file (see LOCKING PROCESS below)
				using (await GetFileLock(ct))
				{
					// 2) Read the COMMITTED file
					T data = default(T);
					bool exists = false;
					DataPersisterTransactionContext<T> control;
					try
					{
						if (File.Exists(_committedFile))
						{
							using (var stream = File.OpenRead(_committedFile))
							{
								data = await _read(ct, stream);
								exists = true;
							}
						}
						control = new DataPersisterTransactionContext<T>(data, exists);
					}
					catch (FileNotFoundException)
					{
						control = new DataPersisterTransactionContext<T>(data, isValuePresent: false);
					}
					catch (Exception ex)
					{
						var exceptionInfo = ExceptionDispatchInfo.Capture(ex);
						control = new DataPersisterTransactionContext<T>(exceptionInfo);
					}

					// 3) Call code updater (update callback func)
					updater(control);

					if (!control.IsCommitted)
					{
						// 4) If the context is not committed, go to step 9
						return new DataPersisterUpdateResult<T>(data, exists, isUpdated: false);
					}

					// x) Alternate flow if the updater ask for a delete
					if (control.IsRemoved)
					{
						if (exists)
						{
							File.Delete(_committedFile);
							return new DataPersisterUpdateResult<T>(default(T), isValuePresent: false, isUpdated: true);
						}

						return new DataPersisterUpdateResult<T>(default(T), isValuePresent: false, isUpdated: false);
					}

					// 5) Save the result in the NEW file, flush & close the file.
					using (var stream = File.OpenWrite(_newFile))
					{
						await _write(ct, control.CommittedValue, stream);
					}

					if (File.Exists(_committedFile))
					{
						// 6) Rename the COMMITTED as OLD file (atomic operation in OS)  -- STARTING HERE THE CHANGE IS DURABLE
						File.Move(_committedFile, _oldFile);

						// 7) Rename the NEW as COMMITTED file (atomic operation in OS)
						File.Move(_newFile, _committedFile);

						// 8) Delete the OLD file
						File.Delete(_oldFile);
					}
					else
					{
						// 6-7-8) Rename the NEW as COMMITTED file (atomic operation in OS)
						File.Move(_newFile, _committedFile);
					}

					return new DataPersisterUpdateResult<T>(control.CommittedValue, isValuePresent: true, isUpdated: true);

					// 9) Close & delete the LOCK file
				}
			}
		}

		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterAsyncUpdaterWithContext<T> asyncUpdater)
		{
			using (await _lock.LockAsync(ct))
			{
				// 1) Lock the file (see LOCKING PROCESS below)
				using (await GetFileLock(ct))
				{
					// 2) Read the COMMITTED file
					T data = default(T);
					bool exists = false;
					DataPersisterTransactionContext<T> control;
					try
					{ 
						if (File.Exists(_committedFile))
						{
								using (var stream = File.OpenRead(_committedFile))
								{
									data = await _read(ct, stream);
									exists = true;
								}
						}
						control = new DataPersisterTransactionContext<T>(data, exists);
					}
					catch (FileNotFoundException)
					{
						control = new DataPersisterTransactionContext<T>(data, isValuePresent: false);
					}
					catch(Exception ex)
					{
						var exceptionInfo = ExceptionDispatchInfo.Capture(ex);
						control = new DataPersisterTransactionContext<T>(exceptionInfo);
					}

					// 3) Call code updater (update callback func)
					await asyncUpdater(ct, control);

					if (!control.IsCommitted)
					{
						// 4) If the context is not committed, go to step 9
						return new DataPersisterUpdateResult<T>(data, exists, isUpdated: false);
					}

					// x) Alternate flow if the updater ask for a delete
					if (control.IsRemoved)
					{
						if (exists)
						{
							File.Delete(_committedFile);
							return new DataPersisterUpdateResult<T>(default(T), isValuePresent: false, isUpdated: true);
						}

						return new DataPersisterUpdateResult<T>(default(T), isValuePresent: false, isUpdated: false);
					}

					// 5) Save the result in the NEW file, flush & close the file.
					using (var stream = File.OpenWrite(_newFile))
					{
						await _write(ct, control.CommittedValue, stream);
					}

					if (File.Exists(_committedFile))
					{
						// 6) Rename the COMMITTED as OLD file (atomic operation in OS)  -- STARTING HERE THE CHANGE IS DURABLE
						File.Move(_committedFile, _oldFile);

						// 7) Rename the NEW as COMMITTED file (atomic operation in OS)
						File.Move(_newFile, _committedFile);

						// 8) Delete the OLD file
						File.Delete(_oldFile);
					}
					else
					{
						// 6-7-8) Rename the NEW as COMMITTED file (atomic operation in OS)
						File.Move(_newFile, _committedFile);
					}

					return new DataPersisterUpdateResult<T>(control.CommittedValue, isValuePresent: true, isUpdated: true);

					// 9) Close & delete the LOCK file
				}
			}
		}

		#endregion

		// Implementation of the LOCKING PROCESS
		private async Task<IDisposable> GetFileLock(CancellationToken ct)
		{
			FileStream file = null;

			// 1) Try to open the LOCK file in exclusive mode (even if it exists) &keep the file opened until the end of the operation, wait & retry as needed
			var tryNo = 1;
			while (!ct.IsCancellationRequested && tryNo++ < _numberOfRetries)
			{
				try
				{
					file = File.Open(_lockFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
					break;
				}
				catch (Exception)
				{
					//this.Log().WarnIfEnabled(() => $"Unable to open lock file {_lockFile}, try no{tryNo}", ex);
					await Task.Delay(tryNo * _retryDelay, ct);
				}
			}

			if (ct.IsCancellationRequested)
			{
				return Disposable.Empty;
			}
			if (file == null)
			{
				// unable to lock file
				throw new InvalidOperationException("Failed to lock the target file.");
			}

			var oldExists = File.Exists(_oldFile);
			var committedExists = File.Exists(_committedFile);
			var newExists = File.Exists(_newFile);

			// 2) If all files (OLD, COMMITTED and NEW exists), Delete the OLD and rename the COMMITTED as OLD  ---that's an odd situation who should be reported
			if (oldExists && committedExists && newExists)
			{
				//this.Log().WarnIfEnabled(() => $"An inconsistent state of the file is found. Make sure all code accessing the {_committedFile} file is using the {nameof(LockedFileDataPersister<T>)} accessor.");
				File.Delete(_oldFile);
				File.Move(_committedFile, _oldFile);
				committedExists = false;
			}

			// 3) If both OLD and NEW exists, rename the NEW as COMMITTED  --This is a FORWARD resolution (previous change was completed - rolling forward)
			if (oldExists && newExists)
			{
				//this.Log().WarnIfEnabled(() => $"Rolling forward previous transaction on file {_committedFile}.");
				File.Move(_newFile, _committedFile);
				newExists = false;
				committedExists = true;
			}
			// 4) If both OLD and COMMITTED exists, delete the OLD
			if (oldExists && committedExists)
			{
				File.Delete(_oldFile);
			}

			// 5) Delete any existing NEW file (uncompleted)-- This is a BACKWARD resolution (previous change was uncompleted - rolling back)
			if (newExists)
			{
				//this.Log().WarnIfEnabled(() => $"Rolling back previous transaction on file {_committedFile}.");
				File.Delete(_newFile);
			}

			// Return a disposable who will close & delete the lock file
			return Disposable
				.Create(() =>
				{
					file.Dispose();
					File.Delete(_lockFile);
				});
		}
	}
}