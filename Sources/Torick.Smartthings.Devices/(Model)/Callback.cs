using System;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices
{
	public class Callback
	{
		public string Id { get; set; }

		public Uri Uri { get; set; }

		public TimeSpan Duration { get; set; }

		public DateTimeOffset Expiration { get; set; }
	}
}