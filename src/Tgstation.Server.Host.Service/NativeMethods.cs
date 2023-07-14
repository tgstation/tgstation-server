using System.Runtime.InteropServices;

namespace Tgstation.Server.Host.Service
{
	/// <summary>
	/// Native methods used by the code.
	/// </summary>
	static class NativeMethods
	{
		/// <summary>
		/// See https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox.
		/// </summary>
		public enum MessageBoxButtons : uint
		{
			/// <summary>
			/// The message box contains two push buttons: Yes and No.
			/// </summary>
			YesNo = 0x00000004,
		}

		/// <summary>
		/// The result of a call to <see cref="MessageBox(HandleRef, string, string, MessageBoxButtons)"/>.
		/// </summary>
		public enum DialogResult : int
		{
			/// <summary>
			/// The Yes button was selected.
			/// </summary>
			Yes = 6,
		}

		/// <summary>
		/// Displays a modal dialog box that contains a system icon, a set of buttons, and a brief application-specific message, such as status or error information. The message box returns an integer value that indicates which button the user clicked.
		/// </summary>
		/// <param name="hWnd">A handle to the owner window of the message box to be created. If this parameter is NULL, the message box has no owner window.</param>
		/// <param name="text">The message to be displayed. If the string consists of more than one line, you can separate the lines using a carriage return and/or linefeed character between each line.</param>
		/// <param name="caption">The dialog box title. If this parameter is NULL, the default title is Error.</param>
		/// <param name="type">The <see cref="MessageBoxButtons"/>.</param>
		/// <returns>The resulting <see cref="DialogResult"/>.</returns>
		/// <remarks>See https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox.</remarks>
		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		public static extern DialogResult MessageBox(HandleRef hWnd, string text, string caption, MessageBoxButtons type);
	}
}
