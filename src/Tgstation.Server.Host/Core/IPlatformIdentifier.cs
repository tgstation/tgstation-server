namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// For identifying the current platform
	/// </summary>
	interface IPlatformIdentifier
	{
		/// <summary>
		/// If the current platform is a Windows platform
		/// </summary>
		bool IsWindows { get; }
	}
}
