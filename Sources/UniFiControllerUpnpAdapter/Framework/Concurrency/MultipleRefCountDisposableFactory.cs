//#define TRACE_MULTIPLE_REF_COUNT_DISPOSABLE_FACTORY

using System;
using System.Threading;

namespace Framework.Concurrency
{
	/// <summary>
	/// This class is a building block for services who needs to share a unique disposable
	/// for which the lifetime is handle by the number of active subscriptions.
	/// </summary>
	/// <remarks>
	/// Don't forgive to dispose all subscriptions!
	/// </remarks>
	public class MultipleRefCountDisposableFactory<T> : IDisposable where T : class, IDisposable
	{
		private readonly Func<T> _factory;
		private T _current;
		private int _refcount;

		private object _lock = new object();

#if TRACE_MULTIPLE_REF_COUNT_DISPOSABLE_FACTORY
		private static int NextInstanceNo = 1;
		private int InstanceNo = Interlocked.Increment(ref NextInstanceNo);
#endif

		public MultipleRefCountDisposableFactory(Func<T> factory)
		{
			_factory = factory;
		}

		/// <summary>
		/// Create a new instance subscription of the inner service
		/// </summary>
		/// <remarks>
		/// On first disposable created, the inner factory is invoked.
		/// When all generated disposables are disposed, the inner service is
		/// disposed.
		/// If new disposables are created again, the inner service is created again.
		/// 
		/// WARNING: You can have a reference to the inner service from the subscription.
		/// Don't dispose it manually or you'll have a disposed shared service!  Let this
		/// component dispose it for you at the right time.
		/// </remarks>
		public MultipleRefCountDisposableSubscription CreateDisposable()
		{
			lock (_lock)
			{
				var newCount = Interlocked.Increment(ref _refcount);
				if (newCount < 1)
				{
					throw new ObjectDisposedException(GetType().Name);
				}

#if TRACE_MULTIPLE_REF_COUNT_DISPOSABLE_FACTORY
				this.Log().Debug(InstanceNo + "> New Subscription, count=" + newCount);
#endif

				if (newCount == 1)
				{
					_current = _factory();
				}

				return new MultipleRefCountDisposableSubscription(Decrease, _current);
			}
		}

		private void Decrease()
		{
			lock (_lock)
			{
				var newCount = Interlocked.Decrement(ref _refcount);
#if TRACE_MULTIPLE_REF_COUNT_DISPOSABLE_FACTORY
				this.Log().Debug(InstanceNo + "> Removed subscription, count=" + newCount);
#endif
				if (newCount > 0)
				{
					return; // no clean-up to do (ref remainings)
				}

				if (_current == null)
				{
					return; // already cleaned-up ?  We're probably disposing the service
				}
#if TRACE_MULTIPLE_REF_COUNT_DISPOSABLE_FACTORY
				this.Log().Debug(InstanceNo + "> Disposing refcounted object.");
#endif

				_current.Dispose(); // dispose managed object
				_current = null;
			}
		}

		public void Dispose()
		{
			Interlocked.Exchange(ref _refcount, -1);
			Decrease();
		}

		public sealed class MultipleRefCountDisposableSubscription : IDisposable
		{
			public T Instance { get; private set; }

			private Action _onDispose;

			internal MultipleRefCountDisposableSubscription(Action onDispose, T instance)
			{
				Instance = instance;
				_onDispose = onDispose;
			}

			public void Dispose()
			{
				var disposeAction = Interlocked.Exchange(ref _onDispose, null);
				if (disposeAction == null)
				{
					throw new ObjectDisposedException(GetType().Name);
				}
				disposeAction();
			}
		}
	}
}
