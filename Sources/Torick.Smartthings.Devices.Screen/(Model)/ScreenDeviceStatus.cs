using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Torick.Smartthings.Devices.Screen
{
    public class ScreenDeviceStatus
    {
		public ScreenDeviceStatus(bool isOn)
		{
			Status = isOn 
				? ScreenStatus.On 
				: ScreenStatus.Off;
		}

		[JsonProperty("status")]
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ScreenStatus Status { get; set; }

	    public override string ToString()
	    {
		    return Status.ToString();
	    }
    }
}
