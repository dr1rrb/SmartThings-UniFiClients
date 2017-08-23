using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.Screen
{
	public enum ScreenStatus
	{
		[EnumMember(Value = "off")]
		Off = 0,

		[EnumMember(Value = "on")]
		On = 1,
	}
}