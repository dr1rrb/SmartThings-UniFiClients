using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Torick.Extensions;

namespace Torick.IoC.Module.LaunchArgs
{
	public class ArgumentManager
	{
		private const string _userProtectedSuffix = "-UserProtected";
		private const string _userProtectSuffix = "-UserProtect";

		private readonly IDictionary<string, string> _values;

		public Argument[] Arguments { get; }

		public ArgumentManager(params Argument[] arguments)
		{
			Arguments = arguments;

			// validate no conflicts
			if (arguments.SelectMany(arg => arg.Options.Distinct()).Distinct().Count() != arguments.SelectMany(arg => arg.Options.Distinct()).Count())
			{
				throw new InvalidOperationException("Some parameters have same options");
			}

			var allArgs = Environment.GetCommandLineArgs()
#if FULL_NET
				.Concat(AppDomain.CurrentDomain?.SetupInformation?.ActivationArguments?.ActivationData ?? Enumerable.Empty<string>())
#endif
				.ToArray();

            _values = ArgumentsParser.Parse(allArgs);
			//this.Log().Debug("Input args = {0}", string.Join(" ", allArgs));
			//this.Log().Debug(string.Join(Environment.NewLine, _values.Select(kvp => "{0}={1}".InvariantCultureFormat(kvp.Key, kvp.Value))));

			CheckHelp();
			CheckUserSecure();
			ValidateRequiredArguments();
		}

		public static ArgumentManager Create(Type type) 
			=> new ArgumentManager(GetArguments(type).ToArray());

		public static ArgumentManager Create(IEnumerable<Type> types) 
			=> new ArgumentManager(types.SelectMany(GetArguments).ToArray());

		private static IEnumerable<Argument> GetArguments(Type type)
		{
			return type
				.GetMembers()
				.OfType<FieldInfo>()
				.Where(fieldInfo =>
					fieldInfo.FieldType == typeof(Argument)
					|| fieldInfo.FieldType == typeof(FlagArgument)
					|| fieldInfo.FieldType == typeof(ValueArgument)
					|| (fieldInfo.FieldType.GetTypeInfo().IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ValueArgument<>))
					|| fieldInfo.FieldType == typeof(ArrayArgument)
					|| (fieldInfo.FieldType.GetTypeInfo().IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(ArrayArgument<>)))
				.Select(fieldInfo => (Argument)fieldInfo.GetValue(null));
		}

		private void CheckHelp()
		{
			if (Arguments.Contains(CommonArguments.Help) && IsSet(CommonArguments.Help))
			{
				PrintHelp();

				Environment.Exit(0);
			}
		}

		private void CheckUserSecure()
		{
			var valuesToProtect = _values
				.Where(kvp => kvp.Key.EndsWith(_userProtectSuffix, StringComparison.OrdinalIgnoreCase))
				.Select(kvp => new
				{
					argument = Arguments.FirstOrDefault(a => a.Options.Contains(kvp.Key.TrimEnd(_userProtectSuffix, StringComparison.OrdinalIgnoreCase))),
					option = kvp.Key,
					value = kvp.Value,
				})
				.Where(x => x.argument != null)
				.ToArray();

			if (valuesToProtect.Any())
			{
				Console.WriteLine(Assembly.GetEntryAssembly()?.GetName().Name);
				Console.WriteLine("© David Rey 2013-2017");
				Console.WriteLine();
				Console.WriteLine("Encrypt arguments:");
				Console.WriteLine("------------------");

				foreach (var value in valuesToProtect)
				{
					var salt = Encoding.UTF8.GetBytes(value.argument.Name);
					var data = ProtectedData.Protect(Encoding.UTF8.GetBytes(value.value), salt, DataProtectionScope.CurrentUser);
					
					Console.WriteLine("{0} ({1}): {2}", value.argument.Name, value.option, Convert.ToBase64String(data)); 
				}

				Environment.Exit(0);
			}
		}

		public void PrintHelp()
		{
			Console.WriteLine(Assembly.GetEntryAssembly()?.GetName().Name);
			Console.WriteLine("© David Rey 2013-2015");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine("------");
			Console.WriteLine("\t{0} {1} [Options]", 
				Path.GetFileName(Assembly.GetEntryAssembly()?.Location),
				Arguments
					.OfType<ValueArgument>()
					.Where(arg => arg.IsRequired && !arg.HasDefaultValue)
					.Select(arg => $"-{arg.Options[0]}:<{arg.Type.Name}>")
					.JoinBy(" "));
			Console.WriteLine();
			Console.WriteLine("Options:");
			Console.WriteLine("--------");
			foreach (var argument in Arguments)
			{
				Console.WriteLine("{0,-15} {1}", argument.Name, string.Join(", ", argument.Options.Select(opt => "-" + opt)));
				var valueArgument = argument as ValueArgument;
				var arrayArgument = argument as ArrayArgument;

				if (valueArgument != null)
				{
					var typeInfo = valueArgument.Type.GetTypeInfo().IsEnum
						? "One of: {" + string.Join(", ", Enum.GetNames(valueArgument.Type)) + "}"
						: "Of type: <" + valueArgument.Type.Name + ">";

					Console.WriteLine("\t{0} {1}", valueArgument.IsRequired ? "(required)" : "(optional)", typeInfo);

					if (valueArgument.HasDefaultValue)
					{
						Console.WriteLine("\tDefault value: {0}", valueArgument.DefaultValue);
					}
				}
				else if (arrayArgument != null)
				{
					var elementType = arrayArgument.Type.GetElementType();
					var typeInfo = elementType.GetTypeInfo().IsEnum
						? "Some of: {" + string.Join(", ", Enum.GetNames(elementType)) + "}"
						: "Of type: <" + elementType.Name + ">";

					Console.WriteLine("\t{0} {1}", arrayArgument.IsRequired ? "(required)" : "(optional)", typeInfo);

					if (arrayArgument.HasDefaultValue)
					{
						Console.WriteLine("\tDefault values: {0}", String.Join(", ", arrayArgument.DefaultValue));
					}
				}

				if (argument.Requires?.Any() ?? false)
				{
					Console.WriteLine("\tRequires: {0}", String.Join("; ", argument.Requires.Select(arg => arg().Name)));
				}
				Console.WriteLine(SplitText("\t", argument.Description));
				Console.WriteLine();
			}


			var aArg = Arguments.Except(CommonArguments.Help/*, CommonArguments.Log, CommonArguments.LogType*/).FirstOrDefault() 
				?? Arguments.FirstOrDefault() 
				//?? CommonArguments.Log
				;

			Console.WriteLine("Infos:");
			Console.WriteLine("------");
			Console.WriteLine("All arguments can be provided encrypted by suffixing the arguement option by {0} (eg. {1}{0}:<encrypted-value>).", _userProtectedSuffix, aArg.Options.First());
			Console.WriteLine("To encrypt the value, you have to append the suffix {0} to the argment option (eg. {1}{0}:<encrypted-value>).", _userProtectSuffix, aArg.Options.First());
		}

		private string SplitText(string prefix, string text)
		{
			prefix = prefix.Replace("\t", new string(' ', 8));
			var textWidth = Console.WindowWidth - prefix.Length - 1;

			var sb = new StringBuilder();
			for (var i = 0; i < text.Length; i += textWidth)
			{
				sb.AppendLine(prefix + text.Substring(i, Math.Min(text.Length - i, textWidth)));
			}

			return sb.ToString();
		}

		public void ValidateRequiredArguments()
		{
			var required = Arguments
				.OfType<ValueArgument>()
				.Where(arg => arg.IsRequired && !arg.HasDefaultValue && !IsSet(arg));

			var innerRequirements = Arguments
				.Where(IsSet)
				.SelectMany(arg => arg.Requires)
				.Select(selector => selector())
				.OfType<ValueArgument>()
				.Distinct()
				.Where(arg => !arg.HasDefaultValue && !IsSet(arg));

			var missings = required
				.Concat(innerRequirements)
				.Distinct()
				.ToArray();

			if (missings.Any())
			{
				Console.WriteLine("Missing required parameter(s) : {0}", String.Join(",", missings.Select(arg => arg.Name)));
				Console.WriteLine();

				PrintHelp();

				Environment.Exit(0);
			}
		}
	
		public bool IsSet(Argument argument)
		{
			return argument.Options.Any(o => _values.Keys.Contains(o, StringComparer.OrdinalIgnoreCase)
				|| _values.Keys.Contains(o + _userProtectedSuffix, StringComparer.OrdinalIgnoreCase));
		}

		public T? GetValue<T>(ValueArgument<T> argument)
			where T : struct
		{
			return SafeConvertTo<T>(GetValue(argument as ValueArgument))
				?? argument.DefaultValue;
		}

		public T?[] GetValue<T>(ArrayArgument<T> argument)
			where T : struct
		{
			return GetValue(argument as ArrayArgument)?.Select(SafeConvertTo<T>).ToArray()
				?? argument.DefaultValue.Cast<T?>().ToArray();
		}

		public string GetValue(ValueArgument argument)
		{
			return 
				argument
					.Options
					.Select(o => FindValue(o, argument))
					.FirstOrDefault(value => value != null)
				?? argument.DefaultValue;
		}

		public string[] GetValue(ArrayArgument argument)
		{
			return 
				argument
					.Options
					.Select(o => FindValue(o, argument))
					.FirstOrDefault(value => value != null)
					?.Split(';')
				?? argument.DefaultValue;
		}

		private string FindValue(string key, Argument arg)
		{
			string value;
			if (_values.TryGetValue(key, out value))
			{
				return value;
			}
			else if (_values.TryGetValue(key + _userProtectedSuffix, out value))
			{
				var salt = Encoding.UTF8.GetBytes(arg.Name);
				var data = ProtectedData.Unprotect(Convert.FromBase64String(value), salt, DataProtectionScope.CurrentUser);

				return Encoding.UTF8.GetString(data);
			}
			else
			{
				return null;
			}
		}

		private static T? SafeConvertTo<T>(string value)
			where T : struct
		{
			if (value.HasValue())
			{
				var type = typeof (T);
				if (type.GetTypeInfo().IsEnum && Enum.TryParse(value, true, out T result))
				{
					return result;
				}
				else
				{
					try
					{
						return (T)Convert.ChangeType(value, type);
					}
					catch (Exception)
					{
					}
				}
			}
			
			return default(T?);
		}
	}
}