using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices
{
	public interface IDeviceProvider
	{
		/// <summary>
		/// Get and observe currently available devices 
		/// </summary>
		IObservable<IImmutableList<IDevice>> GetAndObserveDevices();

		/// <summary>
		/// Gets and observe the the current status of the device
		/// </summary>
		/// <param name="deviceId">Id of the device</param>
		/// <returns>An observable sequence of the status that will be sent back to the device or null if the id is not present</returns>
		(bool isKnownDevice, IObservable<object> status) TryGetAndObserveStatus(string deviceId);
	}
}