using System;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.UniFi
{
	public interface ISsdpPublishingService
	{
		Task<(bool hasDocument, string docuemnt)> GetDescriptionDocument(string deviceId);
	}
}