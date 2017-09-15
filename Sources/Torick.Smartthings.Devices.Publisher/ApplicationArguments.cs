using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Torick.Extensions;
using Torick.IoC.Module.LaunchArgs;

namespace Torick.Smartthings.Devices.Publisher
{
	public class ApplicationArguments
	{
		public static readonly Argument Help = CommonArguments.Help;

		public static readonly ValueArgument Host = new ValueArgument("host")
		{
			Name = "Server hots name",
			Description = "The uri/ip that smartthings have to use to reach this controller",
			IsRequired = true,
			DefaultValue = GetDefaultHost()
		};

		public static readonly ValueArgument<int> Port = new ValueArgument<int>("port")
		{
			Name = "Server port",
			Description = "The port to listen on (used for communication between the smartthings devices and the upnp adapter).",
			IsRequired = true,
			DefaultValue = 5000
		};

		// This MUST be the last on the class in order to let other properties being initialized
		public static ArgumentManager Arguments { get; } = ArgumentManager.Create(typeof(ApplicationArguments));

		private static string GetDefaultHost()
		{
			var domain = IPGlobalProperties.GetIPGlobalProperties().DomainName.Trim('.');
			var hostName = Dns.GetHostName().Trim('.');

			return domain.HasValue()
				? hostName.TrimEnd(domain, StringComparison.OrdinalIgnoreCase) + "." + domain
				: hostName;
		}
	}
}
