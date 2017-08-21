using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.Screen
{
	public interface IScreenService
	{
		Task<string> Get(CancellationToken ct); // TODO: bool !

		Task On(CancellationToken ct);

		Task Off(CancellationToken ct);

		Task Toggle(CancellationToken ct);
	}
}