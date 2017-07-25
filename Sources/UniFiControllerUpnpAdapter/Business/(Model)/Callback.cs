using System;
using System.Linq;
using System.Threading.Tasks;

namespace UniFiControllerUpnpAdapter.Business
{
	public class Callback
	{
		public string Id { get; set; }

		public Uri Uri { get; set; }

		public TimeSpan Duration { get; set; }

		public DateTimeOffset Expiration { get; set; }
	}
}