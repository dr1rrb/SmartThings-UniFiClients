using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace SmartthingsSsdpDeviceProvider
{
	public interface IDeviceProvider
	{
		/// <summary>
		/// Get and observe currently available devices 
		/// </summary>
		IObservable<IImmutableList<IDevice>> GetAndObserveDevices();
	}
}