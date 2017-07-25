using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace UniFiControllerUpnpAdapter.Controllers
{
	[Route("api/[controller]")]
	public class PingController : Controller
    {
		public async Task<ActionResult> Ping([FromHeader(Name = "Smartthings-Device")] string id)
		{
			Response.Headers["Content-Length"] = "0";
			Response.Headers["Smartthings-Device"] = id;

			return Ok();
		}
	}
}
