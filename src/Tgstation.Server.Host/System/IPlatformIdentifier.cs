using System.Runtime.Versioning;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// For identifying the current platform.
	/// </summary>
	public interface IPlatformIdentifier
	{
		/// <summary>
		/// If the current platform is a Windows platform.
		/// </summary>
		[SupportedOSPlatformGuard("windows")]
		bool IsWindows { get; }

		/// <summary>
		/// The extension of executable script files for the system.
		/// </summary>
		string ScriptFileExtension { get; }
	}
}
