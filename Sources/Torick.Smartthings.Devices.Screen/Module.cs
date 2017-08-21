using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Torick.IoC.Module;
using Torick.IoC.Module.LaunchArgs;


namespace Torick.Smartthings.Devices.Screen
{
    public class Module : IModule
    {
	    public void ConfigureServices(IServiceCollection services, ArgumentManager arguments)
	    {
		    services
			    .AddSingleton<IScreenService>(svc => new ScreenDeviceProvider(svc.GetService<IScheduler>()))
			    .AddSingleton<IDeviceProvider>(svc => svc.GetService<IScreenService>() as IDeviceProvider)
				;
		}

	    public void Launch(IServiceProvider services)
	    {
	    }
    }
}
