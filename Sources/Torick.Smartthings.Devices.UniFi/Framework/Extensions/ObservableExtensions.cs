using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Framework.Concurrency;

namespace Framework.Extensions
{
	public static class ObservableExtensions
	{
		public static IObservable<T> Retry<T>(this IObservable<T> source, TimeSpan retryDelay, IScheduler scheduler)
		{
			return source.Catch<T, Exception>(_ => source.DelaySubscription(retryDelay, scheduler).Retry());
		}

		public static IObservable<T> Retry<T>(this IObservable<T> source, int retryCount, TimeSpan retryDelay, IScheduler scheduler)
		{
			return source.Catch<T, Exception>(_ => source.DelaySubscription(retryDelay, scheduler).Retry(retryCount));
		}

		public static IObservable<T> ReplayOneRefCount<T>(this IObservable<T> source, IScheduler scheduler)
		{
			var factory =
				new MultipleRefCountDisposableFactory<ReplayOneRefCountInnerSubscription<T>>(
					() => new ReplayOneRefCountInnerSubscription<T>(source, scheduler));


			return Observable.Create<T>(
				observer =>
				{
					var refcountedSubscription = factory.CreateDisposable();
					var localSubscription = refcountedSubscription
						.Instance
						.Subject
						.Subscribe(observer);

					return new CompositeDisposable
						{
							localSubscription,
							refcountedSubscription
						};
				});
		}

		private class ReplayOneRefCountInnerSubscription<T> : IDisposable
		{
			private readonly CompositeDisposable _disposables;

			internal ReplaySubject<T> Subject { get; }

			internal ReplayOneRefCountInnerSubscription(IObservable<T> source, IScheduler scheduler)
			{
				Subject = new ReplaySubject<T>(1, scheduler);

				_disposables =
					new CompositeDisposable
					{
						source.Subscribe(Subject),
						Subject
					};
			}

			public void Dispose()
			{
				_disposables.Dispose();
			}
		}
	}
}
