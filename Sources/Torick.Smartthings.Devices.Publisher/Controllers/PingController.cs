using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Torick.Smartthings.Devices;

namespace Torick.Smartthings.Devices.Publisher.Controllers
{
	[Route("api/[controller]")]
	public class PingController : Controller
    {
		public async Task<ActionResult> Ping([FromHeader(Name = SmartthingsDevice.Id)] string id)
		{
			Response.Headers["Content-Length"] = "0";
			Response.Headers[SmartthingsDevice.Id] = id;

			return Ok();
		}
	}
}
