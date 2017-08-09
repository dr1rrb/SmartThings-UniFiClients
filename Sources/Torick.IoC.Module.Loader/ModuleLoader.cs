using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Torick.IoC.Module;
using Torick.IoC.Module.LaunchArgs;

namespace Torick.IoC.Module.Loader
{
	public class ModuleLoader
	{
		private readonly string[] _providersDlls;
		private readonly Type[] _arguments;
		private readonly Lazy<IModule[]> _modules;

		public ModuleLoader(string[] providersDlls, Type applicationArguments = null)
		{
			_providersDlls = providersDlls;
			_arguments = applicationArguments == null 
				? new Type[0] 
				: new []{applicationArguments};

			_modules = new Lazy<IModule[]>(GetModules, LazyThreadSafetyMode.PublicationOnly);
		}

		public void Configure(IServiceCollection collection)
		{
			collection.AddSingleton(this);

			var modules = _modules.Value;
			var arguments = ArgumentManager.Create(modules.Select(module => module.GetType()).Concat(_arguments));

			foreach (var module in modules)
			{
				module.ConfigureServices(collection, arguments);
			}
		}

		public void Start(IServiceProvider services)
		{
			foreach (var module in _modules.Value)
			{
				module.Launch(services);
			}
		}

		private IModule[] GetModules()
			=> _providersDlls
				.SelectMany(GetFiles)
				.Select<string, Assembly>(dllPath => AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath))
				.SelectMany(assembly => assembly.GetTypes())
				.Where(type => typeof(IModule).IsAssignableFrom(type))
				.Select(Activator.CreateInstance)
				.Cast<IModule>()
				.ToArray();

		private IEnumerable<string> GetFiles(string path)
		{
			if (File.Exists(path))
			{
				yield return path;
			}
			else if (Directory.Exists(path))
			{
				foreach (var dll in Directory.GetFiles(path, "*.dll"))
				{
					yield return dll;
				}
			}
			else
			{
				// Log FILE NOT FOUND
			}
		}
	}
}