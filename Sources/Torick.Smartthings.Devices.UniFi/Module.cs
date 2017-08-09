using System;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using Framework.Persistence;
using Framework.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Torick.IoC.Module;
using Torick.IoC.Module.LaunchArgs;
using Torick.Smartthings.Devices.UniFi;
using _callbacks = System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Torick.Smartthings.Devices.UniFi.Callback>>;

namespace UniFiControllerClientsProvider
{
	public class Module : IModule
    {
	    public static readonly ValueArgument Controller = new ValueArgument("unifi.controller")
	    {
		    Name = "Username",
		    Description = "Endpoint of the UniFi controller (<uri|ip>[:port]).",
		    IsRequired = true
	    };

	    public static readonly ValueArgument Username = new ValueArgument("unifi.username")
	    {
		    Name = "Username",
		    Description = "Username of the UniFi controller.",
		    IsRequired = true,
		    DefaultValue = "Admin"
	    };

	    public static readonly ValueArgument Password = new ValueArgument("unifi.password")
	    {
		    Name = "Password",
		    Description = "Password of the UniFi controller.",
		    IsRequired = true
	    };

		public void ConfigureServices(IServiceCollection services, ArgumentManager arguments)
	    {
		    services
				.AddSingleton<IUniFiController>(svc => new UniFiController(
				    new Uri($"https://{arguments.GetValue(Controller)}/api/"),
				    arguments.GetValue(Username),
				    arguments.GetValue(Password),
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
				    svc.GetService<IScheduler>()));
	    }

		public void Launch(IServiceProvider services)
	    {
			((DeviceService)services.GetService<IDeviceService>()).Start();
		}
	}
}
