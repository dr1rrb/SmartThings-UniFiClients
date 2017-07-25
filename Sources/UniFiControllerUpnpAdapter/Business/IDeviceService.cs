using System.Threading;
using System.Threading.Tasks;

namespace UniFiControllerUpnpAdapter.Business
{
	public interface IDeviceService
	{
		Task Set(CancellationToken ct, string deviceId, bool isConnected);

		Task AddCallback(CancellationToken ct, string device, Callback callback);
	}
}