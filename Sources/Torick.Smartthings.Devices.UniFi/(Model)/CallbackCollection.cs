using System;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.UniFi
{
	public class CallbackCollection
	{
		public string DeviceId { get; set; }

		public Callback[] Callbacks { get; set; }
	}
}