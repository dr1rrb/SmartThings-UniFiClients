using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UniFiControllerUpnpAdapter.Business
{
	public class ApiResponse<T>
	{
		[JsonProperty("data")]
		public T Data { get; set; }
	}
}