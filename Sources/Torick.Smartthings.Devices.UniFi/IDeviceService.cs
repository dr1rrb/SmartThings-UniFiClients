using System.Threading;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.UniFi
{
	public interface IDeviceService
	{
		Task Set(CancellationToken ct, string deviceId, bool isConnected);

		Task AddCallback(CancellationToken ct, string device, Callback callback);
	}
}