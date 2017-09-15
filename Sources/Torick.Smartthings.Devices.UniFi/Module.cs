using System;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using Torick.IoC.Module;
using Torick.IoC.Module.LaunchArgs;
using Torick.Smartthings.Devices;
using Torick.Smartthings.Devices.UniFi;
using _callbacks = System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<Torick.Smartthings.Devices.Callback>>;

namespace UniFiControllerClientsProvider
{
	public class Module : IModule
    {
	    public static readonly ValueArgument Controller = new ValueArgument("unifi.controller")
	    {
		    Name = "Controller",
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
			    .AddSingleton<IDeviceProvider>(svc => new UniFiControlerToDeviceProviderAdapter(svc.GetService<IUniFiController>()))
			;
	    }

		public void Launch(IServiceProvider services)
	    {
		}
	}
}
