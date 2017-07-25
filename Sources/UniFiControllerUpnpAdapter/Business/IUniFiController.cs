using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using UniFiControllerUpnpAdapter.Business;

namespace UniFiControllerUpnpAdapter.Business
{
	public interface IUniFiController
	{
		IObservable<ImmutableList<Client>> GetAndObserveClients();

		IObservable<bool> GetAndObserveIsConnected(string clientId);
	}
}