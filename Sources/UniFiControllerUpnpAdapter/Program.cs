using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using UniFiControllerUpnpAdapter.Business;

namespace UniFiControllerUpnpAdapter
{
    public class Program
    {
        public static void Main(string[] args)
        {
	        var arguments = ApplicationArguments.GetArguments;
			var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
				.UseUrls($"http://0.0.0.0:{arguments.GetValue(ApplicationArguments.Port)}")
				.Build();

	        //((DeviceService)host.Services.GetService<IDeviceService>()).Start();
	        ((SsdpPublishingService)host.Services.GetService<ISsdpPublishingService>()).Start();

			host.Run();
        }
    }
}
