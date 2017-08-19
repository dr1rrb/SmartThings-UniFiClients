using System;
using System.Reactive.Concurrency;
using Framework.Persistence;
using Framework.Serialization;
using Microsoft.AspNetCore.Mvc;
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
			    //.AddSingleton<IUniFiController>(svc => new UniFiController(
				   // new Uri($"https://{arguments.GetValue(Controller)}/api/"),
				   // arguments.GetValue(Username),
				   // arguments.GetValue(Password),
				   // svc.GetService<IScheduler>()))
			    //.AddSingleton<IDeviceProvider>(svc => new UniFiClientProvider(svc.GetService<IUniFiController>()))
			    //.AddSingleton<IObjectSerializer>(svc => new JsonConverterObjetSerializer())
			    .AddSingleton<IObservableDataPersister<_callbacks>>(svc =>
			    {
				    var persister = new LockedFileDataPersister<_callbacks>("screens.callbacks.json", svc.GetService<IObjectSerializer>());
				    var withDefault = new DefaultValueDataPersisterDecorator<_callbacks>(persister, DefaultValueDataPersisterDecoratorMode.All, _callbacks.Empty);
				    var observable = new ObservableDataPersisterDecorator<_callbacks>(withDefault, svc.GetService<IScheduler>());

				    return observable;
			    })
			    .AddSingleton<IDeviceStatusProvider<ScreenMode>>(svc => new ScreenDevice())
			    .AddSingleton<IDeviceStatusCallbackManager<ScreenMode>>(svc => new DeviceStatusCallbackManager<ScreenMode>(
				    svc.GetService<IDeviceStatusProvider<ScreenMode>>(),
				    svc.GetService<IObservableDataPersister<_callbacks>>(),
				    svc.GetService<IScheduler>()));
		}

	    public void Launch(IServiceProvider services)
	    {
	    }
    }

	public enum ScreenMode
	{
		Off = 0,
		On = 1,
	}

	public class ScreenController : Controller
	{
		public ScreenController()
		{
			
		}

		[Route("api/screen/toggle", Order = 2)]
		public void Toggle()
		{
			
		}
	}

	public class ScreenDevice : IDeviceStatusProvider<ScreenMode>
	{
		public IObservable<ScreenMode> GetAndObserveStatus(string deviceId)
		{
			
		}
	}
}
