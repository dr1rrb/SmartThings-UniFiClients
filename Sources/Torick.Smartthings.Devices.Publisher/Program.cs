using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Torick.IoC.Module.Loader;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher
{
    public class Program
    {
        public static void Main(string[] args)
        {
			// Note: Since modules are not loaded yet, at this point we have access only to arguments defined at the application level (i.e. ApplicationArguments).
	        var arguments = ApplicationArguments.Arguments;
			var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
				.UseUrls($"http://0.0.0.0:{arguments.GetValue(ApplicationArguments.Port)}")
				.Build();

	        host.Services.StartBackgroundServices();
			((SsdpPublishingService)host.Services.GetService<ISsdpPublishingService>()).Start();
	        ((DeviceStatusCallbackManager)host.Services.GetService<IDeviceStatusCallbackManager>()).Start();

			host.Run();
        }
    }
}
