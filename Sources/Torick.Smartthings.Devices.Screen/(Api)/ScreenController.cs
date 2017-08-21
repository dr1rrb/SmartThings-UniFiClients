using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Torick.Smartthings.Devices.Screen
{
	[Route("api/[controller]")]
	public class ScreenController : Controller
	{
		private readonly IScreenService _service;

		public ScreenController(IScreenService service)
		{
			_service = service;
		}

		[HttpGet]
		public async Task<string> Get()
		{
			return await _service.Get(HttpContext.RequestAborted);
		}

		[HttpPut("on")]
		public async Task On()
		{
			await _service.On(HttpContext.RequestAborted);
		}

		[HttpPut("off")]
		public async Task Off()
		{
			await _service.Off(HttpContext.RequestAborted);
		}

		[HttpPost("toggle")]
		public async Task Toggle()
		{
			await _service.Off(HttpContext.RequestAborted);
		}
	}
}