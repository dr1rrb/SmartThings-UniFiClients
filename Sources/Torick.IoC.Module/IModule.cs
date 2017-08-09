using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Torick.IoC.Module.LaunchArgs;

namespace Torick.IoC.Module
{
	public interface IModule
	{
		void ConfigureServices(IServiceCollection services, ArgumentManager arguments);

		void Launch(IServiceProvider services);
	}
}
