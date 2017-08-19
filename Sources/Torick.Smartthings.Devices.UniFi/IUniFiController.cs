using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Torick.Smartthings.Devices;

namespace Torick.Smartthings.Devices.UniFi
{
	public interface IUniFiController
	{
		IObservable<ImmutableList<Client>> GetAndObserveClients();

		IObservable<bool> GetAndObserveIsConnected(string clientId);
	}
}