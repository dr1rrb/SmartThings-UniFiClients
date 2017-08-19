using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.UniFi
{
	[Route("api/unifi")]
	public class DeviceController : Controller
	{
		private readonly IDeviceStatusCallbackManager _devices;

		public DeviceController(IDeviceStatusCallbackManager devices)
		{
			_devices = devices;
		}

		//[HttpPut("{deviceId}")]
		//public async Task<ActionResult> Override(string deviceId, [FromForm] PresenceState state)
		//{
		//	await _devices.Set(CancellationToken.None, deviceId, new PresenceDeviceStatus
		//	{
		//		Id = deviceId,
		//		Presence = PresenceState.Present
		//	});

		//	return Ok();
		//}
	}
}
