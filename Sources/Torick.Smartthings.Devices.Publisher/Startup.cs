using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Framework.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Torick.Smartthings.Devices.UniFi;

namespace Torick.Smartthings.Devices.Publisher
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
	        //var arguments = ApplicationArguments.GetArguments;

			// Add framework services.
	        services
		        .AddMvc()
		        .AddModules("./Devices/");

			services
		        .AddSingleton<IScheduler>(svc => TaskPoolScheduler.Default)



		        .AddSingleton<ISsdpPublishingService>(svc => new SsdpPublishingService(
			        svc.GetServices<IDeviceProvider>(),
					//new Uri($"http://192.168.144.202:{arguments.GetValue(ApplicationArguments.Port)}"),
			        new Uri("http://192.168.144.202:5000"),
					svc.GetService<IScheduler>()))
				;
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app
				.UseMvc()
	            .UseResponseBuffering();
        }
    }
}
