using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Framework.Persistence;
using Torick.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Torick.IoC.Module.LaunchArgs;
using Torick.Persistence;
using Torick.Serialization;
using Torick.Smartthings.Devices.UniFi;
using _callbacks = System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Torick.Smartthings.Devices.Callback>>;

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
			// Add framework services.
	        services
		        .AddMvc()
		        .AddModules(typeof(ApplicationArguments), "./Devices/");

			services
		        .AddSingleton<IScheduler>(svc => TaskPoolScheduler.Default)
		        .AddSingleton<ISsdpPublishingService>(svc => new SsdpPublishingService(
			        svc.GetServices<IDeviceProvider>(),
					new Uri($"http://{svc.GetService<ArgumentManager>().GetValue(ApplicationArguments.Host)}:{svc.GetService<ArgumentManager>().GetValue(ApplicationArguments.Port)}"),
					svc.GetService<IScheduler>()))
				.AddSingleton<IObjectSerializer>(svc => new JsonConverterObjetSerializer())
				.AddSingleton<IObservableDataPersister<_callbacks>>(svc =>
				{
					var persister = new LockedFileDataPersister<_callbacks>("callbacks.json", svc.GetService<IObjectSerializer>());
					var withDefault = new DefaultValueDataPersisterDecorator<_callbacks>(persister, DefaultValueDataPersisterDecoratorMode.All, _callbacks.Empty);
					var observable = new ObservableDataPersisterDecorator<_callbacks>(withDefault, svc.GetService<IScheduler>());

					return observable;
				})
				.AddSingleton<IDeviceStatusCallbackManager>(svc => new DeviceStatusCallbackManager(
					svc.GetServices<IDeviceProvider>(),
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
	            .UseResponseBuffering();
        }
    }
}
