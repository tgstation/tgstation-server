using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for engine version management.
	/// </summary>
	[Flags]
	public enum EngineRights : ulong
	{
		/// <summary>
		/// User has no rights.
		/// </summary>
		None = 0,

		/// <summary>
		/// User may view the active installed engine versions.
		/// </summary>
		ReadActive = 1 << 0,

		/// <summary>
		/// User may list all installed engine versions.
		/// </summary>
		ListInstalled = 1 << 1,

		/// <summary>
		/// User may install official <see cref="Models.EngineType.Byond"/> versions or change the active <see cref="Models.EngineType.Byond"/> version.
		/// </summary>
		InstallOfficialOrChangeActiveByondVersion = 1 << 2,

		/// <summary>
		/// User may cancel an engine installation job.
		/// </summary>
		CancelInstall = 1 << 3,

		/// <summary>
		/// User may upload and activate custom <see cref="Models.EngineType.Byond"/> builds.
		/// </summary>
		InstallCustomByondVersion = 1 << 4,

		/// <summary>
		/// User may delete non-active engine builds.
		/// </summary>
		DeleteInstall = 1 << 5,

		/// <summary>
		/// User may install official <see cref="Models.EngineType.OpenDream"/> versions or change the active <see cref="Models.EngineType.OpenDream"/> version.
		/// </summary>
		InstallOfficialOrChangeActiveOpenDreamVersion = 1 << 6,

		/// <summary>
		/// User may activate custom <see cref="Models.EngineType.OpenDream"/> builds via zip upload or custom git committish.
		/// </summary>
		InstallCustomOpenDreamVersion = 1 << 7,
	}
}
