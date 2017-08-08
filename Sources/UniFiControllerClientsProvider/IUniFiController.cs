using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using SmartthingsSsdpDeviceProvider;
using UniFiControllerUpnpAdapter.Business;

namespace UniFiControllerUpnpAdapter.Business
{
	public interface IUniFiController
	{
		IObservable<ImmutableList<Client>> GetAndObserveClients();

		IObservable<bool> GetAndObserveIsConnected(string clientId);
	}

	public class UniFiClientProvider : IDeviceProvider
	{
		private readonly IUniFiController _controller;

		public UniFiClientProvider(IUniFiController controller) => _controller = controller;

		public IObservable<IImmutableList<IDevice>> GetAndObserveDevices() 
			=> _controller.GetAndObserveClients().Select(clients => clients.Select(client => new UniFiClient(client) as IDevice).ToImmutableList());
	}

	public class UniFiClient : IDevice
	{
		private readonly Client _client;

		public UniFiClient(Client client) => _client = client;

		public string Id => _client.Id;

		public string DisplayName => HtmlAgilityPack.HtmlEntity.DeEntitize(_client.DisplayName);

		public string DeviceNamespace => "torick.net";

		public string DeviceType => "UniFi client device";

		public string Manufacturer => HtmlAgilityPack.HtmlEntity.DeEntitize(_client.Manufacturer);

		public string ModelName => "UniFi client";
	}
}