using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Concurrency;
using Framework.Extensions;
using Framework.Persistence;

namespace Framework.Persistence
{
	/// <summary>
	/// A decorator for <see cref="IDataPersister{TEntity}"/> which adds ability to observe the "current" value through an <seealso cref="IObservable{T}"/> sequence.
	/// </summary>
	/// <remarks>
	/// IMPORTANT: The value `default(T)` will be observed when the state is empty.
	/// </remarks>
	public class ObservableDataPersisterDecorator<T> : IObservableDataPersister<T>
	{
		private readonly IDataPersister<T> _inner;
		private readonly IScheduler _replayScheduler;

		private readonly Framework.Concurrency.AsyncLock _updateGate = new Framework.Concurrency.AsyncLock();
		private readonly Subject<DataReaderLoadResult<T>> _update = new Subject<DataReaderLoadResult<T>>();
		private readonly IObservable<DataReaderLoadResult<T>> _getAndObserve;

		/// <summary>
		/// Creates a ObservableDataPersisterDecorator wrapping an IDataPersister which can be 
		/// an other decorator like DefaultValueDataPersisterDecorator that manages a default value
		/// </summary>
		/// <param name="inner">Decoratee</param>
		/// <param name="replayScheduler">Scheduler to use to replay the current value when using <see cref="ObservableDataPersisterDecorator{T}.GetAndObserve"/>.</param>
		public ObservableDataPersisterDecorator(IDataPersister<T> inner, IScheduler replayScheduler)
		{
			_inner = inner;//.Validation().NotNull(nameof(inner));
			_replayScheduler = replayScheduler;//.Validation().NotNull(nameof(replayScheduler));

			_getAndObserve = BuildGetAndObserve();
		}

		/// <inheritdoc />
		public async Task<DataReaderLoadResult<T>> Load(CancellationToken ct)
		{
			return await _inner.Load(ct);
		}

		/// <inheritdoc />
		public bool IsDataConstant { get; } = false;

		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterUpdaterWithContext<T> updater)
		{
			using (await _updateGate.LockAsync(ct))
			{
				var result = await _inner.Update(ct, updater);

				if (result.IsUpdated)
				{
					_update.OnNext(result);
				}

				return result;
			}
		}

		/// <inheritdoc />
		public async Task<DataPersisterUpdateResult<T>> Update(CancellationToken ct, DataPersisterAsyncUpdaterWithContext<T> asyncUpdater)
		{
			using (await _updateGate.LockAsync(ct))
			{
				var result = await _inner.Update(ct, asyncUpdater);

				if (result.IsUpdated)
				{
					_update.OnNext(result);
				}

				return result;
			}
		}

		/// <inheritdoc />
		public IObservable<DataReaderLoadResult<T>> Observe() => _update;

		/// <inheritdoc />
		public IObservable<DataReaderLoadResult<T>> GetAndObserve() => _getAndObserve;

		private IObservable<DataReaderLoadResult<T>> BuildGetAndObserve() => Observable
			.FromAsync(Load)
			.Concat(_update) // Potential bug: if the data is updated after the load and before the subscription to _update, the value will be lost.
			.ReplayOneRefCount(_replayScheduler);
	}
}
