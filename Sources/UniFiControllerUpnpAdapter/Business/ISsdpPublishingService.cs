using System;
using System.Threading.Tasks;

namespace UniFiControllerUpnpAdapter.Business
{
	public interface ISsdpPublishingService
	{
		Task<(bool hasDocument, string docuemnt)> GetDescriptionDocument(string deviceId);
	}
}