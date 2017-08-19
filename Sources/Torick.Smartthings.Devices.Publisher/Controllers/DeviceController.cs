using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Torick.Extensions;
using Torick.Smartthings.Devices;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher.Controllers
{
	public class DeviceController : Controller
	{
		private readonly ISsdpPublishingService _ssdp;
		private readonly IDeviceStatusCallbackManager _callbacks;

		public DeviceController(ISsdpPublishingService ssdp, IDeviceStatusCallbackManager callbacks)
		{
			_ssdp = ssdp;
			_callbacks = callbacks;
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

		[Route("api/device/{deviceId}", Order = 2)]
		public async Task<IActionResult> Subscribe(
			string deviceId,
			[FromHeader(Name = "CALLBACK")] string callbackHeader,
			[FromHeader(Name = "TIMEOUT")] string timeoutHeader)
		{
			if (!HttpContext.Request.Method.Equals("SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
			{
				return NotFound();
			}

			if (string.IsNullOrWhiteSpace(deviceId)
				|| string.IsNullOrWhiteSpace(callbackHeader)
				|| string.IsNullOrWhiteSpace(timeoutHeader))
			{
				return BadRequest();
			}

			var callbackUri = new Uri(callbackHeader.TrimStart('<').TrimEnd('>'), UriKind.Absolute);
			var callbackDuration = TimeSpan.FromSeconds(int.Parse(timeoutHeader.TrimStart("Second-", StringComparison.OrdinalIgnoreCase)));
			var callbackExpiration = DateTimeOffset.Now + callbackDuration;

			var callback = new Callback
			{
				Id = Guid.NewGuid().ToString("N"),
				Uri = callbackUri,
				Duration = callbackDuration,
				Expiration = callbackExpiration
			};

			await _callbacks.AddCallback(ControllerContext.HttpContext.RequestAborted, deviceId, callback);

			Response.Headers["SID"] = $"uuid:{callback.Id}";
			Response.Headers["SERVER"] = $"Windows/10.1706 UPnP/1.1 UniFiClientManagerServer/1.0";
			Response.Headers["TIMEOUT"] = $"Second-{callback.Duration.TotalSeconds}";
			Response.Headers["Content-Length"] = "0";
			Response.Headers[SmartthingsDevice.Id] = deviceId;

			return Ok();
		}
	}
}
