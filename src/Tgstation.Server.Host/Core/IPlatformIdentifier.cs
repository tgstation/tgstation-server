namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// For identifying the current platform
	/// </summary>
	public interface IPlatformIdentifier
	{
		/// <summary>
		/// If the current platform is a Windows platform
		/// </summary>
		bool IsWindows { get; }
	}
}
