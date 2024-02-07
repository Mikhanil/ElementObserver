using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace ElementObserver;

internal static class ProcessExtensions
{
	[DllImport("user32.dll")]
	private static extern int SetWindowText(IntPtr hWnd, string text);

	public static void SetWindowText(this Process process, in string newName)
	{
		if (process.MainWindowHandle == IntPtr.Zero)
			return;

		SetWindowText(process.MainWindowHandle, newName);
	}

	public static string GetCommandLineArgs(this Process process)
	{
		if (process is null || process.Id < 1) return "";

		if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WMI is only supported on Windows.");

		var query =
			$@"SELECT CommandLine
           FROM Win32_Process
           WHERE ProcessId = {process.Id}";

		using var searcher = new ManagementObjectSearcher(query);
		using var collection = searcher.Get();
		var managementObject = collection.OfType<ManagementObject>().FirstOrDefault();
		return managementObject != null ? (string)managementObject["CommandLine"] : string.Empty;
	}
}