using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Framework.Extensions;
using Microsoft.AspNetCore.Mvc;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.UniFi
{
	[Route("api/unifi")]
	public class DeviceController : Controller
	{
		private readonly IDeviceService _devices;

		public DeviceController(IDeviceService devices)
		{
			_devices = devices;
		}

		[Route("{deviceId}", Order = 2)]
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

			await _devices.AddCallback(ControllerContext.HttpContext.RequestAborted, deviceId, callback);

			Response.Headers["SID"] = $"uuid:{callback.Id}";
			Response.Headers["SERVER"] = $"Windows/10.1706 UPnP/1.1 UniFiClientManagerServer/1.0";
			Response.Headers["TIMEOUT"] = $"Second-{callback.Duration.TotalSeconds}";
			Response.Headers["Content-Length"] = "0";
			Response.Headers[SmartthingsDevice.Id] = deviceId;

			return Ok();
		}

		[HttpPut("{deviceId}")]
		public async Task<ActionResult> Override(string deviceId, [FromForm] PresenceState state)
		{
			await _devices.Set(CancellationToken.None, deviceId, state == PresenceState.Present);

			return Ok();
		}
	}
}
