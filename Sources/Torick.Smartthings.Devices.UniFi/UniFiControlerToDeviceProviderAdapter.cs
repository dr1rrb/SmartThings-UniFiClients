using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Torick.Extensions;

namespace Torick.Smartthings.Devices.UniFi
{
	public class UniFiControlerToDeviceProviderAdapter : IDeviceProvider
	{
		private readonly IUniFiController _controller;

		public UniFiControlerToDeviceProviderAdapter(IUniFiController controller) => _controller = controller;

		public IObservable<IImmutableList<IDevice>> GetAndObserveDevices() 
			=> _controller
				.GetAndObserveClients()
				.Select(clients => clients.Select(client => new UniFiClientToDeviceAdapter(client) as IDevice).ToImmutableList());

		public (bool isKnownDevice, IObservable<object> status) TryGetAndObserveStatus(string deviceId)
		{
			if (deviceId.StartsWith(UniFiClientToDeviceAdapter.DeviceIdPrefix, StringComparison.OrdinalIgnoreCase))
			{
				var status = _controller
					.GetAndObserveIsConnected(deviceId.TrimStart(UniFiClientToDeviceAdapter.DeviceIdPrefix, StringComparison.OrdinalIgnoreCase))
					.Select(isConnected => new PresenceDeviceStatus
					{
						Id = deviceId,
						Presence = isConnected ? PresenceState.Present : PresenceState.NotPresent
					});

				return (true, status);
			}
			else
			{
				return (false, Observable.Empty<object>());
			}
		}
	}
}