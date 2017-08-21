using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Torick.Smartthings.Devices.Screen
{
    public class ScreenDeviceStatus
    {
	    [JsonProperty("value")]
	    [JsonConverter(typeof(StringEnumConverter))]
	    public ScreenStatus Value { get; set; }
    }
}
