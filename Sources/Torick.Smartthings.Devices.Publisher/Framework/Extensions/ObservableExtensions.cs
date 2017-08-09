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
	internal static class ObservableExtensions
	{
		public static IObservable<T> Retry<T>(this IObservable<T> source, TimeSpan retryDelay, IScheduler scheduler)
		{
			return source.Catch<T, Exception>(_ => source.DelaySubscription(retryDelay, scheduler).Retry());
		}

		public static IObservable<T> Retry<T>(this IObservable<T> source, int retryCount, TimeSpan retryDelay, IScheduler scheduler)
		{
			return source.Catch<T, Exception>(_ => source.DelaySubscription(retryDelay, scheduler).Retry(retryCount));
		}
	}
}
