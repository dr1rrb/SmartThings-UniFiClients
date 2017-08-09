using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Framework.Extensions;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher.Controllers
{
	[Route("api/[controller]")]
	public class DeviceController : Controller
	{
		private readonly ISsdpPublishingService _ssdp;

		public DeviceController(ISsdpPublishingService ssdp)
		{
			_ssdp = ssdp;
		}

		[HttpGet("{id}")]
		public async Task<ActionResult> Get(string id)
		{
			var (hasDocument, document) = await _ssdp.GetDescriptionDocument(id);
			if (hasDocument)
			{
				return Content(document, "application/xml", Encoding.UTF8);
			}
			else
			{
				return NotFound();
			}
		}
	}
}
