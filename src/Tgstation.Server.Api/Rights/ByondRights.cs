using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Byond"/>
	/// </summary>
	[Flags]
	public enum ByondRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User may check the active installed BYOND version
		/// </summary>
		ReadActive = 1,

		/// <summary>
		/// User may list all installed BYOND versions
		/// </summary>
		ListInstalled = 2,

		/// <summary>
		/// User may change the active BYOND version. Also allows installing official BYOND versions
		/// </summary>
		InstallOfficialOrChangeActiveVersion = 4,

		/// <summary>
		/// User may cancel version installations
		/// </summary>
		CancelInstall = 8,

		/// <summary>
		/// User may upload custom BYOND versions
		/// </summary>
		InstallCustomVersion = 16,
	}
}
