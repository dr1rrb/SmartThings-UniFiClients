using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Torick.Smartthings.Devices.UniFi
{
	public class Client
	{
		public string DisplayName => string.IsNullOrWhiteSpace(Name) ? HostName : Name;

		[JsonProperty("_id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("mac")]
		public string Mac { get; set; }

		[JsonProperty("hostname")]
		public string HostName { get; set; }

		[JsonProperty("oui")]
		public string Manufacturer { get; set; }

	}
}