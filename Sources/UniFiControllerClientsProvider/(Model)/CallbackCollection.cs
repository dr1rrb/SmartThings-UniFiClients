using System;
using System.Linq;
using System.Threading.Tasks;

namespace UniFiControllerUpnpAdapter.Business
{
	public class CallbackCollection
	{
		public string DeviceId { get; set; }

		public Callback[] Callbacks { get; set; }
	}
}