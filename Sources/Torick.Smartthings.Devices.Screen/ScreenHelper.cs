using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Torick.Smartthings.Devices.Screen
{
	public static class ScreenHelper
	{
		#region External monitor state
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		private const int WM_SYSCOMMAND = 0x112;
		private const uint SC_MONITORPOWER = 0xF170;
		private enum MonitorState
		{
			MonitorStateOn = -1,
			MonitorStateOff = 2,
			MonitorStateStandBy = 1
		}

		private static void SetMonitorState(MonitorState state)
			=> SendMessage(GetConsoleWindow(), WM_SYSCOMMAND, (IntPtr)SC_MONITORPOWER, (IntPtr)state);

		#endregion

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		#region External mouse move
		[DllImport("user32.dll")]
		private static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);

		private const int MOUSEEVENTF_MOVE = 0x0001; 
		#endregion

		public static async Task Off(CancellationToken ct)
			=> SetMonitorState(MonitorState.MonitorStateOff);

		public static async Task On(CancellationToken ct)
		{
			mouse_event(MOUSEEVENTF_MOVE, 0, 1, 0, UIntPtr.Zero);
			await Task.Delay(40, ct);
			mouse_event(MOUSEEVENTF_MOVE, 0, -1, 0, UIntPtr.Zero);
		}






		public static async Task<string> Get(CancellationToken ct)
		{
			var handle = GetConsoleWindow();
			var monitor = MonitorFromWindow(handle, MONITOR_DEFAULTTOPRIMARY);
			var result = GetDevicePowerState(monitor, out var isOn);
			return $"result: {result}, isOn: {isOn}";
		}

		const int MONITOR_DEFAULTTOPRIMARY = 1;

		[DllImport("user32.dll")]
		static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool GetDevicePowerState(IntPtr handle, out bool state);
	}
}