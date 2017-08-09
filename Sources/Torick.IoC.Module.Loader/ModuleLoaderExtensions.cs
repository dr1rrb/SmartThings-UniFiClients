using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Torick.IoC.Module.Loader
{
	public static class ModuleLoaderExtensions
	{
		public static IServiceCollection AddModules(this IServiceCollection services, Type applicationArguments, params string[] modulePaths)
		{
			new ModuleLoader(modulePaths, applicationArguments).Configure(services);
			return services;
		}

		public static IServiceCollection AddModules(this IServiceCollection services, params string[] modulePaths)
		{
			new ModuleLoader(modulePaths).Configure(services);
			return services;
		}

		public static void StartBackgroundServices(this IServiceProvider services)
		{
			services.GetService<ModuleLoader>().Start(services);
		}
	}
}