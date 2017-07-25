using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace UniFiControllerUpnpAdapter.Business
{
	public enum PresenceState
	{
		[EnumMember(Value = "not present")]
		NotPresent = 0,

		[EnumMember(Value = "present")]
		Present = 1
	}
}