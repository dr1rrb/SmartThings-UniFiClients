using System;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.UniFi
{
	public class UniFiClientToDeviceAdapter : IDevice
	{
		public const string DeviceIdPrefix = "unifi_";
		private readonly Client _client;

		public UniFiClientToDeviceAdapter(Client client) => _client = client;

		public string Id => DeviceIdPrefix + _client.Id;

		public string DisplayName => HtmlAgilityPack.HtmlEntity.DeEntitize(_client.DisplayName);

		public string DeviceNamespace => "torick.net";

		public string DeviceType => "UniFi client device";

		public string Manufacturer => HtmlAgilityPack.HtmlEntity.DeEntitize(_client.Manufacturer);

		public string ModelName => "UniFi client";
	}
}