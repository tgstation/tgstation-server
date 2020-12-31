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
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may view the active installed BYOND version.
		/// </summary>
		ReadActive = 1,

		/// <summary>
		/// User may list all installed BYOND versions.
		/// </summary>
		ListInstalled = 2,

		/// <summary>
		/// User may install official BYOND versions or change the active BYOND version.
		/// </summary>
		InstallOfficialOrChangeActiveVersion = 4,

		/// <summary>
		/// User may cancel BYOND installation job.
		/// </summary>
		CancelInstall = 8,

		/// <summary>
		/// User may upload and activate custom BYOND builds.
		/// </summary>
		InstallCustomVersion = 16,
	}
}
