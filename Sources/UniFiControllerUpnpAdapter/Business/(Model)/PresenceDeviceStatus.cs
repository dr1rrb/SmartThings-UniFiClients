using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UniFiControllerUpnpAdapter.Business
{
	public class PresenceDeviceStatus
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("presence")]
		[JsonConverter(typeof(StringEnumConverter))]
		public PresenceState Presence { get; set; }
	}
}