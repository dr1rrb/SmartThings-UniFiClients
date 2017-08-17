using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.UniFi
{
	public interface ISsdpPublishingService
	{
		Task<IImmutableList<IDevice>> GetDevices(CancellationToken ct);

		Task<(bool hasDocument, string document)> GetDescriptionDocument(CancellationToken ct, string deviceId);
	}
}