namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For gathering runtime OS information
	/// </summary>
	interface IOSIdentifier
	{
		/// <summary>
		/// If the operating system is Win32 based
		/// </summary>
		bool IsWindows { get; }
	}
}