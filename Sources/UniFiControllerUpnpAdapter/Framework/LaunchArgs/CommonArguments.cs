using System;

namespace Framework.LaunchArgs
{
	public static class CommonArguments
	{
		public static readonly FlagArgument Help = new FlagArgument("?", "h", "help")
			{
				Name = "Help",
				Description = "Print this help screen."
			};

//		public static readonly ValueArgument<LogMessageType> Log = new ValueArgument<LogMessageType>("log")
//			{
//				Name = "Log",
//				Description = "Set log level (for all logs)",
//#if DEBUG
//				DefaultValue = LogMessageType.Debug,
//#endif
//			};

//		public static readonly ArrayArgument<LogRepositories> LogType = new ArrayArgument<LogRepositories>("logtype")
//			{
//				Name = "Log Type",
//				Description = "Define which logger will be injected by default",
//#if DEBUG
//				DefaultValue = new[] { LogRepositories.File, LogRepositories.Console,  }
//#else
//				DefaultValue = new[] { LogRepositories.File }
//#endif
//		};

//		public enum LogRepositories
//		{
//			File,
//			Console
//		}
	}
}
