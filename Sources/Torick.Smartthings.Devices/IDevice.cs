using System;
using System.Linq;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices
{
	public interface IDevice
	{
		/// <summary>
		/// The unique identifier of the device (Required)
		/// </summary>
		string Id { get; }

		/// <summary>
		/// The display name of the device which will appear in the smarthings devices configuration list (Required)
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// The namespace of the smartthings device handler (Required)
		/// </summary>
		string DeviceNamespace { get; }

		/// <summary>
		/// The type of the smartthings device handler (Required)
		/// </summary>
		string DeviceType { get; }

		/// <summary>
		/// The manufacturer of the device (Optional)
		/// </summary>
		string Manufacturer { get; }

		/// <summary>
		/// The model name of the device. (Optional)
		/// </summary>
		string ModelName { get; }
	}
}