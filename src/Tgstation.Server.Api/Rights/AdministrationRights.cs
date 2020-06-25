using System;

namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// Rights for <see cref="Models.Administration"/>
	/// </summary>
	[Flags]
	public enum AdministrationRights : ulong
	{
		/// <summary>
		/// User has no rights
		/// </summary>
		None = 0,

		/// <summary>
		/// User can edit themself and other <see cref="Models.User"/>s and also create others
		/// </summary>
		WriteUsers = 1,

		/// <summary>
		/// User can gracefully restart the host
		/// </summary>
		RestartHost = 2,

		/// <summary>
		/// User can change <see cref="Models.Administration.NewVersion"/>
		/// </summary>
		ChangeVersion = 4,

		/// <summary>
		/// User can change their password
		/// </summary>
		EditOwnPassword = 8,

		/// <summary>
		/// User can read info and rights of other users
		/// </summary>
		ReadUsers = 16,

		/// <summary>
		/// User can list and download <see cref="Models.LogFile"/>s.
		/// </summary>
		DownloadLogs = 32,
	}
}
