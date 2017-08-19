using System.Threading;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices
{
	public interface IDeviceStatusCallbackManager
	{
		Task AddCallback(CancellationToken ct, string device, Callback callback);
	}
}