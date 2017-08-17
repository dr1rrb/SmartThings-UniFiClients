using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Framework.Extensions;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher.Controllers
{
	public class DeviceController : Controller
	{
		private readonly ISsdpPublishingService _ssdp;

		public DeviceController(ISsdpPublishingService ssdp)
		{
			_ssdp = ssdp;
		}

		[HttpGet("api/devices")]
		public async Task<IImmutableList<IDevice>> Get(CancellationToken ct)
		{
			return await _ssdp.GetDevices(ct);
		}

		[HttpGet("api/[controller]/{id}")]
		public async Task<ActionResult> Get(CancellationToken ct, string id)
		{
			var (hasDocument, document) = await _ssdp.GetDescriptionDocument(ct, id);
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
