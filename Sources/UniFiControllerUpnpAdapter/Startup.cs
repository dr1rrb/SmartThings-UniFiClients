using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Framework.Persistence;
using Framework.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniFiControllerUpnpAdapter.Business;
using _callbacks = System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<UniFiControllerUpnpAdapter.Business.Callback>>;

namespace UniFiControllerUpnpAdapter
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
	        var arguments = ApplicationArguments.GetArguments;

			// Add framework services.
			services.AddMvc();

	        services
		        .AddSingleton<IScheduler>(svc => TaskPoolScheduler.Default)
		        .AddSingleton<IUniFiController>(svc => new UniFiController(
					new Uri($"https://{arguments.GetValue(ApplicationArguments.Controller)}/api/"),
					arguments.GetValue(ApplicationArguments.Username),
					arguments.GetValue(ApplicationArguments.Password), 
					svc.GetService<IScheduler>()))
		        .AddSingleton<ISsdpPublishingService>(svc => new SsdpPublishingService(
			        svc.GetService<IUniFiController>(),
			        new Uri($"http://192.168.144.202:{arguments.GetValue(ApplicationArguments.Port)}"),
					svc.GetService<IScheduler>()))
		        .AddSingleton<IObjectSerializer>(svc => new JsonConverterObjetSerializer())
		        .AddSingleton<IObservableDataPersister<_callbacks>>(svc =>
		        {
			        var persister = new LockedFileDataPersister<_callbacks>("callbacks.json", svc.GetService<IObjectSerializer>());
			        var withDefault = new DefaultValueDataPersisterDecorator<_callbacks>(persister, DefaultValueDataPersisterDecoratorMode.All, ImmutableDictionary<string, ImmutableList<Callback>>.Empty);
			        var observable = new ObservableDataPersisterDecorator<_callbacks>(withDefault, svc.GetService<IScheduler>());

			        return observable;
		        })
		        .AddSingleton<IDeviceService>(svc => new DeviceService(
			        svc.GetService<IUniFiController>(),
			        svc.GetService<IObservableDataPersister<_callbacks>>(),
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
	            .UseResponseBuffering()
				;
        }
    }
}
