using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Framework.Concurrency;
using Framework.Extensions;
using Framework.Persistence;
using Framework.Web;

namespace Torick.Smartthings.Devices.UniFi
{
	public class DeviceService : IDeviceService, IDisposable
	{
		private readonly HttpClient _client = new HttpClient();
		private readonly SerialDisposable _subscription = new SerialDisposable();
		private readonly Subject<(string deviceId, bool isConnected)> _override = new Subject<(string deviceId, bool isConnected)>();

		private readonly IUniFiController _unifi;
		private readonly IObservableDataPersister<ImmutableDictionary<string, ImmutableList<Callback>>> _storage;
		private readonly IScheduler _scheduler;

		public DeviceService(IUniFiController unifi, IObservableDataPersister<ImmutableDictionary<string, ImmutableList<Callback>>> storage, IScheduler scheduler)
		{
			_unifi = unifi;
			_storage = storage;
			_scheduler = scheduler;
		}

		public async Task AddCallback(CancellationToken ct, string device, Callback callback)
		{
			// Currently we keep only one callback per device
			await _storage.Update(ct, ctx => ctx.Commit(ctx.Value.SetItem(device, (/*ctx.Value.GetValueOrDefault(device) ??*/ ImmutableList<Callback>.Empty).Add(callback))));
		}

		public void Start()
		{
			var track = new Func<string, ImmutableList<Callback>, IDisposable>(Track).AsMemoized();
			var deviceSubscriptions = new SerialCompositeDisposable();
			var storageSubscription = _storage
				.GetAndObserve()
				.Do(data => deviceSubscriptions.Update(data.Value.Select(kvp => track(kvp.Key, kvp.Value)).ToImmutableList()))
				.Retry(TimeSpan.FromSeconds(5), _scheduler)
				.Subscribe();

			_subscription.Disposable = new CompositeDisposable(
				storageSubscription,
				deviceSubscriptions,
				StorageScavenging());
		}

		public async Task Set(CancellationToken ct, string deviceId, bool isConnected)
			=> _override.OnNext((deviceId, isConnected));

		private IDisposable Track(string deviceId, IImmutableList<Callback> callbacks)
		{
			return _unifi
				.GetAndObserveIsConnected(deviceId)

				// Allow overrides for test
				.Merge(_override.Where(t => t.deviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase)).Select(t => t.isConnected))
				
				.DistinctUntilChanged()
				.Do(isConnected => Console.WriteLine($"Device '{deviceId}' is now connect: {isConnected}"))

				// refresh the state each 5 mn
				.Select(isConnected => Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(5), _scheduler).Select(_ => isConnected))
				.Switch()

				// Publish the state
				.Select(isConnected => Observable.FromAsync(ct => Notify(ct, deviceId, callbacks, isConnected)))
				.Switch()

				// Safety
				.Retry(TimeSpan.FromSeconds(5), _scheduler)
				.Subscribe();
		}

		private async Task Notify(CancellationToken ct, string deviceId, IImmutableList<Callback> callbacks, bool isConnected)
		{
			try
			{
				var payload = new JsonContent(new PresenceDeviceStatus
				{
					Id = deviceId,
					Presence = isConnected ? PresenceState.Present : PresenceState.NotPresent
				})
				{
					Headers = { { "Smartthings-Device", deviceId } }
				};
				payload.TrySetContentLength();

				await callbacks
					.Select(callback => new HttpRequestMessage(HttpMethod.Post, callback.Uri) { Content = payload })
					.Select(request => Observable
						.FromAsync(async ct2 => (await _client.SendAsync(request, ct2)).EnsureSuccessStatusCode())
						.Retry(5, TimeSpan.FromSeconds(3), _scheduler))
					.Merge()
					.ToTask(ct);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
			}
		}

		private IDisposable StorageScavenging()
		{
			return _scheduler.ScheduleAsync(
				TimeSpan.FromMinutes(15),
				async (s, ct) =>
				{
					await _storage.Update(ct, ctx =>
					{
						var updated = ctx.Value;
						var now = s.Now;
						foreach (var callbacksPerDevice in ctx.Value)
						{
							var outdated = callbacksPerDevice.Value.Where(callback => callback.Expiration < now).ToList();
							if (outdated.Count == callbacksPerDevice.Value.Count)
							{
								updated = updated.Remove(callbacksPerDevice.Key);
							}
							else if (outdated.Count > 0)
							{
								updated = updated.SetItem(callbacksPerDevice.Key, callbacksPerDevice.Value.RemoveRange(outdated));
							}
						}

						if (ctx.Value != updated)
						{
							ctx.Commit(updated);
						}
					});
					return StorageScavenging();
				});
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_client.Dispose();
		}
	}
}