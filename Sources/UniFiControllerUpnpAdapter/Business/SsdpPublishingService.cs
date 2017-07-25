using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Framework.Concurrency;
using Framework.Extensions;
using Rssdp;

namespace UniFiControllerUpnpAdapter.Business
{
	public class SsdpPublishingService : ISsdpPublishingService, IDisposable
	{
		private readonly SerialDisposable _subscription = new SerialDisposable();
		private readonly IUniFiController _controller;
		private readonly Uri _deviceUrl;
		private readonly IScheduler _scheduler;
		private readonly Func<SsdpDevicePublisher, SsdpRootDevice, IDisposable> _addDevice;

		private ImmutableDictionary<string, SsdpRootDevice> _devices = ImmutableDictionary<string, SsdpRootDevice>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

		public SsdpPublishingService(IUniFiController controller, Uri deviceUrl, IScheduler scheduler)
		{
			_controller = controller;
			_deviceUrl = deviceUrl;
			_scheduler = scheduler;

			_addDevice = new Func<SsdpDevicePublisher, SsdpRootDevice, IDisposable>(AddDevice).AsMemoized();
		}

		public void Start() => _subscription.Disposable = StartCore();

		private IDisposable StartCore()
		{
			var publisher = new SsdpDevicePublisher();
			var deviceSubscriptions = new SerialCompositeDisposable();
			var controllerSubscription = _controller
				.GetAndObserveClients()
				.Do(clients => deviceSubscriptions.Update(clients.Select(client => _addDevice(publisher, GetDevice(client))).ToImmutableList()))
				.Retry(TimeSpan.FromSeconds(30), _scheduler)
				.Subscribe();

			return new CompositeDisposable
			{
				publisher,
				deviceSubscriptions,
				controllerSubscription
			};
		}

		public async Task<(bool hasDocument, string docuemnt)> GetDescriptionDocument(string deviceId) 
			=> _devices.TryGetValue(deviceId, out var device) 
				? (true, device.ToDescriptionDocument()) 
				: (false, string.Empty);

		private SsdpRootDevice GetDevice(Client client)
		{
			while (true)
			{
				var devices = _devices;
				if (devices.TryGetValue(client.Id, out var device))
				{
					return device;
				}

				device = ToDevice(client);
				if (Interlocked.CompareExchange(ref _devices, devices.Add(client.Id, device), devices) == devices)
				{
					return device;
				}
			}
		}

		private SsdpRootDevice ToDevice(Client client) => new SsdpRootDevice
		{
			CacheLifetime = TimeSpan.FromMinutes(30), //How long SSDP clients can cache this info.
			Location = new Uri($"/api/device/{client.Id}", UriKind.Relative), // Must point to the URL that serves your devices UPnP description document. 
			DeviceTypeNamespace = "torick-net",
			DeviceType = "UniFiDevice",
			DeviceVersion = 1,
			FriendlyName = HtmlAgilityPack.HtmlEntity.DeEntitize(client.DisplayName), // Yes de-entitize should have be done in the UniFi controller, but as in fact we use this only once ...
			Manufacturer = HtmlAgilityPack.HtmlEntity.DeEntitize(client.Manufacturer),
			ModelName = "UniFi client",
			Uuid = client.Id,
			UrlBase = _deviceUrl
		};

		private IDisposable AddDevice(SsdpDevicePublisher publisher, SsdpRootDevice device)
		{
			publisher.AddDevice(device);

			return Disposable.Create(() => publisher.RemoveDevice(device));
		}

		public void Dispose() => _subscription.Dispose();
	}
}