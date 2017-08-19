using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		private readonly Lazy<ImmutableArray<IModule>> _modules;

		public ModuleLoader(string[] providersDlls, Type applicationArguments = null)
		{
			_providersDlls = providersDlls;
			_arguments = applicationArguments == null 
				? new Type[0] 
				: new []{applicationArguments};

			_modules = new Lazy<ImmutableArray<IModule>>(_GetModules, LazyThreadSafetyMode.PublicationOnly);
		}

		public void Configure(IServiceCollection collection)
		{
			var modules = _modules.Value;
			var arguments = ArgumentManager.Create(modules.Select(module => module.GetType()).Concat(_arguments));

			collection.AddSingleton(this);
			collection.AddSingleton(arguments);

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

		public ImmutableArray<IModule> GetModules()
			=> _modules.Value;

		private ImmutableArray<IModule> _GetModules()
			=> _providersDlls
				.SelectMany(GetFiles)
				.Select(dllPath =>
				{
					try
					{
						return AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
					}
					catch (Exception)
					{
						return default(Assembly);
					}
				})
				.Where(assembly => assembly != null)
				.SelectMany(assembly => assembly.GetTypes())
				.Where(type => typeof(IModule).IsAssignableFrom(type))
				.Select(Activator.CreateInstance)
				.Cast<IModule>()
				.ToImmutableArray();

		private IEnumerable<string> GetFiles(string path)
		{
			path = Path.Combine(Directory.GetCurrentDirectory(), path);
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