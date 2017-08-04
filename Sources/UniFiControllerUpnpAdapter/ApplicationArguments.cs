using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Framework.LaunchArgs;

namespace UniFiControllerUpnpAdapter
{
    public class ApplicationArguments
    {
		public static readonly Argument Help = CommonArguments.Help;

	    public static readonly ValueArgument<int> Port = new ValueArgument<int>("port")
	    {
		    Name = "Port",
		    Description = "The port to listen on (used for communication between the smartthings devices and the upnp adapter).",
		    IsRequired = true,
		    DefaultValue = 5000
	    };

		public static readonly ValueArgument Controller = new ValueArgument("c", "controller")
	    {
		    Name = "Username",
		    Description = "Endpoint of the UniFi controller (<uri|ip>[:port]).",
		    IsRequired = true
	    };

		public static readonly ValueArgument Username = new ValueArgument("u", "username")
	    {
		    Name = "Username",
		    Description = "Username of the UniFi controller.",
		    IsRequired = true,
		    DefaultValue = "Admin"
	    };

	    public static readonly ValueArgument Password = new ValueArgument("p", "password")
	    {
		    Name = "Password",
		    Description = "Password of the UniFi controller.",
		    IsRequired = true
	    };

		// This MUST be the last on the class in order to let other properties being initialized
	    public static ArgumentManager GetArguments { get; } = ArgumentManager.Create(typeof(ApplicationArguments));
	}
}
