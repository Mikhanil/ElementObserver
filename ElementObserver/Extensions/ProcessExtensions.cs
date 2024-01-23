using System.Runtime.InteropServices;
using System.Diagnostics;


namespace ElementObserver;

internal static class ProcessExtensions
{
	[DllImport("user32.dll")]
	private static extern int SetWindowText(IntPtr hWnd, string text);

	public static void SetWindowText(this Process process, in string newName)
	{
		if(process.MainWindowHandle == IntPtr.Zero)
			return;

		SetWindowText(process.MainWindowHandle, newName);
	}
}