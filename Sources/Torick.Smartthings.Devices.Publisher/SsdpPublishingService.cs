using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Framework.Concurrency;
using Framework.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rssdp;
using Torick.Smartthings.Devices;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher
{
	public class SsdpPublishingService : ISsdpPublishingService, IDisposable
	{
		private readonly ConditionalWeakTable<SsdpRootDevice, IDisposable> _publications = new ConditionalWeakTable<SsdpRootDevice, IDisposable>();
		private readonly object _publicationsGate = new object();
		private readonly SerialDisposable _subscription = new SerialDisposable();
		private readonly SsdpDevicePublisher _publisher = new SsdpDevicePublisher();

		private readonly IEnumerable<IDeviceProvider> _providers;
		private readonly Uri _deviceUrl;
		private readonly IScheduler _scheduler;

		private ImmutableDictionary<string, (IDevice source, SsdpRootDevice ssdp)> _devices = ImmutableDictionary<string, (IDevice, SsdpRootDevice)>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase);

		public SsdpPublishingService(IEnumerable<IDeviceProvider> providers, Uri deviceUrl, IScheduler scheduler)
		{
			_providers = providers;
			_deviceUrl = deviceUrl;
			_scheduler = scheduler;
		}

		public void Start() => _subscription.Disposable = StartCore();

		private IDisposable StartCore()
		{
			var deviceSubscriptions = new SerialCompositeDisposable();
			var controllerSubscription = _providers
				.Select(provider => provider.GetAndObserveDevices().StartWith(ImmutableList<IDevice>.Empty))
				.CombineLatest()
				.Select(devicesPerProvider => devicesPerProvider.SelectMany(devices => devices))
				.Do(devices =>
				{
					var publishedDevices = devices
						.Select(client => PublishDevice(_publisher, GetDevice(client)))
						.ToImmutableList();

					deviceSubscriptions.Update(publishedDevices);
				})
				.Retry(TimeSpan.FromSeconds(30), _scheduler)
				.Subscribe();

			return new CompositeDisposable
			{
				deviceSubscriptions,
				controllerSubscription
			};
		}

		public async Task<IImmutableList<IDevice>> GetDevices(CancellationToken ct)
			=> _publisher
				.Devices
				.Select(d => _devices[d.Uuid].source)
				.ToImmutableList();

		public async Task<(bool hasDocument, string document)> GetDescriptionDocument(CancellationToken ct, string deviceId) 
			=> _devices.TryGetValue(deviceId, out var device) 
				? (true, device.ssdp.ToDescriptionDocument()) 
				: (false, string.Empty);

		private SsdpRootDevice GetDevice(IDevice client)
		{
			while (true)
			{
				var devices = _devices;
				if (devices.TryGetValue(client.Id, out var device))
				{
					return device.ssdp;
				}

				device = ToDevice(client);
				if (Interlocked.CompareExchange(ref _devices, devices.Add(client.Id, device), devices) == devices)
				{
					return device.ssdp;
				}
			}
		}

		private (IDevice, SsdpRootDevice) ToDevice(IDevice device) => (device, new SsdpRootDevice
		{
			CacheLifetime = TimeSpan.FromMinutes(30), //How long SSDP clients can cache this info.
			Location = new Uri($"/api/device/{device.Id}", UriKind.Relative), // Must point to the URL that serves your devices UPnP description document. 
			DeviceTypeNamespace = "torick-net",
			DeviceType = "UniFiDevice",
			DeviceVersion = 1,
			FriendlyName = device.DisplayName.MustHaveValue(nameof(IDevice.DisplayName)), // Yes de-entitize should have be done in the UniFi controller, but as in fact we use this only once ...
			Manufacturer = device.Manufacturer.OrDefault("unknown"),
			ModelName = device.ModelName.OrDefault("unknown"),
			Uuid = device.Id.MustHaveValue(nameof(IDevice.Id)),
			UrlBase = _deviceUrl,
			CustomProperties =
			{
				new SsdpDeviceProperty
				{
					Name = "smartthingsDeviceNamespace",
					Value = device.DeviceNamespace.MustHaveValue(nameof(IDevice.DeviceNamespace))
				},
				new SsdpDeviceProperty
				{
					Name = "smartthingsDeviceType",
					Value = device.DeviceType.MustHaveValue(nameof(IDevice.DeviceType))
				}
			}
		});

		private IDisposable PublishDevice(SsdpDevicePublisher publisher, SsdpRootDevice device)
		{
			lock (_publicationsGate)
			{
				return _publications.GetValue(device, Publish);
			}

			IDisposable Publish(SsdpRootDevice d)
			{
				publisher.AddDevice(d);
				return Disposable.Create(UnPublish);

				void UnPublish()
				{
					lock (_publicationsGate)
					{
						publisher.RemoveDevice(d);
						_publications.Remove(d);
					}
				}
			}
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_publisher.Dispose();
		}
	}
}