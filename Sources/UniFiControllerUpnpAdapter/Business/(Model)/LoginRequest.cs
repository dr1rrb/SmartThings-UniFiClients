using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UniFiControllerUpnpAdapter.Business
{
	public class LoginRequest
	{
		[JsonProperty("username")]
		public string UserName { get; set; }

		[JsonProperty("password")]
		public string Password { get; set; }
	}
}