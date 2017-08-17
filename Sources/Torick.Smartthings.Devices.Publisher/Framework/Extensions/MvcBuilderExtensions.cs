using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Framework.Extensions
{
	internal static class MvcBuilderExtensions
	{
		public static IMvcBuilder AddModules(this IMvcBuilder services, Type applicationArguments, params string[] modulePaths)
		{
			var loader = new ModuleLoader(modulePaths, applicationArguments);
			loader.Configure(services.Services);
			foreach (var module in loader.GetModules())
			{
				services.AddApplicationPart(module.GetType().Assembly);
			}

			return services;
		}

		public static IMvcBuilder AddModules(this IMvcBuilder services, params string[] modulePaths)
		{
			var loader = new ModuleLoader(modulePaths);
			loader.Configure(services.Services);
			foreach (var module in loader.GetModules())
			{
				services.AddApplicationPart(module.GetType().Assembly);
			}

			return services;
		}
	}
}