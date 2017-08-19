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
	        //var arguments = ApplicationArguments.GetArguments;
			var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
				//.UseUrls($"http://0.0.0.0:{arguments.GetValue(ApplicationArguments.Port)}")
				.UseUrls($"http://0.0.0.0:5000")
				.Build();

	        host.Services.StartBackgroundServices();
			((SsdpPublishingService)host.Services.GetService<ISsdpPublishingService>()).Start();
	        ((DeviceStatusCallbackManager)host.Services.GetService<IDeviceStatusCallbackManager>()).Start();

			host.Run();
        }
    }
}
