using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Torick.Extensions;

namespace Torick.Smartthings.Devices.Screen
{
	public class ScreenDeviceProvider : IScreenService, IDeviceProvider
	{
	    private readonly IScheduler _scheduler;
	    private readonly Device _device;

		private bool _status = true;
		private readonly ISubject<bool> _observeStatus = new Subject<bool>();

	    public ScreenDeviceProvider(IScheduler scheduler)
	    {
		    _scheduler = scheduler;
		    _device = new Device();
	    }

		public Task<string> Get(CancellationToken ct)
			=> ScreenHelper.Get(ct);

		public async Task On(CancellationToken ct)
		{
			await ScreenHelper.On(ct);
			_observeStatus.OnNext(true);
		}

		public async Task Off(CancellationToken ct)
		{
			await ScreenHelper.Off(ct);
			_observeStatus.OnNext(false);
		}

		public async Task Toggle(CancellationToken ct)
		{
			var status = _status;
			if (status)
			{
				await ScreenHelper.Off(ct);
			}
			else
			{
				await ScreenHelper.On(ct);
			}
			_observeStatus.OnNext(!status);
		}

		public IObservable<IImmutableList<IDevice>> GetAndObserveDevices()
		    => Observable.Return(ImmutableArray.Create(_device as IDevice) as IImmutableList<IDevice>, _scheduler);

	    public (bool isKnownDevice, IObservable<object> status) TryGetAndObserveStatus(string deviceId) 
			=> _device.Id.Equals(deviceId, StringComparison.OrdinalIgnoreCase) 
				? (true, _observeStatus.StartWith(() => _status, _scheduler).Select(s => s ? ScreenStatus.On : ScreenStatus.Off as object)) 
				: (false, Observable.Empty<object>(_scheduler));

		private class Device : IDevice
	    {
		    public Device()
		    {
			    DisplayName = System.Net.Dns.GetHostName();
				Id = $"{DisplayName}_screen";
		    }

		    public string Id { get; }
		    public string DisplayName { get; }
		    public string DeviceNamespace => "torick.net";
		    public string DeviceType => "Computer screen";
		    public string Manufacturer { get; }
		    public string ModelName { get; }
	    }
	}
}
